using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Attributes;
using Sharp.Shared.Calls;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

[assembly: DisableRuntimeMarshalling]

namespace NativeCallExample;

internal unsafe interface IBaseEntityVirtualCall : IVirtualCall<IBaseEntity>
{
    // Same as signature call, but this one uses vtable index instead of signature.
    bool PassesDamageFilter(IBaseEntity instance, TakeDamageInfo* pInfo);
}

internal interface IWeaponServiceSignatureCall : ISignatureCall<IWeaponService>
{
    // in gamedata it will be CCSPlayer_WeaponServices::GetWeaponByName
    // Where does CCSPlayer_WeaponServices comes from? you can F12 (if you use Visual Studio) then see NetClass attribute.
    // this is the class name what we need.
    // if your function name in gamedata mismatch with method name, you can use AddressKey to specify it.
    // by default, it always uses {NetClass}::{MethodName} to find the signature.
    [AddressKey("GetWeaponByName")]
    nint GetWeaponByName(IWeaponService weaponService, string weaponName);
}

// you also can define without type parameter
internal interface IWeaponServiceSignatureCallWithoutType : ISignatureCall
{
    [AddressKey("CCSPlayer_WeaponServices::GetWeaponByName")]
    nint GetWeaponByName(IWeaponService weaponService, string weaponName);

    [AddressKey("CCSPlayer_WeaponServices::GetWeaponByName")]
    nint GetWeaponByNameUsingPointer(nint weaponService, string weaponName);
}

internal class NativeCallExample : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;

    // How to invoke them is ignored.
    private readonly IBaseEntityVirtualCall      _vCall;
    private readonly IWeaponServiceSignatureCall _sigCall;

    public NativeCallExample(ISharedSystem sharedSystem,
        string                             dllPath,
        string                             sharpPath,
        Version                            version,
        IConfiguration                     coreConfiguration,
        bool                               hotReload)
    {
        _sharedSystem = sharedSystem;

        sharedSystem.GetModSharp().GetGameData().Register("native-call.games");

        // For concept validation, you can directly construct and call.
        _vCall   = new BaseEntityVirtualCall(sharedSystem.GetModSharp().GetGameData());
        _sigCall = new WeaponServiceSignatureCall(sharedSystem.GetModSharp().GetGameData());

        // For a real project, you should always use dependency injection!
        var services = new ServiceCollection();

        // You must add GameData, otherwise native call will fail to construct.
        services.AddSingleton(sharedSystem.GetModSharp().GetGameData());

        services.AddSingleton<IBaseEntityVirtualCall, BaseEntityVirtualCall>();
        services.AddSingleton<IWeaponServiceSignatureCall, WeaponServiceSignatureCall>();

        var provider = services.BuildServiceProvider();
        _vCall   = provider.GetRequiredService<BaseEntityVirtualCall>();
        _sigCall = provider.GetRequiredService<WeaponServiceSignatureCall>();
    }

    public bool Init()
        => true;

    public void Shutdown()
    {
        _sharedSystem.GetModSharp().GetGameData().Register("native-call.games");
    }

    private IBaseWeapon? GetWeaponByName(IPlayerPawn pawn)
    {
        if (pawn.GetWeaponService() is not { } ws)
        {
            return null;
        }

        var pWeapon = _sigCall.GetWeaponByName(ws, "weapon_ak47");

        return _sharedSystem.GetModSharp().CreateNativeObject<IBaseWeapon>(pWeapon);
    }

    private unsafe bool PassesDamageFilter(IBaseEntity entity, in TakeDamageInfo info)
    {
        fixed (TakeDamageInfo* pInfo = &info)
        {
            return _vCall.PassesDamageFilter(entity, pInfo);
        }
    }

    public string DisplayName   => "Native Call Example";
    public string DisplayAuthor => "ModSharp dev team";
}
