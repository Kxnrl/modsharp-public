using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Extensions.EntityHookManager;
using Sharp.Extensions.GameEventManager;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace SharpExtensions;

public sealed class SharpExtensions : IModSharpModule
{
    private readonly ServiceProvider _provider;

    public SharpExtensions(ISharedSystem sharedSystem,
        string                           dllPath,
        string                           sharpPath,
        Version                          version,
        IConfiguration                   coreConfiguration,
        bool                             hotReload)
    {
        var services = new ServiceCollection();
        services.AddSingleton(sharedSystem);
        services.AddEntityHookManager();
        services.AddGameEventManager();

        _provider = services.BuildServiceProvider();
    }

    public bool Init()
        => true;

    public void PostInit()
    {
        // load all sharp extensions, otherwise they won't work.
        _provider.LoadAllSharpExtensions();
    }

    public void Shutdown()
    {
        // Shut extensions down before disposing the container to unregister their callbacks.
        _provider.ShutdownAllSharpExtensions();
        _provider.Dispose();
    }

    public string DisplayName   => "Sharp Extensions Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}
