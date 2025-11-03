using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Types;

namespace EntityListener;

public sealed class EntityListener : IModSharpModule, IEntityListener
{
    private readonly ISharedSystem _sharedSystem;

    public EntityListener(ISharedSystem sharedSystem,
        string                          dllPath,
        string                          sharpPath,
        Version                         version,
        IConfiguration                  coreConfiguration,
        bool                            hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // install listener, any class what inherits IEntityListener can be a listener.
        _sharedSystem.GetEntityManager().InstallEntityListener(this);

        return true;
    }

    public void PostInit()
    {
        // tell the modsharp add hooks for specific entity IO
        _sharedSystem.GetEntityManager().HookEntityOutput("func_door", "OnClose");
        _sharedSystem.GetEntityManager().HookEntityInput("func_door", "Close");

        // entity IO can be found in base.fgd
    }

    public void Shutdown()
    {
        // must uninstall the listener in Shutdown
        // otherwise you will get fucked after reloaded.
        _sharedSystem.GetEntityManager().RemoveEntityListener(this);
    }

    // all callbacks are optional to implement, you can just implement what you need.

    public void OnEntityCreated(IBaseEntity entity)
    {
        if (entity.Classname.Equals("cs_gamerules", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("CS Game rules Proxy has been created!");
        }
    }

    public void OnEntityDeleted(IBaseEntity entity)
    {
        if (entity.Classname.Equals("prop_dynamic", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[OnEntityDeleted] prop_dynamic has been deleted.");
        }
    }

    public void OnEntitySpawned(IBaseEntity entity)
    {
        if (entity.Classname.Equals("prop_dynamic", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[OnEntitySpawned] prop_dynamic has been spawned.");
        }
    }

    public void OnEntityFollowed(IBaseEntity entity, IBaseEntity? owner)
    {
        if (entity.Classname.Equals("prop_dynamic", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[OnEntityFollowed] prop_dynamic has been followed.");
        }
    }

    public EHookAction OnEntityFireOutput(IBaseEntity entity, string output, IBaseEntity? activator, float delay)
    {
        if (!entity.Classname.Equals("func_door", StringComparison.OrdinalIgnoreCase))
        {
            return EHookAction.Ignored;
        }

        Console.WriteLine(
            $"[OnEntityFireOutput] {entity.Classname}, output={output}, activator={activator?.Classname}, delay={delay}");

        // you can abort the output event here by returning EHookAction.SkipCallReturnOverride

        return EHookAction.Ignored;
    }

    public EHookAction OnEntityAcceptInput(IBaseEntity entity,
        string                                         input,
        in EntityVariant                               value,
        IBaseEntity?                                   activator,
        IBaseEntity?                                   caller)
    {
        if (!entity.Classname.Equals("func_door", StringComparison.OrdinalIgnoreCase))
        {
            return EHookAction.Ignored;
        }

        Console.WriteLine(
            $"[OnEntityAcceptInput] {entity.Classname}, value={value.AutoCastString()}, activator={activator?.Classname}, caller={activator?.Classname}");

        // you can abort the input here by returning EHookAction.SkipCallReturnOverride

        return EHookAction.Ignored;
    }

    public string DisplayName   => "Entity Listener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    // just write it according to the example.
    int IEntityListener.ListenerVersion => IEntityListener.ApiVersion;

    // the larger the number = the higher the priority; in most cases, 0 is used directly.
    int IEntityListener.ListenerPriority => 0;
}