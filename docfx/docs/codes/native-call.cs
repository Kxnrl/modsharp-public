using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Attributes;
using Sharp.Shared.Calls;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

[assembly: DisableRuntimeMarshalling]

namespace NativeCallExample;

internal interface IBaseEntityVirtualCall : IVirtualCall<IBaseEntity>
{
    // Resolves the current CBaseEntity::CollisionRulesChanged vtable index from GameData.
    // The current virtual-call generator requires the first parameter to be named "instance".
    void CollisionRulesChanged(IBaseEntity instance);
}

internal interface IBaseEntitySignatureCall : ISignatureCall<IBaseEntity>
{
    // IBaseEntity's NetClass is CBaseEntity. AddressKey overrides the METHOD part of the key.
    [AddressKey("SetGravityScale")]
    void SetGravity(IBaseEntity entity, float scale);
}

// Without a generic native type, AddressKey must contain the full GameData key.
internal interface IUnscopedEntitySignatureCall : ISignatureCall
{
    [AddressKey("CBaseEntity::SetGravityScale")]
    void SetGravity(IBaseEntity entity, float scale);

    [AddressKey("CBaseEntity::SetGravityScale")]
    void SetGravityUsingPointer(nint entity, float scale);
}

public sealed class NativeCallExample : IModSharpModule
{
    private readonly ISharedSystem _shared;
    private readonly ServiceProvider _provider;
    private readonly IBaseEntityVirtualCall _virtualCall;
    private readonly IBaseEntitySignatureCall _signatureCall;

    public NativeCallExample(ISharedSystem sharedSystem,
        string                             dllPath,
        string                             sharpPath,
        Version                            version,
        IConfiguration                     coreConfiguration,
        bool                               hotReload)
    {
        _shared = sharedSystem;
        var services = new ServiceCollection();
        // The SDK generator emits BaseEntityVirtualCall and BaseEntitySignatureCall.
        services.AddSingleton(sharedSystem.GetModSharp().GetGameData());
        services.AddSingleton<IBaseEntityVirtualCall, BaseEntityVirtualCall>();
        services.AddSingleton<IBaseEntitySignatureCall, BaseEntitySignatureCall>();
        _provider = services.BuildServiceProvider();
        // Resolve the interface types that were registered, not their implementation types.
        _virtualCall = _provider.GetRequiredService<IBaseEntityVirtualCall>();
        _signatureCall = _provider.GetRequiredService<IBaseEntitySignatureCall>();
    }

    public bool Init()
    {
        _shared.GetClientManager().InstallCommandCallback("native_gravity", OnGravityCommand);
        return true;
    }

    // .native_gravity or ms_native_gravity: set your pawn's gravity scale to 0.5.
    private ECommandAction OnGravityCommand(IGameClient client, StringCommand command)
    {
        if (client.GetPlayerController()?.GetPlayerPawn() is { IsAlive: true } pawn)
        {
            _signatureCall.SetGravity(pawn, 0.5f);
            _virtualCall.CollisionRulesChanged(pawn);
        }

        return ECommandAction.Stopped;
    }

    public void Shutdown()
    {
        _shared.GetClientManager().RemoveCommandCallback("native_gravity", OnGravityCommand);
        _provider.Dispose();
    }

    public string DisplayName   => "Native Call Example";
    public string DisplayAuthor => "ModSharp dev team";
}
