namespace blog_generator;

public class Worker : BackgroundService
{
    private readonly ILogger logger;
    protected GeneralConfig config;

    public Worker(ILogger<Worker> logger, GeneralConfig config)
    {
        this.logger = logger;
        this.config = config;
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
            logger.LogInformation($"Worker running at: {DateTimeOffset.Now}, connecting to: {config.WebsocketEndpoint}?token={config.AnonymousToken}");
            await Task.Delay(1000, stoppingToken);
        }
    }
}
