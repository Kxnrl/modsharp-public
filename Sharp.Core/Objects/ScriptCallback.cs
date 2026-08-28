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

using System;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Core.GameEntities;
using Sharp.Core.Managers;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Core.Objects;

internal sealed class ScriptCallback : ContextObject, IScriptCallback
{
    private readonly ICoreScriptManager _manager;
    private readonly uint               _id;
    private readonly uint               _ownerHandle;

    public ScriptCallback(ICoreScriptManager manager, uint id, uint ownerHandle)
    {
        _manager     = manager;
        _id          = id;
        _ownerHandle = ownerHandle;
    }

    public IBaseEntity? Owner => BaseEntity.Create(Entity.FindByEHandle(_ownerHandle));

    public bool IsAvailable => !IsDisposed && _manager.IsCallbackAlive(_id);

    public ScriptReturnValue? Invoke(params ReadOnlySpan<ScriptArgument> args)
    {
        CheckDisposed();

        var invoked = _manager.InvokeCallback(_id, args, out var result);

        if (!invoked)
        {
            Dispose();

            throw new InvalidOperationException(
                "cs_script callback is no longer available (its owning point_script was destroyed); check IsAvailable before invoking.");
        }

        return result;
    }

    protected override void OnDisposed()
    {
        base.OnDisposed();

        Dispose();
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        _manager.ReleaseCallback(_id);
        MarkAsDisposed();
    }
}
