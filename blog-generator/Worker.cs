using System.Net.WebSockets;
using System.Text.RegularExpressions;
using AutoMapper;
using blog_generator.Configs;
using contentapi.data;
using contentapi.data.Views;
using contentapi.Db;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace blog_generator;

public class Worker : BackgroundService
{
    private readonly ILogger logger;
    protected WebsocketConfig wsconfig;
    protected BlogGenerator blogGenerator;
    protected IMapper mapper;

    public const string contentName = nameof(RequestType.content);
    public const string userName = nameof(RequestType.user);
    public const string blogFields = "id, text, hash, lastRevisionId, values, createUserId, createDate, parentId, keywords, description, contentType";

    public const string requestKey = "request";

    public const string initialPrecheckKey = "initial_precheck";
    public const string blogRefreshKey = "blog_refresh";
    public const string blogParentKey = "blog_parent";
    public const string blogPagesKey = "blog_pages";
    public const string styleRefreshKey = "style_refresh";

    public Worker(ILogger<Worker> logger, WebsocketConfig wsconfig, BlogGenerator blogGenerator, IMapper mapper)
    {
        this.logger = logger;
        this.wsconfig = wsconfig;
        this.blogGenerator = blogGenerator;
        this.mapper = mapper;
    }

    public SearchRequests GetFullBlogRegenSearchRequest(string hash)
    {
        return new SearchRequests()
        {
            values = new Dictionary<string, object>() {
                { "hash", hash },
                { "type", InternalContentType.page }
            },
            requests = new List<SearchRequest>() {
                new SearchRequest() {
                    name = blogParentKey,
                    type = contentName,
                    fields = blogFields,
                    query = "hash = @hash and contentType = @type"
                },
                new SearchRequest() {
                    name = blogPagesKey,
                    type = contentName,
                    fields = blogFields,
                    query = $"parentId in @{blogParentKey}.id and contentType = @type"
                },
                new SearchRequest() {
                    type = userName,
                    fields = "id, username, createDate, avatar",
                    query = $"id in @{blogParentKey}.createUserId or id in @{blogPagesKey}.createUserId"
                }
                //Unfortunately, this will need to be a separate request after receiving the first.
                //Thus, generating the style will be separate.
                /*, new SearchRequest() {
                    name = "blog_styles",
                    type = contentName,
                    fields = blogFields,
                    query = "hash in @blog_parent.values.share_styles" //TODO: this may cause problems
                }*/
            }
        };
    }

    public SearchRequests GetStyleRegenRequest(List<string> styles)
    {
        return new SearchRequests()
        {
            values = new Dictionary<string, object>() {
                { "hashes", styles }
            },
            requests = new List<SearchRequest>() {
                new SearchRequest() {
                    type = contentName,
                    fields = blogFields, //Should also be enough for styles...
                    query = "hash in @hashes"
                },
                new SearchRequest() {
                    type = userName,
                    fields = "id, name, createDate, avatar",
                    query = $"id in @{contentName}.createUserId"
                }
            }
        };
    }

    //Only staging, because we still have to send the new request and get it back...
    protected async Task BlogStaging(string hash, long lastRevisionId, Func<WebSocketRequest, Task> sendFunc, bool force = false)
    {
        logger.LogDebug($"Testing blog {hash} for regeneration. Apparent last revision: {lastRevisionId}, forcing: {force}");

        if(force || await blogGenerator.ShouldRegenBlog(hash, lastRevisionId))
        {
            logger.LogInformation($"Requesting recreate of entire blog '{hash}' (forced: {force})");

            //This will also refresh (unconditionally?) the style
            await sendFunc(new WebSocketRequest()
            {
                id = blogRefreshKey,
                type = requestKey,
                data = GetFullBlogRegenSearchRequest(hash)
            });
        }
        else
        {
            logger.LogInformation($"Blog {hash} up to date, leaving it alone");
        }
    }

    public T ForceCastResult<T>(object item)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item)) ??
            throw new InvalidOperationException($"Can't cast item to {typeof(T)}");
    }

    public List<T> ForceCastResultObjects<T>(GenericSearchResult result, string key, string onBehalf)
    {
        if(!result.objects.ContainsKey(key))
            throw new InvalidOperationException($"No {key} result in {onBehalf}!!");

        return result.objects[key].Select(x => ForceCastResult<T>(x)).ToList();
    }

    protected async Task HandleResponse(WebSocketResponse response, Func<WebSocketRequest, Task> sendFunc)
    {
        logger.LogDebug($"Received response type {response.type}, id {response.id}: {JsonConvert.SerializeObject(response)}");

        if(response.data == null)
        {
            logger.LogInformation($"Null data in response type {response.type} (id: {response.id}), ignoring");
            return;
        }

        if(response.id == initialPrecheckKey)
        {
            var responseData = ((JObject)response.data).ToObject<GenericSearchResult>() ?? 
                throw new InvalidOperationException($"Couldn't convert {initialPrecheckKey} response data to GenericSearchResult");

            var contents = ForceCastResultObjects<ContentView>(responseData, contentName, initialPrecheckKey);
            logger.LogDebug($"Initial_precheck: {contents.Count} potential blogs found");

            //Remove old blogs that are no longer in service, ie blogs on the system that weren't returned in the full check
            blogGenerator.CleanupMissingBlogs(contents.Select(x => x.hash));
            
            //Then go regen all the blogs. Yes, this might be a lot of individual lookups but oh well
            foreach(var blog in contents)
                await BlogStaging(blog.hash, blog.lastRevisionId, sendFunc);
        }
        else if(response.id == blogRefreshKey)
        {
            var responseData = ((JObject)response.data).ToObject<GenericSearchResult>() ?? 
                throw new InvalidOperationException($"Couldn't convert {blogRefreshKey} response data to GenericSearchResult");

            var users = ForceCastResultObjects<UserView>(responseData, userName, blogRefreshKey); 
            var parent = ForceCastResultObjects<ContentView>(responseData, blogParentKey, blogRefreshKey).First();
            var pages = ForceCastResultObjects<ContentView>(responseData, blogPagesKey, blogRefreshKey);

            //Need to go get styles here, it won't be part of the blog generation
            var styles = blogGenerator.GetStylesForParent(parent);

            if(styles.Count > 0)
            {
                await sendFunc(new WebSocketRequest()
                {
                    id = styleRefreshKey,
                    type = requestKey,
                    data = GetStyleRegenRequest(styles)
                });
            }

            //And then blog generation
            await blogGenerator.GenerateFullBlog(parent, pages, users);
        }
        else if(response.id == styleRefreshKey)
        {
            var responseData = ((JObject)response.data).ToObject<GenericSearchResult>() ?? 
                throw new InvalidOperationException($"Couldn't convert {styleRefreshKey} response data to GenericSearchResult");

            var contents = ForceCastResultObjects<ContentView>(responseData, contentName, styleRefreshKey);
            var users = ForceCastResultObjects<UserView>(responseData, userName, styleRefreshKey);

            //Now just regen the contents as styles
            foreach(var style in contents)
            {
                await blogGenerator.GenerateStyle(style, users);
            }
        }

        //Check for the id to know what kind of response it is. Rescan, etc.

        //If you get content inside an event activity type, that's what you use to regen. Regens are two types:
        //full blog regen and style regen. If the content is in the cache and marked a style, go save it again.
        //If the content is a blog (share), then regen that blog. If the PARENT is a blog, regen that too, in that
        //order. I don't know if the order matters too much since the blogs are in separate folders.

        //However, there are other things. Always check the cache, it SHOULD represent the... well, wait. 

        //Don't force on content where it is a share, but force if parent is share (activity)
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var ws = new ClientWebSocket();
                using var ms = new MemoryStream();

                var sender = new Func<WebSocketRequest, Task>((o) => ws.SendObjectAsync<WebSocketRequest>(o, WebSocketMessageType.Text, stoppingToken));

                await ws.ConnectAsync(new Uri($"{wsconfig.WebsocketEndpoint}?token={wsconfig.AnonymousToken}"), stoppingToken);

                //We HAVE to wait for the initial lastId message
                var connectResponse = await ws.ReceiveObjectAsync<WebSocketResponse>(ms, stoppingToken);

                logger.LogInformation($"Websocket connection opened, response: {JsonConvert.SerializeObject(connectResponse)}");

                //Don't worry about styles here, they will be brought in as part of the blog regeneration, which this precheck is pulling
                var precheckRequest = new WebSocketRequest()
                {
                    id = initialPrecheckKey,
                    type = requestKey,
                    data = new SearchRequests()
                    {
                        values = new Dictionary<string, object>() {
                            { "key", "share" },
                            { "value", "true" },
                            { "type", InternalContentType.page }
                        },
                        requests = new List<SearchRequest>() {
                            new SearchRequest() {
                                type = contentName,
                                fields = "id, lastRevisionId, hash, values, contentType",
                                query = "!valuelike(@key, @value) and contentType = @type"
                            }
                        }
                    }
                };

                //First, do the rescan
                logger.LogInformation("Requesting precheck data now...");
                await sender(precheckRequest);

                logger.LogInformation("Beginning listen loop...");

                //Then, just listen
                while(!stoppingToken.IsCancellationRequested)
                {
                    var listenResponse = await ws.ReceiveObjectAsync<WebSocketResponse>(ms, stoppingToken);
                    await HandleResponse(listenResponse, sender);
                }
            }
            catch(Exception ex)
            {
                logger.LogWarning($"Exception broke out of websocket loop: \n{ex}\nWill retry in {wsconfig.ReconnectInterval}...");
                await Task.Delay(wsconfig.ReconnectInterval, stoppingToken);
            }
        }
    }
}
