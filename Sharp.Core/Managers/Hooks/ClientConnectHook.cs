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

internal class ClientConnectHook : HookType<ClientConnectHookParams, bool, ClientConnectHook>
{
    public ClientConnectHook(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Client.OnClientConnect += CServerSideClient_CheckConnect;

    private HookReturnValue<bool> CServerSideClient_CheckConnect(SteamID steamId, string name, ref string rejectReason)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<bool>(EHookAction.Ignored, true);
        }

        var param  = new ClientConnectHookParams(false, steamId, name);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        rejectReason = param.Reason;

        return result;
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => false;

    protected override HookActionFlags SupportedActions => HookActionFlags.Ignored | HookActionFlags.SkipCallReturnOverride;
}

internal sealed class ClientConnectHookParams : FunctionParams, IClientConnectHookParams
{
    public ClientConnectHookParams(bool postHook, SteamID steamId, string name) : base(postHook)
    {
        SteamId = steamId;
        Name    = name;
    }

    public SteamID SteamId { get; }
    public string  Name    { get; }

    public string Reason => _reason ?? string.Empty;

    private string? _reason;

    public void SetBlockReason(string reason)
    {
        CheckDisposed();
        CheckEditable();

        _reason = reason;
    }
}
