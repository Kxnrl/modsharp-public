/*
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Core.Managers.Hooks;

internal class EmitSoundHook : HookType<EmitSoundHookParams, SoundOpEventGuid, EmitSoundHook>
{
    public EmitSoundHook(ILoggerFactory factory) : base(factory)
    {
        Bridges.Forwards.Game.OnEmitSound     += CSoundEmitterSystem_EmitSound;
        Bridges.Forwards.Game.OnEmitSoundPost += CSoundEmitterSystem_EmitSoundPost;
    }

    private EHookAction CSoundEmitterSystem_EmitSound(int entity,
        ref string                                        sound,
        ref float                                         volume,
        ref ulong                                         receivers,
        ref bool                                          changeReceiver,
        ref bool                                          overrideSound)
    {
        if (!IsHookInvokeRequired(false))
        {
            return EHookAction.Ignored;
        }

        var param  = new EmitSoundHookParams(false, entity, sound, volume, receivers, changeReceiver);
        var result = InvokeHookPre(param);

        sound          = param.SoundName;
        volume         = param.Volume;
        changeReceiver = param.Changed;
        receivers      = param.Receivers.AsPrimitive();
        overrideSound  = param.OverrideSound;

        return result.Action;
    }

    private void CSoundEmitterSystem_EmitSoundPost(int entity,
        string                                         sound,
        float                                          volume,
        ulong                                          receivers,
        bool                                           changeReceiver,
        EHookAction                                    action,
        SoundOpEventGuid                               guid)
    {
        if (!IsHookInvokeRequired(true))
        {
            return;
        }

        var param = new EmitSoundHookParams(true, entity, sound, volume, receivers, changeReceiver);

        InvokeHookPost(param, new HookReturnValue<SoundOpEventGuid>(action, guid));

        param.MarkAsDisposed();
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => true;
}

internal sealed class EmitSoundHookParams : FunctionParams, IEmitSoundHookParams
{
    public EmitSoundHookParams(bool postHook,
        EntityIndex                 entityIndex,
        string                      soundName,
        float                       volume,
        ulong                       receivers,
        bool                        changed) : base(postHook)
    {
        EntityIndex = entityIndex;
        SoundName   = soundName;
        Volume      = volume;
        Changed     = changed;
        Receivers   = receivers;
    }

    public EntityIndex     EntityIndex   { get; }
    public string          SoundName     { get; private set; }
    public float           Volume        { get; private set; }
    public NetworkReceiver Receivers     { get; private set; }
    public bool            Changed       { get; private set; }
    public bool            OverrideSound { get; private set; }

    public void SetSoundName(string soundName)
    {
        CheckDisposed();
        CheckEditable();
        SoundName     = soundName;
        OverrideSound = true;
    }

    public void SetVolume(float volume)
    {
        CheckDisposed();
        CheckEditable();
        Volume = volume;
    }

    public int GetReceiverCount()
    {
        CheckDisposed();

        return Receivers.Count();
    }

    public bool HasReceiver(PlayerSlot slot)
    {
        CheckDisposed();

        return Receivers.HasClient(slot);
    }

    public void AddReceiver(PlayerSlot slot)
    {
        CheckDisposed();
        CheckEditable();

        Receivers = Receivers.Append(slot);
        Changed   = true;
    }

    public void RemoveReceiver(PlayerSlot slot)
    {
        CheckDisposed();
        CheckEditable();

        Receivers = Receivers.Remove(slot);
        Changed   = true;
    }

    public void UpdateReceiver(NetworkReceiver newReceivers)
    {
        CheckDisposed();
        CheckEditable();

        Receivers = newReceivers;
        Changed   = true;
    }
}
