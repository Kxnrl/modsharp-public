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

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Core.Managers.Hooks;

internal class ConnectClientHook : HookType<ConnectClientHookParams, NetworkDisconnectionReason, ConnectClientHook>
{
    public ConnectClientHook(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Client.OnConnectClient += CNetworkGameServer_ConnectClient;

    private HookReturnValue<NetworkDisconnectionReason> CNetworkGameServer_ConnectClient(SteamID steamId, string name, uint ip)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<NetworkDisconnectionReason>(EHookAction.Ignored);
        }

        var param  = new ConnectClientHookParams(false, steamId, name, ip);
        var result = InvokeHookPre(param);

        param.MarkAsDisposed();

        return result;
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => false;
}

internal sealed class ConnectClientHookParams : FunctionParams, IConnectClientHookParams
{
    private          string? _ipAddressCache;
    private readonly uint    _rawIp;

    public ConnectClientHookParams(bool postHook, SteamID steamId, string name, uint rawIp) : base(postHook)
    {
        SteamId = steamId;
        Name    = name;
        _rawIp  = rawIp;
    }

    public SteamID SteamId { get; }
    public string  Name    { get; }
    public string  Ip      => _ipAddressCache ??= ParseIpAddress(_rawIp);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ParseIpAddress(uint rawIp)
        => $"{rawIp >> 24}.{(rawIp >> 16) & 0xFF}.{(rawIp >> 8) & 0xFF}.{rawIp & 0xFF}";
}
