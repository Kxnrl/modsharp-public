using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Hooks;
using Sharp.Shared.Utilities;

[assembly: DisableRuntimeMarshalling]

namespace HookExample;

public sealed class HookExample : IModSharpModule
{
    private readonly ISharedSystem _shared;
    private readonly IVirtualHook _virtualHook;
    private readonly IDetourHook _detourHook;

    private static HookExample _instance = null!;
    private static unsafe delegate* unmanaged<nint, void> _collisionRulesChangedOriginal;
    private static unsafe delegate* unmanaged<byte*, int, nint> _createEntityByNameOriginal;

    public HookExample(ISharedSystem sharedSystem,
        string                       dllPath,
        string                       sharpPath,
        Version                      version,
        IConfiguration               coreConfiguration,
        bool                         hotReload)
    {
        _shared = sharedSystem;
        _instance = this;
        _virtualHook = _shared.GetHookManager().CreateVirtualHook();
        _detourHook = _shared.GetHookManager().CreateDetourHook();
    }

    public unsafe bool Init()
    {
        // VMT hook: only calls through CCSPlayerPawn's vtable slot are intercepted.
        var index = _shared.GetModSharp().GetGameData().GetVFuncIndex("CBaseEntity::CollisionRulesChanged");
        _virtualHook.Prepare("server", "CCSPlayerPawn", index,
            (nint) (delegate* unmanaged<nint, void>) &HookCollisionRulesChanged);
        if (!_virtualHook.Install())
        {
            return false;
        }

        _collisionRulesChangedOriginal = (delegate* unmanaged<nint, void>) _virtualHook.Trampoline;

        // Detour hook: all calls to this function body are intercepted, not just vtable calls.
        _detourHook.Prepare("CreateEntityByName",
            (nint) (delegate* unmanaged<byte*, int, nint>) &HookCreateEntityByName);
        if (!_detourHook.Install())
        {
            // ModSharp calls Shutdown when Init returns false, including partial initialization.
            return false;
        }

        _createEntityByNameOriginal = (delegate* unmanaged<byte*, int, nint>) _detourHook.Trampoline;
        return true;
    }

    public void Shutdown()
    {
        // Uninstall in reverse installation order; detour objects additionally require Dispose.
        _detourHook.Uninstall();
        _virtualHook.Uninstall();
        _detourHook.Dispose();
        _instance = null!;
    }

    [UnmanagedCallersOnly]
    private static unsafe void HookCollisionRulesChanged(nint entityPointer)
    {
        // Never allow managed exceptions to escape an unmanaged callback.
        try
        {
            if (_instance._shared.GetEntityManager().MakeEntityFromPointer<IBaseEntity>(entityPointer)
                is { IsValidEntity: true } entity)
            {
                Console.WriteLine($"[CollisionRulesChanged] {entity.Classname}");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
        }

        // Preserve both the original behavior and other hooks in the chain.
        _collisionRulesChangedOriginal(entityPointer);
    }

    [UnmanagedCallersOnly]
    private static unsafe nint HookCreateEntityByName(byte* classNamePointer, int forcedIndex)
    {
        try
        {
            Console.WriteLine($"[CreateEntityByName] {Utils.ReadString(classNamePointer)}, index={forcedIndex}");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
        }

        // -1 lets the engine choose a free index. Do not replace it with a random entity index.
        return _createEntityByNameOriginal(classNamePointer, forcedIndex);
    }

    public string DisplayName   => "Native Hook Example";
    public string DisplayAuthor => "ModSharp dev team";
}
