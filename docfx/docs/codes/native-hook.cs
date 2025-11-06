using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Hooks;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;

// in this example, we disable runtime marshaling
// IMPORTANT: please check our documentation.
[assembly: DisableRuntimeMarshalling]

namespace HookExample;

public sealed class HookExample : IModSharpModule
{
    public string DisplayName   => "Hook Example";
    public string DisplayAuthor => "Modsharp dev team";

    private readonly ISharedSystem _shared;

    private readonly IVirtualHook _virtualHook;
    private readonly IDetourHook  _detourHook;

    private static unsafe delegate* unmanaged<nint, void>       CCSPlayerPawn_CollisionRulesChangedOriginal;
    private static unsafe delegate* unmanaged<byte*, int, nint> CreateEntityByNameOriginal;

    // Our hook functions (like `HookCCSPlayerPawnCollisionRulesChanged`) must be `static`.
    // This is a requirement of the `[UnmanagedCallersOnly]` attribute, which is necessary for C# methods
    // to be called directly from unmanaged (native) code like the game engine.
    //
    // However, a static method cannot directly access instance members like `_shared` or `_virtualHook`.
    // To solve this, we create a static reference to our single `HookExample` instance, so our
    // static hook functions can then use `_this` to access the instance's members.
    private static HookExample _this = null!;

    public HookExample(ISharedSystem sharedSystem,
        string                       dllPath,
        string                       sharpPath,
        Version                      version,
        IConfiguration               coreConfiguration,
        bool                         hotReload)
    {
        _shared = sharedSystem;

        _this = this;

        // VMT (Virtual Method Table) hooking modifies a specific function pointer in a class's vtable.
        // This means the hook only triggers when that virtual function is called on an instance of that specific class.

        // For example: Hooking PrimaryAttack on CWeaponSSG08 will not affect CWeaponRevolver.
        // Although they both inherit PrimaryAttack from a base weapon class, each has its own
        // separate virtual table, so the hook only applies to CWeaponSSG08.
        _virtualHook = _shared.GetHookManager().CreateVirtualHook();

        // Detour hooking overwrites the first few bytes of a target function in memory to jump to our code.
        // This is the ideal method for hooking non-virtual, static, or global functions.
        // It will intercept *all* calls to that specific function, regardless of where they originate from.
        _detourHook = _shared.GetHookManager().CreateDetourHook();
    }

    public bool Init()
    {
        if (!InstallVmtHook())
        {
            return false;
        }

        if (!InstallDetour())
        {
            _virtualHook.Uninstall();

            return false;
        }

        return true;
    }

    public void Shutdown()
    {
        _virtualHook.Uninstall();

        _detourHook.Uninstall();
        _detourHook.Dispose();
    }

    private unsafe bool InstallVmtHook()
    {
        var vFuncIndex = _shared.GetModSharp().GetGameData().GetVFuncIndex("CBaseEntity::CollisionRulesChanged");

        // We are not passing a C# delegate but a raw function pointer that the native game code can call directly instead.
        // This is solely for high performance, because "marshal" is slow and brings .NET runtime overhead.
        // With `[UnmanagedCallersOnly]` attribute, it tells the C# compiler to generate a direct, native function pointer
        // for the method (`HookCCSPlayerPawnCollisionRulesChanged`) which can be called from native code with minimal overhead,
        // just like any other native function.
        _virtualHook.Prepare("server",
                             "CCSPlayerPawn",
                             vFuncIndex,
                             (nint) (delegate* unmanaged<nint, void>) (&HookCCSPlayerPawnCollisionRulesChanged));

        // You can also prepare the hook by directly finding the virtual table address
        /*
        var vtableAddress = _shared.GetLibraryModuleManager().Server.GetVirtualTableByName("CCSPlayerPawn");
        _virtualHook.Prepare(vtableAddress, vFuncIndex, (nint) (delegate* unmanaged<nint, void>) (&HookCCSPlayerPawnCollisionRulesChanged));
        */

        if (!_virtualHook.Install())
        {
            return false;
        }

        // After installation, the hook manager provides a "trampoline". This is a small piece of executable code
        // that jumps to the original, un-hooked game function. We save its address so we can call it later.
        CCSPlayerPawn_CollisionRulesChangedOriginal = (delegate* unmanaged<nint, void>) _virtualHook.Trampoline;

        return true;
    }

    private unsafe bool InstallDetour()
    {
        // See the explanation in `InstallVmtHook` for why we pass our hooked function like this instead of a C# delegate
        _detourHook.Prepare("CreateEntityByName", (nint) (delegate* unmanaged<byte*, int, nint>) (&HookCreateEntityByName));

        // You can also prepare the hook in this way
        /*
        var createEntityByNameAddress = _shared.GetModSharp().GetGameData().GetAddress("CreateEntityByName");
        _detourHook.Prepare(createEntityByNameAddress, (nint) (delegate* unmanaged<byte*, int, nint>) (&HookCreateEntityByName));
        */

        if (!_detourHook.Install())
        {
            _detourHook.Dispose();

            return false;
        }

        // After installation, the hook manager provides a "trampoline". This is a small piece of executable code
        // that jumps to the original, un-hooked game function. We save its address so we can call it later.
        CreateEntityByNameOriginal = (delegate* unmanaged<byte*, int, nint>) _detourHook.Trampoline;

        return true;
    }

    [UnmanagedCallersOnly] // YOU MUST ADD THIS TO YOUR HOOKED FUNCTION
    private static unsafe void HookCCSPlayerPawnCollisionRulesChanged(nint @this)
    {
        // NOTE: Returning early from this hook without calling the original function will
        // prevent any other modules that hooked this function *after* this one from running.
        // This breaks the hook chain and can cause compatibility issues, not recommended to
        // do so in most cases, unless you are sure that users won't run into this type of issue.

        // Use the EntityManager to wrap the native pointer in a safe, usable C# object.
        if (_this._shared.GetEntityManager().MakeEntityFromPointer<IBaseEntity>(@this) is not { IsValidEntity: true } entity)
        {
            // If the entity is invalid, do nothing.
            return;
        }

        // We hooked `CCSPlayerPawn`, so we expect the entity to be a player pawn.
        // This check is for safety, though it should always pass in this specific hook.
        if (entity.AsPlayerPawn() is not { } pawn)
        {
            CCSPlayerPawn_CollisionRulesChangedOriginal(@this);

            return;
        }

        // The player is on team Spectator, we can stop it from running CollisionRulesChanged
        // because this is never supposed to happen
        if (pawn.Team == CStrikeTeam.Spectator)
        {
            return;
        }

        Console.WriteLine("[HookCCSPlayerPawnCollisionRulesChanged] entity.Classname: {0}", entity.Classname);

        // CRITICAL: Always call the original function unless you want to completely block
        // its functionality. Omitting this call can lead to unexpected behavior or crashes.
        CCSPlayerPawn_CollisionRulesChangedOriginal(@this);
    }

    [UnmanagedCallersOnly] // YOU MUST ADD THIS TO YOUR HOOKED FUNCTION
    private static unsafe nint HookCreateEntityByName(byte* classNamePtr, int forcedIndex)
    {
        // NOTE: Returning early from this hook without calling the original function will
        // prevent any other modules that hooked this function *after* this one from running.
        // This breaks the hook chain and can cause compatibility issues, not recommended to
        // do so in most cases, unless you are sure that users won't run into this type of issue.

        // Convert the C-style string pointer to a C# string for easy comparison.
        var className = Utils.ReadString(classNamePtr);

        // We can prevent the game from creating certain entities
        if (className.Equals("weapon_ak47", StringComparison.OrdinalIgnoreCase))
        {
            // By returning 0 (nullptr), we tell the game that the entity creation failed.
            return 0;
        }

        // The `forcedIndex` argument determines the entity slot. -1 means the game picks one automatically.
        // To showcase modifying arguments, we'll pick a random index if the game didn't specify one.
        // !!This is for demonstration only and can cause conflicts.!!
        if (forcedIndex == -1)
        {
            forcedIndex = Random.Shared.Next(0x1000, 0x7FFF);
        }

        // run the game's CreateEntityByName code with our modified argument
        var entityPtr = CreateEntityByNameOriginal(classNamePtr, forcedIndex);

        // here we can check if the entity is created successfully
        if (entityPtr == nint.Zero)
        {
            Console.WriteLine("[HookCreateEntityByName] Failed to create entity \"{0}\" with forced index \"{1}\"",
                              className,
                              forcedIndex);

            return entityPtr;
        }

        Console.WriteLine(
            "[HookCreateEntityByName] Successfully created entity \"{0}\" with forced index \"{1}\". Address: 0x{2:X}",
            className,
            forcedIndex,
            entityPtr);

        // Or we can do things to certain entities
        if (_this._shared.GetEntityManager().MakeEntityFromPointer<IBaseEntity>(entityPtr) is
        {
            Classname: "weapon_glock",
        } entity)
        {
            // Teleport it right after the creation
            entity.Teleport(new Vector(1337.0f, 1337.0f, 1337.0f));
        }

        return entityPtr;
    }
}
