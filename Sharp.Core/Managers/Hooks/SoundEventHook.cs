/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
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

internal class SoundEventHook : HookType<SoundEventHookParams, SoundOpEventGuid, SoundEventHook>
{
    public SoundEventHook(ILoggerFactory factory) : base(factory)
    {
        Bridges.Forwards.Game.OnSoundEvent     += SoundOpGameSystem_SoundEvent;
        Bridges.Forwards.Game.OnSoundEventPost += SoundOpGameSystem_SoundEventPost;
    }

    private EHookAction SoundOpGameSystem_SoundEvent(int entity,
        string                                           sound,
        float                                            duration,
        ref ulong                                        receivers,
        ref bool                                         changed)
    {
        if (!IsHookInvokeRequired(false))
        {
            return EHookAction.Ignored;
        }

        var param  = new SoundEventHookParams(false, entity, sound, duration, receivers, changed);
        var result = InvokeHookPre(param);

        changed   = param.Changed;
        receivers = param.Receivers.AsPrimitive();

        param.MarkAsDisposed();

        return result.Action;
    }

    private void SoundOpGameSystem_SoundEventPost(int entity,
        string                                        sound,
        float                                         duration,
        ulong                                         receivers,
        bool                                          changed,
        EHookAction                                   action,
        SoundOpEventGuid                              guid)
    {
        if (!IsHookInvokeRequired(true))
        {
            return;
        }

        var param = new SoundEventHookParams(true, entity, sound, duration, receivers, changed);

        InvokeHookPost(param, new HookReturnValue<SoundOpEventGuid>(action, guid));

        param.MarkAsDisposed();
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => true;
}

internal sealed class SoundEventHookParams : FunctionParams, ISoundEventHookParams
{
    public SoundEventHookParams(bool postHook,
        EntityIndex                  entityIndex,
        string                       soundName,
        float                        soundDuration,
        ulong                        receivers,
        bool                         changed) : base(postHook)
    {
        EntityIndex   = entityIndex;
        SoundName     = soundName;
        SoundDuration = soundDuration;
        Changed       = changed;
        Receivers     = receivers;
    }

    public EntityIndex     EntityIndex   { get; }
    public string          SoundName     { get; }
    public float           SoundDuration { get; }
    public NetworkReceiver Receivers     { get; private set; }
    public bool            Changed       { get; private set; }

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
