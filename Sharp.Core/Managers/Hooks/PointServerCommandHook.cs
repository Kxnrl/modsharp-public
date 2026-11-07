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

namespace Sharp.Core.Managers.Hooks;

internal class PointServerCommandHook : HookType<PointServerCommandHookParams, EmptyHookReturn, PointServerCommandHook>
{
    public PointServerCommandHook(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Extern.PointServerCommand.OnPointServerCommand += PointServerCommand_OnPointServerCommand;

    private EHookAction PointServerCommand_OnPointServerCommand(nint ptrEntity, string command)
    {
        if (!IsHookInvokeRequired(false))
        {
            return EHookAction.Ignored;
        }

        var param  = new PointServerCommandHookParams(false, ptrEntity, command);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        return result.Action;
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => false;

    protected override HookActionFlags SupportedActions => HookActionFlags.Ignored | HookActionFlags.SkipCallReturnOverride;
}

internal sealed class PointServerCommandHookParams : EntityFunctionParams, IPointServerCommandHookParams
{
    public PointServerCommandHookParams(bool postHook, nint entity, string command) : base(postHook, entity)
        => Command = command;

    public string Command { get; }
}
