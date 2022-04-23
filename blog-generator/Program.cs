using blog_generator;
using blog_generator.Setup;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        DefaultSetup.AddDefaultServices(services);
        DefaultSetup.AddConfigBinding<GeneralConfig>(services, hostContext.Configuration);
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
