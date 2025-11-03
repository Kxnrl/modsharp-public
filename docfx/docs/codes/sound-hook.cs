using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;

namespace SoundHookExample;

public sealed class SoundHook : IModSharpModule
{
    private readonly IHookManager  _hooks;
    private readonly ISoundManager _sounds;

    public SoundHook(ISharedSystem sharedSystem,
        string                     dllPath,
        string                     sharpPath,
        Version                    version,
        IConfiguration             coreConfiguration,
        bool                       hotReload)
    {
        _hooks  = sharedSystem.GetHookManager();
        _sounds = sharedSystem.GetSoundManager();
    }

    public bool Init()
    {
        _hooks.EmitSound.InstallHookPre(OnEmitSound);
        _hooks.SoundEvent.InstallHookPre(OnSoundEvent);

        _hooks.EmitSound.InstallHookPost(OnEmitSoundPost);
        _hooks.SoundEvent.InstallHookPost(OnSoundEventPost);

        return true;
    }

    public void Shutdown()
    {
        // must remove the hooks in Shutdown
        // otherwise you will get fucked after reloaded.
        _hooks.EmitSound.RemoveHookPre(OnEmitSound);
        _hooks.SoundEvent.RemoveHookPre(OnSoundEvent);

        _hooks.EmitSound.RemoveHookPost(OnEmitSoundPost);
        _hooks.SoundEvent.RemoveHookPost(OnSoundEventPost);
    }

    private HookReturnValue<SoundOpEventGuid> OnEmitSound(IEmitSoundHookParams param,
        HookReturnValue<SoundOpEventGuid>                                      previousResult)
    {
        if (previousResult.Action is EHookAction.SkipCallReturnOverride)
        {
            // the sound action was already blocked by other hook.
            return default;
        }

        // Block the headshot sound
        if (param.SoundName.Equals("Player.DeathHeadShot.Victim"))
        {
            return new HookReturnValue<SoundOpEventGuid>(EHookAction.SkipCallReturnOverride);
        }

        if (param.SoundName.Equals("Player.DeathHeadShot.Onlooker"))
        {
            // send to all players
            // ulong.MaxValue = 1<<0 + 1<<1 + 1<<2 + ... + 1<<63
            // you also can update the receiver by yourself.
            param.UpdateReceiver(ulong.MaxValue);

            // override the sound name
            param.SetSoundName("Player.DeathHeadShot");

            // change the param and return the game original call
            return new HookReturnValue<SoundOpEventGuid>(EHookAction.ChangeParamReturnDefault);
        }

        return new HookReturnValue<SoundOpEventGuid>();
    }

    private void OnEmitSoundPost(IEmitSoundHookParams param, HookReturnValue<SoundOpEventGuid> result)
    {
        // the sound action was blocked by other hook.
        if (result.Action is EHookAction.SkipCallReturnOverride)
        {
            return;
        }

        if (param.SoundName.Equals("Player.DeathHeadShot.Victim"))
        {
            // we can modify the sound parameters here.
            _sounds.SetSoundEventParam(result.ReturnValue, "volume", 100f, new RecipientFilter(param.Receivers));
        }
    }

    private HookReturnValue<SoundOpEventGuid> OnSoundEvent(ISoundEventHookParams param,
        HookReturnValue<SoundOpEventGuid>                                        previousResult)
    {
        // some sounds don't go through EmitSound, so we hook SoundEvent too.

        if (param.SoundName.Equals("why.some.*******s2.devs.copy.my.code.and.refactor.it.using.ai"))
        {
            // block sound
            return new HookReturnValue<SoundOpEventGuid>(EHookAction.SkipCallReturnOverride);
        }

        return new HookReturnValue<SoundOpEventGuid>();
    }

    private void OnSoundEventPost(ISoundEventHookParams param, HookReturnValue<SoundOpEventGuid> result)
    {
        // you can do something after sound event emitted.
        // like in the EmitSound post hook.
    }

    public string DisplayName   => "Sound Hook Example";
    public string DisplayAuthor => "ModSharp dev team";
}