using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Modules.AdminOperations.Services;
using Sharp.Shared;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using static CUserMessage_DllStatus.Types;

namespace Sharp.Modules.AdminOperations;

// ReSharper disable once UnusedMember.Global
internal class AdminOperations : IModSharpModule
{
    string IModSharpModule.DisplayName => "AdminOperations";
    string IModSharpModule.DisplayAuthor => "1";

    private readonly IServiceProvider _provider;
    private readonly ILogger<AdminOperations> _logger;

    // <code>public YourModule(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)</code>
    public AdminOperations(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
    {
        var bridge = new InterfaceBridge(sharedSystem, dllPath, sharpPath, version, coreConfiguration, hotReload);
        var services = new ServiceCollection();
        services.AddSingleton(bridge);
        services.AddSingleton(sharedSystem.GetLoggerFactory());
        services.AddLogging(p => p.ClearProviders());
        services.AddSingleton<IAdminOperationStorageService, AdminOperationStorageService>();
        services.AddSingleton<IBanService, BanService>();

        _provider = services.BuildServiceProvider();
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<AdminOperations>();
    }

    public bool Init()
    {
        _provider.CallInit<IAdminService>(e => { _logger.LogError(e, "An error occurred when calling Init."); });

        return true;
    }

    public void PostInit()
    {
        _provider.CallPostInit<IAdminService>(e => { _logger.LogError(e, "An error occurred when calling PostInit."); });

    }

    public void OnAllModulesLoaded()
    {
        _provider.CallAllModulesLoaded<IAdminService>(e =>
        {
            _logger.LogError(e, "An error occurred when calling OnAllModulesLoaded.");
        });
    }

    public void OnLibraryConnected(string name)
    {
        _provider.CallLibraryConnected<IAdminService>(name,
            e => { _logger.LogError(e, "An error occurred when calling OnLibraryConnected."); });
    }

    public void OnLibraryDisconnect(string name)
    {
        _provider.CallLibraryDisconnect<IAdminService>(name,
            e => { _logger.LogError(e, "An error occurred when calling OnLibraryDisconnect.."); });
    }

    public void Shutdown()
    {
        _provider.CallShutdown<IAdminService>(e =>
        {
            _logger.LogError(e, "An error occurred when calling OnLibraryDisconnect..");
        });
    }
}

internal static class ServiceProviderExtensions
{
    // 修正扩展方法写法：static方法 + this修饰第一个参数
    public static void CallInit<T>(this IServiceProvider self, Action<Exception> onError)
        where T : IAdminService
    {
        self.CallInit<T>(type => type is { IsInterface: true, ContainsGenericParameters: false }, onError);
    }

    public static void CallInit<T>(this IServiceProvider self, Func<Type, bool> typeFilterPredicate, Action<Exception> onError)
        where T : IAdminService
    {
        foreach (var service in self.GetAllServices<T>(typeFilterPredicate))
        {
            try
            {
                service.OnInit();
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
    }

    public static void CallPostInit<T>(this IServiceProvider self, Action<Exception> onError)
        where T : IAdminService
    {
        self.CallPostInit<T>(type => type is { IsInterface: true, ContainsGenericParameters: false }, onError);
    }

    public static void CallPostInit<T>(this IServiceProvider self, Func<Type, bool> typeFilterPredicate, Action<Exception> onError)
        where T : IAdminService
    {
        foreach (var service in self.GetAllServices<T>(typeFilterPredicate))
        {
            try
            {
                service.OnPostInit();
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
    }

    public static void CallAllModulesLoaded<T>(this IServiceProvider self, Action<Exception> onError)
        where T : IAdminService
    {
        self.CallAllModulesLoaded<T>(type => type is { IsInterface: true, ContainsGenericParameters: false },
            onError);
    }

    public static void CallAllModulesLoaded<T>(this IServiceProvider self, Func<Type, bool> typeFilterPredicate, Action<Exception> onError)
        where T : IAdminService
    {
        foreach (var service in self.GetAllServices<T>(typeFilterPredicate))
        {
            try
            {
                service.OnAllModulesLoaded();
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
    }

    public static void CallLibraryConnected<T>(this IServiceProvider self, string libName, Action<Exception> onError)
        where T : IAdminService
    {
        self.CallLibraryConnected<T>(libName,
            type => type is { IsInterface: true, ContainsGenericParameters: false }, onError);
    }

    public static void CallLibraryConnected<T>(this IServiceProvider self, string libName, Func<Type, bool> typeFilterPredicate,
        Action<Exception> onError)
        where T : IAdminService
    {
        foreach (var service in self.GetAllServices<T>(typeFilterPredicate))
        {
            try
            {
                service.OnLibraryConnected(libName);
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
    }

    public static void CallLibraryDisconnect<T>(this IServiceProvider self, string libName, Action<Exception> onError)
        where T : IAdminService
    {
        self.CallLibraryDisconnect<T>(libName,
            type => type is { IsInterface: true, ContainsGenericParameters: false }, onError);
    }

    public static void CallLibraryDisconnect<T>(this IServiceProvider self, string libName, Func<Type, bool> typeFilterPredicate,
        Action<Exception> onError)
        where T : IAdminService
    {
        foreach (var service in self.GetAllServices<T>(typeFilterPredicate))
        {
            try
            {
                service.OnLibraryDisconnect(libName);
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
    }

    public static void CallShutdown<T>(this IServiceProvider self, Action<Exception> onError)
        where T : IAdminService
    {
        self.CallShutdown<T>(type => type is { IsInterface: true, ContainsGenericParameters: false }, onError);
    }

    public static void CallShutdown<T>(this IServiceProvider self, Func<Type, bool> typeFilterPredicate, Action<Exception> onError)
        where T : IAdminService
    {
        foreach (var service in self.GetAllServices<T>(typeFilterPredicate))
        {
            try
            {
                service.OnShutdown();
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
    }

    // https://stackoverflow.com/questions/69836192/argumentexception-optionsmanager-cant-be-converted-to-service-type-ioptions
    private static IEnumerable<T> GetAllServices<T>(this IServiceProvider provider, Func<Type, bool> predicate)
    {
        var site = typeof(ServiceProvider)
            .GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(provider)!;
        var desc = site
            .GetType()
            .GetField("_descriptors", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(site) as ServiceDescriptor[];
        return desc!.Select(s => predicate(s.ServiceType) ? provider.GetRequiredService(s.ServiceType) : null)
            .OfType<T>();
    }
}