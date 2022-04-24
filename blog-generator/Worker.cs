using System.Net.WebSockets;
using System.Text.RegularExpressions;
using blog_generator.Configs;
using contentapi.data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace blog_generator;

public class Worker : BackgroundService
{
    private readonly ILogger logger;
    protected WebsocketConfig wsconfig;
    protected StorageConfig storageConfig;
    protected BlogManager blogManager;

    public Worker(ILogger<Worker> logger, WebsocketConfig wsconfig, StorageConfig storageConfig, BlogManager blogManager)
    {
        this.logger = logger;
        this.wsconfig = wsconfig;
        this.storageConfig = storageConfig;
        this.blogManager = blogManager;
    }

    protected async Task HandleResponse(WebSocketResponse response, WebSocket ws)
    {
        logger.LogDebug($"Received: {JsonConvert.SerializeObject(response)}");

        if(response.data == null)
        {
            logger.LogInformation($"Null data in response type {response.type}, ignoring");
            return;
        }

        if(response.id == "initial_precheck")
        {
            var responseData = ((JObject)response.data).ToObject<GenericSearchResult>();
        }

        //Check for the id to know what kind of response it is. Rescan, etc.

        //Data can be converted with JObject ToObject<type>

        //If you get content inside an event activity type, that's what you use to regen. Regens are two types:
        //full blog regen and style regen. If the content is in the cache and marked a style, go save it again.
        //If the content is a blog (share), then regen that blog. If the PARENT is a blog, regen that too, in that
        //order. I don't know if the order matters too much since the blogs are in separate folders.

        //However, there are other things. Always check the cache, it SHOULD represent the... well, wait. 
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
                    await HandleResponse(listenResponse, ws);
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
