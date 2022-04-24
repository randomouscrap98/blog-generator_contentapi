using System.Net.WebSockets;
using System.Text.RegularExpressions;
using blog_generator.Configs;
using contentapi.data;
using Newtonsoft.Json;

namespace blog_generator;

//public class RevisionData
//{
//    public long LastRevisionId {get;set;} = 0;
//    public bool IsStyle {get;set;} = false;
//}

public class Worker : BackgroundService
{
    private readonly ILogger logger;
    protected WebsocketConfig wsconfig;
    protected StorageConfig storageConfig;
    protected TemplateConfig templateConfig;

    //protected Dictionary<string, RevisionData> cachedRevisions = new Dictionary<string, RevisionData>();

    public Worker(ILogger<Worker> logger, WebsocketConfig wsconfig, StorageConfig storageConfig, TemplateConfig templateConfig)
    {
        this.logger = logger;
        this.wsconfig = wsconfig;
        this.storageConfig = storageConfig;
        this.templateConfig = templateConfig;
    }

    //protected async Task<Dictionary<string, RevisionData>> LoadCachedRevisions()
    //{
    //    if(File.Exists(storageConfig.RevisionsFile))
    //    {
    //        logger.LogInformation($"Reading cached revisions from file {storageConfig.RevisionsFile}");
    //        var result = JsonConvert.DeserializeObject<Dictionary<string, RevisionData>>(await File.ReadAllTextAsync(storageConfig.RevisionsFile, System.Text.Encoding.UTF8))
    //            ?? throw new InvalidOperationException($"Couldn't deserialize cached revisions from {storageConfig.RevisionsFile}!");
    //        logger.LogInformation($"Found {result.Count} revisions");
    //        return result;
    //    }
    //    else
    //    {
    //        return new Dictionary<string, RevisionData>();
    //    }
    //}

    //protected Task SaveCachedRevisions(Dictionary<string, RevisionData> cachedRevisions)
    //{
    //    return File.WriteAllTextAsync(storageConfig.RevisionsFile, JsonConvert.SerializeObject(cachedRevisions));
    //}

    protected Task HandleResponse(WebSocketResponse response)
    {
        logger.LogDebug($"Received: {JsonConvert.SerializeObject(response)}");

        //Check for the id to know what kind of response it is. Rescan, etc.

        //Data can be converted with JObject ToObject<type>

        //If you get content inside an event activity type, that's what you use to regen. Regens are two types:
        //full blog regen and style regen. If the content is in the cache and marked a style, go save it again.
        //If the content is a blog (share), then regen that blog. If the PARENT is a blog, regen that too, in that
        //order. I don't know if the order matters too much since the blogs are in separate folders.

        //However, there are other things. Always check the cache, it SHOULD represent the... well, wait. 
        return Task.CompletedTask;
    }

    protected string StylePath(string hash) => Path.Join(templateConfig.StylesFolder, $"{hash}.css");
    protected bool StyleExists(string hash) => File.Exists(StylePath(hash));
    protected string BlogMainPath(string hash) => Path.Join(templateConfig.BlogFolder, hash, "index.html");
    protected string BlogPathPath(string hash, string pageHash) => Path.Join(templateConfig.BlogFolder, hash, $"{pageHash}.html");
    protected bool BlogMainExists(string hash) => File.Exists(BlogMainPath(hash));

    protected async Task<bool> ShouldRegenStyle(string hash, long revisionId)
    {
        if(!StyleExists(hash))
            return true;

        return !Regex.IsMatch(await File.ReadAllTextAsync(StylePath(hash)), @$"^/\*{revisionId}\*/");
    }

    protected async Task<bool> ShouldRegenBlog(string hash, long revisionId)
    {
        if(!BlogMainExists(hash))
            return true;

        var lines = await File.ReadAllLinesAsync(BlogMainPath(hash));

        //First line doctype, next html
        return !Regex.IsMatch(lines[2], @$"^<!--{revisionId}-->");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            //Stuff like this, we need to fail immediately, it should break out of all the work etc
            //cachedRevisions = await LoadCachedRevisions();

            try
            {
                using var ws = new ClientWebSocket();
                using var ms = new MemoryStream();

                await ws.ConnectAsync(new Uri($"{wsconfig.WebsocketEndpoint}?token={wsconfig.AnonymousToken}"), stoppingToken);

                //We HAVE to wait for the initial lastId message
                var connectResponse = await ws.ReceiveObjectAsync<WebSocketResponse>(ms, stoppingToken);

                logger.LogInformation($"Websocket connection opened, response: {JsonConvert.SerializeObject(connectResponse)}");

                //Don't worry about styles here, they will be brought in as part of the blog regeneration, which this precheck is pulling
                var precheckRequest = new WebSocketRequest()
                {
                    id = "initial_precheck",
                    type = "request",
                    data = new SearchRequests()
                    {
                        values = new Dictionary<string, object>() {
                            { "key", "share" },
                            { "value", "true" }
                        },
                        requests = new List<SearchRequest>() {
                            new SearchRequest() {
                                type = nameof(RequestType.content),
                                fields = "id, lastRevisionId, hash, values",
                                query = "!valuelike(@key, @value)"
                            }
                        }
                    }
                };

                //First, do the rescan
                logger.LogInformation("Requesting precheck data now...");
                await ws.SendObjectAsync(precheckRequest, WebSocketMessageType.Text, stoppingToken);

                logger.LogInformation("Beginning listen loop...");

                //Then, just listen
                while(!stoppingToken.IsCancellationRequested)
                {
                    var listenResponse = await ws.ReceiveObjectAsync<WebSocketResponse>(ms, stoppingToken);
                    await HandleResponse(listenResponse);
                }
            }
            catch(Exception ex)
            {
                logger.LogWarning($"Exception broke out of websocket loop: {ex}");
                await Task.Delay(wsconfig.ReconnectInterval, stoppingToken);
            }
        }
    }
}
