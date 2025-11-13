using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Attributes;
using Sharp.Shared.Calls;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;

namespace NativeCallExample;

internal unsafe interface IBaseEntityVirtualCall : IVirtualCall<IBaseEntity>
{
    // Same as signature call, but this one uses vtable index instead of signature.
    void* GetDynamicBinding(nint instance, void** result);
}

internal interface IWeaponServiceSignatureCall : ISignatureCall<IWeaponService>
{
    // in gamedata it will be CCSPlayer_WeaponServices::GetWeaponByName
    // WHere does CCSPlayer_WeaponServices comes from? you can F12 (if you use Visual Studio) then see NetClass attribute.
    // this is the class name what we need.
    [AddressKey("GetWeaponByName")]
    nint GetWeaponByName(IWeaponService weaponService, string weaponName);
}

internal class NativeCallExample : IModSharpModule
{
    // How to invoke them is ignored.
    private readonly IBaseEntityVirtualCall _vCall;
    private readonly IWeaponServiceSignatureCall _sigCall;
    public NativeCallExample(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
    {
        // For concept validation, you can directly construct and call.
        _vCall = new BaseEntityVirtualCall(sharedSystem.GetModSharp().GetGameData());
        _sigCall = new WeaponServiceSignatureCall(sharedSystem.GetModSharp().GetGameData());

        // For a real project, you should always use dependency injection!
        var services = new ServiceCollection();
        // You must add this, otherwise native call will fail to construct.
        services.AddSingleton(sharedSystem.GetModSharp().GetGameData());
        services.AddSingleton<BaseEntityVirtualCall>();
        services.AddSingleton<WeaponServiceSignatureCall>();

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<BaseEntityVirtualCall>();
        provider.GetRequiredService<WeaponServiceSignatureCall>();
    }

    public bool Init() => true;

    public void Shutdown()
    {
    }

    public string DisplayName => "Native Call Example";
    public string DisplayAuthor => "ModSharp dev team";
}
