using blog_generator;
using blog_generator.Configs;
using blog_generator.Setup;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        DefaultSetup.AddDefaultServices(services);
        DefaultSetup.AddConfigBinding<PathManagementConfig>(services, hostContext.Configuration);
        DefaultSetup.AddConfigBinding<TemplateConfig>(services, hostContext.Configuration);
        DefaultSetup.AddConfigBinding<WebsocketConfig>(services, hostContext.Configuration);
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
