using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Extensions.EntityHookManager;
using Sharp.Extensions.GameEventManager;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace SharpExtensions;

// ReSharper disable once UnusedMember.Global
internal class SharpExtensions : IModSharpModule
{
    private readonly IServiceProvider _provider;

    public SharpExtensions(ISharedSystem sharedSystem, string? dllPath, string? sharpPath, Version? version, IConfiguration? coreConfiguration, bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);
        var services = new ServiceCollection();
        services.AddSingleton(sharedSystem);
        services.AddEntityHookManager();
        services.AddGameEventManager();

        _provider = services.BuildServiceProvider();
    }

    public bool Init()
    {
        _provider.LoadAllSharpExtensions();
        return true;
    }

    public void Shutdown()
    {
        _provider.ShutdownAllSharpExtensions();
    }

    public string DisplayName => "Sharp Extensions Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}