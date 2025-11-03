using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace SoundEmitterExample;

public sealed class SoundEmitter : IModSharpModule
{
    private readonly ISharedSystem _shared;

    public SoundEmitter(ISharedSystem sharedSystem,
        string                        dllPath,
        string                        sharpPath,
        Version                       version,
        IConfiguration                coreConfiguration,
        bool                          hotReload)
        => _shared = sharedSystem;

    public bool Init()
    {
        // client chat/console
        _shared.GetClientManager().InstallCommandCallback("hello", OnClientCommand);

        return true;
    }

    public void Shutdown()
    {
        // must release the command in Shutdown
        // otherwise you will get fucked after reloaded.
        _shared.GetClientManager().RemoveCommandCallback("hello", OnClientCommand);
    }

    // type 'ms_hello' in client console, or chat trigger with '.hello' or '/hello' or '!hello'
    private ECommandAction OnClientCommand(IGameClient client, StringCommand command)
    {
        if (_shared.GetEntityManager().FindPlayerControllerBySlot(client.Slot) is not { } controller
            || controller.GetPlayerPawn() is not { } pawn)
        {
            return ECommandAction.Stopped;
        }

        var sm = _shared.GetSoundManager();
        var ms = _shared.GetModSharp();

        var soundEvent = command.ArgCount > 0 ? command.GetArg(1) : "MySoundEventName";

        if (!sm.IsSoundEventValid(soundEvent))
        {
            return ECommandAction.Stopped;
        }

        var duration = sm.GetSoundDuration(soundEvent);

        controller.EmitSoundClient(soundEvent, 1.5f); // emit sound to specified client, with volume override
        pawn.EmitSoundClient(soundEvent, 1.5f); // emit sound to specified client, with volume override
        pawn.EmitSound(soundEvent); // emit sound to all clients from the pawn
        pawn.EmitSound(soundEvent, filter: new RecipientFilter(pawn.Team)); // emit sound to same team only from the pawn

        sm.StartSoundEvent(soundEvent, filter: new RecipientFilter(CStrikeTeam.CT)); // start a sound event to CT only

        var guid = sm.StartSoundEvent(soundEvent); // start a global sound event to all clients

        ms.PushTimer(() =>
                     {
                         // modify sound event parameter 'pitch' to 1.1
                         sm.SetSoundEventParam(guid, "pitch", 1.1f);
                     },
                     Math.Clamp(0.2, (duration / 2) - (duration / 4), duration));

        ms.PushTimer(() =>
                     {
                         // force stop sound event
                         sm.StopSoundEvent(guid);
                     },
                     duration / 2);

        return ECommandAction.Stopped;
    }

    public string DisplayName   => "Sound Emitter Example";
    public string DisplayAuthor => "ModSharp dev team";
}