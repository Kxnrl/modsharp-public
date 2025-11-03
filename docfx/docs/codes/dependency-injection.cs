using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;

namespace DependencyInjection;

public sealed class DependencyInjection : IModSharpModule
{
    private readonly IServiceProvider _provider;

    public DependencyInjection(ISharedSystem sharedSystem,
        string                               dllPath,
        string                               sharpPath,
        Version                              version,
        IConfiguration                       coreConfiguration,
        bool                                 hotReload)
    {
        var services = new ServiceCollection();

        // add services you want to use via dependency injection here
        services.AddSingleton(sharedSystem);
        services.AddSingleton(coreConfiguration);

        // build service provider
        _provider = services.BuildServiceProvider();
    }

    public bool Init()
    {
        // resolve services what you need
        var sharedSystem = _provider.GetRequiredService<ISharedSystem>();
        sharedSystem.GetModSharp().LogMessage("Hello World!");

        return true;
    }

    public void Shutdown()
    {
    }

    public string DisplayName   => "Dependency Injection Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}
