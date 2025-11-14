using System;
using Microsoft.Extensions.Configuration;
using SharedInterface.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;

namespace SharedInterface;

public class MySharedModule : IModSharpModule, IMySharedModule
{
    private readonly ISharpModuleManager  _modules;
    private readonly MySecondSharedModule _mySecondSharedModule;

    public MySharedModule(ISharedSystem sharedSystem,
        string                          dllPath,
        string                          sharpPath,
        Version                         version,
        IConfiguration?                 coreConfiguration,
        bool                            hotReload)
    {
        _modules              = sharedSystem.GetSharpModuleManager();
        _mySecondSharedModule = new MySecondSharedModule();
    }

    public bool Init()
        => true;

    public void PostInit()
    {
        // Always register interfaces in (or after) PostInit, otherwise you may get fucked up.
        _modules.RegisterSharpModuleInterface<IMySharedModule>(this, IMySharedModule.Identity, this);

        _modules.RegisterSharpModuleInterface<IMySecondSharedModule>(this,
                                                                     IMySecondSharedModule.Identity,
                                                                     _mySecondSharedModule);
    }

    public void Shutdown()
    {
        // you don't need to unregister interfaces, they will be unregistered automatically
    }

    public void CallMe()
    {
        Console.WriteLine("Hello.");
    }

    public string DisplayName   => "Shared Module Example";
    public string DisplayAuthor => "ModSharp dev team";
}

internal sealed class MySecondSharedModule : IMySecondSharedModule
{
    public void CallYou()
    {
        // Do you want to do
    }
}