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
    private readonly IServiceProvider _provider;

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
        // shutdown all sharp extensions, otherwise you will get fucked after reloaded.
        _provider.ShutdownAllSharpExtensions();
    }

    public string DisplayName   => "Sharp Extensions Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}
