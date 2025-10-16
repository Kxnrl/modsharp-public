using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;

namespace DependencyInjection;

// Or you can use this annotation if you have ReSharper.
// [UsedImplicitly]
// Recommend you use this comment.
// ReSharper disable once UnusedMember.Global
internal class DependencyInjection : IModSharpModule
{
    private readonly IServiceProvider _provider;
    public DependencyInjection(ISharedSystem sharedSystem, string? dllPath, string? sharpPath, Version? version, IConfiguration? coreConfiguration, bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);
        var services = new ServiceCollection();
        services.AddSingleton(sharedSystem);
        _provider = services.BuildServiceProvider();
    }

    public bool Init()
    {
        var sharedSystem = _provider.GetRequiredService<ISharedSystem>();
        sharedSystem.GetModSharp().LogMessage("Hello World!");
        return true;
    }

    public void Shutdown()
    {
    }

    public string DisplayName => "Dependency Injection Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}