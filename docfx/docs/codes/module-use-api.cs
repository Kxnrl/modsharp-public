using System;
using Microsoft.Extensions.Configuration;
using SharedInterface.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;

namespace UseSharedModule;

public class UseSharedModule : IModSharpModule
{
    private readonly ISharpModuleManager _modules;

    public UseSharedModule(ISharedSystem sharedSystem,
        string                           dllPath,
        string                           sharpPath,
        Version                          version,
        IConfiguration?                  coreConfiguration,
        bool                             hotReload)
        => _modules = sharedSystem.GetSharpModuleManager();

    public bool Init()
        => true;

    public void Shutdown()
    {
    }

    private void WhatEventYouWantToCall()
    {
        // call interface here

        GetInterface()?.CallMe();

        if (GetInterface() is { } api)
        {
            api.CallMe();
        }
    }

    private IModSharpModuleInterface<IMySharedModule>? _cachedInterface;

    // this may have performance issue if you call it too frequently,

    private IMySharedModule? GetInterface()
    {
        // you can NOT cache the Instance directly!
        // cache `IModSharpModuleInterface<T>` instead.
        if (_cachedInterface?.Instance is null)
        {
            _cachedInterface = _modules.GetOptionalSharpModuleInterface<IMySharedModule>(IMySharedModule.Identity);
        }

        return _cachedInterface?.Instance;
    }

    // the bast way is cache with 'OnAllModulesLoaded'/'OnLibraryConnected' and clear with 'OnLibraryDisconnect' manually
    // OnAllModulesLoaded called when after you have load and others modules are loaded, even you have reloaded.
    // OnLibraryConnected called when a module is loaded and ready to provide an interface.
    // OnLibraryDisconnect called when a module is unloaded and interface no longer available.
    // see below:

    // cache interface here
    public void OnAllModulesLoaded()
    {
        _cachedInterface = _modules.GetOptionalSharpModuleInterface<IMySharedModule>(IMySharedModule.Identity);

        // call your interface method
        if (_cachedInterface?.Instance is { } instance)
        {
            instance.CallMe();
        }
    }

    // it will be called when SharedModule is loaded/reload
    public void OnLibraryConnected(string name)
    {
        // name is the Assembly name of the provider
        if (!name.Equals("SharedModule"))
        {
            return;
        }

        _cachedInterface = _modules.GetRequiredSharpModuleInterface<IMySharedModule>(IMySharedModule.Identity);

        // call your interface method
        if (_cachedInterface?.Instance is { } instance)
        {
            instance.CallMe();
        }
    }

    public string DisplayName   => "Use Shared Module Example";
    public string DisplayAuthor => "ModSharp dev team";
}
