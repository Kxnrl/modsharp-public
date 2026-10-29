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
using Sharp.Core.Objects;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Objects;

namespace Sharp.Core.Managers.Hooks;

internal class PrintStatusHook : HookType<PrintStatusHookParams, EmptyHookReturn, PrintStatusHook>
{
    public PrintStatusHook(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Game.OnPrintStatus += CNetworkGameServer_PrintStatus;

    private EHookAction CNetworkGameServer_PrintStatus(nint pClient)
    {
        if (!IsHookInvokeRequired(false))
        {
            return EHookAction.Ignored;
        }

        var param  = new PrintStatusHookParams(false, pClient);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        return result.Action;
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => false;

    protected override HookActionFlags SupportedActions => HookActionFlags.Ignored | HookActionFlags.SkipCallReturnOverride;
}

internal sealed class PrintStatusHookParams : FunctionParams, IPrintStatusHookParams
{
    public IGameClient? Client { get; }

    public PrintStatusHookParams(bool postHook, nint client) : base(postHook)
        => Client = GameClient.Create(client);
}
