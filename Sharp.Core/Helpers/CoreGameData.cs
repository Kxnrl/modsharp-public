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

using Sharp.Core.Bridges.Interfaces;

namespace Sharp.Core.Helpers;

internal sealed class GameClientGameData
{
    public required int SignonState        { get; init; }
    public required int FakeClient         { get; init; }
    public required int UserId             { get; init; }
    public required int SteamId            { get; init; }
    public required int Slot               { get; init; }
    public required int GetName            { get; init; }
    public required int PerfectWorld       { get; init; }
    public required int FullyAuthenticated { get; init; }
    public required int DeltaTick          { get; init; }
    public required int IsHltv             { get; init; }
    public required int ConVars            { get; init; }
}

internal sealed class CNetworkGameServerGameData
{
    public required int VecClients { get; init; }
}

internal sealed class CBaseWeaponGameData
{
    public required int VData { get; init; }
}

internal static class CoreGameData
{
    private static GameClientGameData? _gameClientGameData;

    public static GameClientGameData GameClient
        => _gameClientGameData ??= new GameClientGameData
        {
            SignonState        = Core.GetOffset("CServerSideClient::m_SignonState"),
            FakeClient         = Core.GetOffset("CServerSideClient::m_FakeClient"),
            UserId             = Core.GetOffset("CServerSideClient::m_UserId"),
            SteamId            = Core.GetOffset("CServerSideClient::m_SteamId"),
            Slot               = Core.GetOffset("CServerSideClient::m_Slot"),
            GetName            = Core.GetOffset("CServerSideClient::m_Name"),
            PerfectWorld       = Core.GetOffset("CServerSideClient::m_PerfectWorld"),
            FullyAuthenticated = Core.GetOffset("CServerSideClient::m_FullyAuthenticated"),
            DeltaTick          = Core.GetOffset("CServerSideClient::m_nDeltaTick"),
            IsHltv             = Core.GetOffset("CServerSideClient::m_IsHLTV"),
            ConVars            = Core.GetOffset("CServerSideClient::m_ConVars"),
        };

    private static CNetworkGameServerGameData? _cNetworkGameServerGameData;

    public static CNetworkGameServerGameData CNetworkGameServer
        => _cNetworkGameServerGameData ??= new CNetworkGameServerGameData
        {
            VecClients = Core.GetOffset("CNetworkGameServer::m_vecClients"),
        };

    private static CBaseWeaponGameData? _cBaseWeapon;

    public static CBaseWeaponGameData CBaseWeapon
        => _cBaseWeapon ??= new CBaseWeaponGameData { VData = Core.GetOffset("CBaseWeapon::m_VData") };

    private static GameData? _core;

    public static GameData Core => _core ??= GameData.Create(Bridges.Natives.Core.GetGameData())!;
}
