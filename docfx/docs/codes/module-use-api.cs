using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using SharedInterface.Shared;

namespace UseSharedModule;

public class UseSharedModule : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;

    public UseSharedModule(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration? coreConfiguration, bool hotReload)
    {
        _sharedSystem = sharedSystem;
    }

    public bool Init()
    {
        return true;
    }


    public void Shutdown()
    {
    }

    private IModSharpModuleInterface<ISharedModule>? _cachedInterface;

    // this may have performance issue if you call it too frequently,

    private ISharedModule? GetInterface()
    {
        if (_cachedInterface?.Instance is null)
        {
            _cachedInterface = _sharedSystem.GetSharpModuleManager().GetOptionalSharpModuleInterface<ISharedModule>(ISharedModule.Identity);

            // set the listener of cookie load event
            if (_cachedInterface?.Instance is { } instance)
            {
                instance.CallMe();
            }
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
        _cachedInterface = _sharedSystem.GetSharpModuleManager().GetOptionalSharpModuleInterface<ISharedModule>(ISharedModule.Identity);

        // set the listener of cookie load event
        if (_cachedInterface?.Instance is { } instance)
        {
            instance.CallMe();
        }
    }

    // it will be called when SharedModule is loaded
    public void OnLibraryConnected(string name)
    {
        // match name
        if (!name.Equals("SharedModule"))
        {
            return;
        }
        _cachedInterface = _sharedSystem.GetSharpModuleManager().GetRequiredSharpModuleInterface<ISharedModule>(ISharedModule.Identity);

        // set the listener of cookie load event
        if (_cachedInterface?.Instance is { } instance)
        {
            instance.CallMe();
        }
    }


    public string DisplayName => "Use Shared Module Example";
    public string DisplayAuthor => "ModSharp dev team";
}