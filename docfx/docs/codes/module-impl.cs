using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using SharedInterface.Shared;

namespace SharedInterface;

public class SharedModule : IModSharpModule, ISharedModule
{
    private readonly ISharedSystem _sharedSystem;

    public SharedModule(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration? coreConfiguration, bool hotReload)
    {
        _sharedSystem = sharedSystem;
    }

    public bool Init()
    {
        return true;
    }

    public void PostInit()
    {
        // Always register interfaces in PostInit, otherwise you may get fucked up.
        _sharedSystem.GetSharpModuleManager()
            .RegisterSharpModuleInterface<ISharedModule>(this, ISharedModule.Identity, this);
    }

    public void Shutdown()
    {
    }

    public string DisplayName => "Shared Module Example";
    public string DisplayAuthor => "ModSharp dev team";
    
    public void CallMe()
    {
        Console.WriteLine("Hello.");
    }
}