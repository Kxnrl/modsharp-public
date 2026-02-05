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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sharp.Core.Bridges.Interfaces;
using Sharp.Core.Objects;
using Sharp.Core.Utilities;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;

namespace Sharp.Core.Managers;

internal interface ICoreParticleManager : IParticleManager;

internal class ParticleManager : ICoreParticleManager
{
    private readonly ILogger<ParticleManager>  _logger;
    private readonly Dictionary<string, ulong> _particleIndexCache;

    private const uint ResourceSeed = 0xEDABCDEF;

    private NetworkingStringTable _effectDispatchStringTable = null!;
    private int                   _particleEffectIndex;

    public ParticleManager(ILogger<ParticleManager> logger)
    {
        _logger             = logger;
        _particleIndexCache = new Dictionary<string, ulong>(StringComparer.Ordinal);

        Bridges.Forwards.Game.OnServerActivate += OnServerActivate;
        Bridges.Forwards.Game.OnGameShutdown   += OnGameShutdown;
    }

    private void OnGameShutdown()
    {
        _particleIndexCache.Clear();
    }

    private void OnServerActivate()
    {
        if (NetworkingStringTable.Create(Bridges.Natives.Core.FindStringTable("EffectDispatch")) is not { } stringTable)
        {
            throw new InvalidOperationException("Failed to find EffectDispatch string table");
        }

        _effectDispatchStringTable = stringTable;
        _particleEffectIndex       = _effectDispatchStringTable.FindStringIndex("ParticleEffect");
    }

    private static void DispatchParticleEffectRaw(RecipientFilter filter, CMsgEffectData effectData)
    {
        var msg = new CMsgTEEffectDispatch { Effectdata = effectData };
        NetMessageHelper.SendNetMessage(filter, msg);
    }

    public void DispatchParticleEffect(string particle, Vector origin, Vector angles, RecipientFilter filter = default)
    {
        var idx = GetEffectIndex(particle);

        var effectData = new CMsgEffectData
        {
            Origin      = origin.ToMsgVector(),
            Angles      = angles.ToMsgQAngle(),
            Effectindex = idx,
            Effectname  = (uint) _particleEffectIndex,
        };

        DispatchParticleEffectRaw(filter, effectData);
    }

    public void DispatchParticleEffect(string          particle,
                                       IBaseEntity     entity,
                                       Vector          origin,
                                       Vector          angles,
                                       bool            resetEntity = false,
                                       RecipientFilter filter      = default)
    {
        var idx = GetEffectIndex(particle);

        var handle = entity.RefHandle;
        var flags  = resetEntity ? EffectDataFlags.ResetParticlesOnEntity : EffectDataFlags.None;

        if (handle.IsValid())
        {
            flags |= EffectDataFlags.AttachToEntity;
        }

        var effectData = new CMsgEffectData
        {
            Origin      = origin.ToMsgVector(),
            Angles      = angles.ToMsgQAngle(),
            Effectindex = idx,
            Effectname  = (uint) _particleEffectIndex,
            Entity      = handle.GetPackedValue(),
            Flags       = (uint) flags,
        };

        DispatchParticleEffectRaw(filter, effectData);
    }

    public void DispatchParticleEffect(string                 particle,
                                       ParticleAttachmentType attachType,
                                       IBaseEntity            entity,
                                       byte                   attachmentIndex = 0,
                                       bool                   resetEntity     = false,
                                       RecipientFilter        filter          = default)
    {
        var idx = GetEffectIndex(particle);

        var flags = EffectDataFlags.AttachToEntity;

        if (resetEntity)
        {
            flags |= EffectDataFlags.ResetParticlesOnEntity;
        }

        var origin = entity.GetAbsOrigin();

        var effectData = new CMsgEffectData
        {
            Origin          = origin.ToMsgVector(),
            Effectindex     = idx,
            Effectname      = (uint) _particleEffectIndex,
            Entity          = entity.RefHandle.GetPackedValue(),
            Flags           = (uint) flags,
            Damagetype      = (uint) attachType,
            Attachmentindex = attachmentIndex,
        };

        DispatchParticleEffectRaw(filter, effectData);
    }

    public void DispatchEffect(string effectName, IBaseEntity entity, Vector origin, RecipientFilter filter)
    {
        var idx = _effectDispatchStringTable.FindStringIndex(effectName);

        if (idx == -1)
        {
            _logger.LogError("No effect index was found for '{name}'", effectName);

            return;
        }

        var effectData = new CMsgEffectData
        {
            Effectname = (uint) idx, Entity = entity.RefHandle.GetPackedValue(), Origin = origin.ToMsgVector(),
        };

        DispatchParticleEffectRaw(filter, effectData);
    }

    private ulong GetEffectIndex(string particle)
    {
        ref var indexRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_particleIndexCache, particle, out var exists);

        if (exists)
        {
            return indexRef;
        }

        var idx = MurmurHash64B.Compute(particle, ResourceSeed);
        indexRef = idx;

        return idx;
    }
}
