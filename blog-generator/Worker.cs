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
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation($"Worker running at: {DateTimeOffset.Now}, templates in: {config.TemplatesFolder}");
            await Task.Delay(1000, stoppingToken);
        }
    }
}
