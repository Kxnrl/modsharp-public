using System;
using Microsoft.Extensions.Configuration;
using SharedInterface.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;

namespace UseSharedModule;

public sealed class UseSharedModule : IModSharpModule
{
    // The provider project's AssemblyName, not its module class name.
    private const string ProviderAssemblyName = "SharedInterface";

    private readonly ISharpModuleManager _modules;
    private IModSharpModuleInterface<IMySharedModule>? _cachedInterface;

    public UseSharedModule(ISharedSystem sharedSystem,
        string                           dllPath,
        string                           sharpPath,
        Version                          version,
        IConfiguration                   coreConfiguration,
        bool                             hotReload)
        => _modules = sharedSystem.GetSharpModuleManager();

    public bool Init()
        => true;

    public void OnAllModulesLoaded()
        => ResolveAndCall();

    public void OnLibraryConnected(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            ResolveAndCall();
        }
    }

    public void OnLibraryDisconnect(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _cachedInterface = null;
        }
    }

    private void ResolveAndCall()
    {
        // Cache the wrapper, never the provider instance across unload/reload.
        if (_cachedInterface?.Instance is not null)
        {
            return;
        }

        _cachedInterface = _modules.GetOptionalSharpModuleInterface<IMySharedModule>(IMySharedModule.Identity);
        _cachedInterface?.Instance?.CallMe();
    }

    public void Shutdown()
        => _cachedInterface = null;

    public string DisplayName   => "Use Shared Module Example";
    public string DisplayAuthor => "ModSharp dev team";
}
