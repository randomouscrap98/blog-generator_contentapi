using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace blog_generator.Setup;

public static class DefaultSetup
{
    private static bool OneTimeRun = false;
    private static readonly object OneTimeLock = new object();

    public static bool OneTimeSetup()
    {
        lock(OneTimeLock)
        {
            if(OneTimeRun)
                return false;

            //SqlMapper.RemoveTypeMap(typeof(DateTime)); 
            //SqlMapper.AddTypeHandler(typeof(DateTime), new DapperUtcDateTimeHandler());

            //MappingSchema.Default.SetConverter<JToken, DataParameter>(json => new DataParameter { Value = json.ToString() });
            //MappingSchema.Default.SetConverter<string, JToken>(str => JToken.Parse(str));

            return true;
        }
    }

    /// <summary>
    /// Add all default service implementations to the given service container. Can override later of course.
    /// </summary>
    /// <remarks>
    /// To replace services (such as for unit tests), you can do: services.Replace(ServiceDescriptor.Transient<IFoo, FooB>());i
    /// </remarks>
    /// <param name="services"></param>
    public static void AddDefaultServices(IServiceCollection services)
    {
        //Thus, the profiles need to be in the SAME assembly. Later, change this to find at least one of the profiles themselves
        services.AddAutoMapper(typeof(DefaultSetup));

        services.AddSingleton<BlogManager>();
        services.AddSingleton<BlogGenerator>();
    }

    /// <summary>
    /// An easy (default) way to add configs in as "intuitive" of a way as possible. Configs added through this endpoint are SINGLETONS
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static void AddConfigBinding<T>(IServiceCollection services, IConfiguration config) where T : class
    {
        var ct = typeof(T);
        var name = ct.Name;
        services.Configure<T>(config.GetSection(name));
        var generator = new Func<IServiceProvider, T>(p => (p.GetService<IOptionsMonitor<T>>() ?? throw new InvalidOperationException($"Mega config failure on {name}!")).CurrentValue);

        //If it already exists (maybe with default values), replace it. They clearly 
        //actually want it from the config
        if(services.Any(x => x.ServiceType == ct))
            services.Replace(ServiceDescriptor.Singleton<T>(generator));
        else
            services.AddSingleton<T>(generator);
    }
}