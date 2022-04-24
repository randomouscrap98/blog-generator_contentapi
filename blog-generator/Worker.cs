using blog_generator.Configs;

namespace blog_generator;

public class Worker : BackgroundService
{
    private readonly ILogger logger;
    protected WebsocketConfig wsconfig;

    public Worker(ILogger<Worker> logger, WebsocketConfig wsconfig)
    {
        this.logger = logger;
        this.wsconfig = wsconfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long lastId = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

            }
            catch(Exception ex)
            {
                logger.LogWarning($"Exception broke out of websocket loop: {ex}");
            }
            logger.LogInformation($"Worker running at: {DateTimeOffset.Now}, connecting to: {wsconfig.WebsocketEndpoint}?token={wsconfig.AnonymousToken}");
            await Task.Delay(1000, stoppingToken);
        }
    }
}
