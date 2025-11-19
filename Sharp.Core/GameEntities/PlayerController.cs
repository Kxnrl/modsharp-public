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

using Sharp.Core.Bridges.Natives;
using Sharp.Core.GameObjects;
using Sharp.Core.Helpers;
using Sharp.Core.Objects;
using Sharp.Core.Utilities;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;

namespace Sharp.Core.GameEntities;

internal partial class PlayerController : BaseEntity, IPlayerController
{
    public void Print(HudPrintChannel channel,
        string                        message,
        string?                       param1 = null,
        string?                       param2 = null,
        string?                       param3 = null,
        string?                       param4 = null)
        => Player.ControllerPrint(_this, channel, message, param1, param2, param3, param4);

    public float LaggedMovement
    {
        get => Player.ControllerGetLaggedMovement(_this);
        set => Player.ControllerSetLaggedMovement(_this, value);
    }

    public void SwitchTeam(CStrikeTeam team)
        => Player.ControllerSwitchTeam(_this, team);

    public void Respawn()
        => Player.ControllerRoundRespawn(_this);

    public void SetClanTag(string tag)
        => Player.ControllerSetClanTag(_this, tag);

    public void CheckPawn()
        => Player.ControllerCheckPawn(_this);

    public void SetPawn(IBasePlayerPawn pawn, bool unknown1, bool unknown2, bool unknown3, bool unknown4)
        => Player.ControllerSetPawn(_this, pawn.GetAbsPtr(), unknown1, unknown2, unknown3, unknown4);

    public IBasePlayerPawn? GetPawn()
        => BasePlayerPawn.Create(Player.ControllerGetPawn(_this));

    public IPlayerPawn? GetPlayerPawn()
        => PlayerPawn.Create(PlayerPawnHandle.GetEntityPtr());

    public IObserverPawn? GetObserverPawn()
        => ObserverPawn.Create(ObserverPawnHandle.GetEntityPtr());

    public void SetPlayerPawn(IPlayerPawn pawn)
        => PlayerPawnHandle = pawn.Handle.As<IPlayerPawn>();

    public IGameClient? GetGameClient()
        => ConnectedState is not PlayerConnectedState.PlayerConnected
            ? null
            : GameClient.Create(Client.GetClientBySlot(PlayerSlot));

    public PlayerSlot PlayerSlot => (byte) (Index - 1);

    public unsafe SoundOpEventGuid EmitSoundClient(string sound, float? volume = null)
        => Player.ControllerEmitSoundClient(_this, sound, &volume);

    public IEconItemView? GetItemInLoadoutFromInventory(CStrikeTeam team, int slot)
        => EconItemView.Create(Player.ControllerGetItemInLoadoutFromInventory(_this, team, slot));

    public string PlayerName
    {
        get => SchemaSystem.GetNetVarString(_this, "CBasePlayerController", "m_iszPlayerName");
        set => SchemaSystem.SetNetVarString(_this, "CBasePlayerController", "m_iszPlayerName", value, 128);
    }

#region Schemas

    [NativeSchemaField("CCSPlayerController", "m_szClan", typeof(CUtlSymbolLarge))]
    public partial SchemaField GetClanTagField();

    [NativeSchemaField("CCSPlayerController", "m_hPlayerPawn", typeof(CEntityHandle<IPlayerPawn>))]
    public partial SchemaField GetPlayerPawnHandleField();

    [NativeSchemaField("CCSPlayerController", "m_hObserverPawn", typeof(CEntityHandle<IPlayerPawn>))]
    public partial SchemaField GetObserverPawnHandleField();

    [NativeSchemaField("CCSPlayerController", "m_iPawnHealth", typeof(uint))]
    public partial SchemaField GetPawnHealthField();

    [NativeSchemaField("CBasePlayerController", "m_bIsHLTV", typeof(bool))]
    public partial SchemaField GetIsHltvField();

    [NativeSchemaField("CCSPlayerController", "m_iPendingTeamNum", typeof(CStrikeTeam))]
    public partial SchemaField GetPendingTeamNumField();

    [NativeSchemaField("CCSPlayerController", "m_bHasCommunicationAbuseMute", typeof(bool))]
    public partial SchemaField GetHasCommunicationAbuseMuteField();

    [NativeSchemaField("CCSPlayerController", "m_bControllingBot", typeof(bool))]
    public partial SchemaField GetControllingBotField();

    [NativeSchemaField("CCSPlayerController", "m_bPawnIsAlive", typeof(bool))]
    public partial SchemaField GetIsPawnAliveField();

    [NativeSchemaField("CBasePlayerController", "m_steamID", typeof(SteamID))]
    public partial SchemaField GetSteamIdField();

    [NativeSchemaField("CCSPlayerController", "m_iScore", typeof(int))]
    public partial SchemaField GetScoreField();

    [NativeSchemaField("CCSPlayerController", "m_iRoundsWon", typeof(int))]
    public partial SchemaField GetRoundsWonField();

    [NativeSchemaField("CCSPlayerController", "m_iMVPs", typeof(int))]
    public partial SchemaField GetMvpCountField();

    [NativeSchemaField("CCSPlayerController", "m_nUpdateCounter", typeof(int))]
    public partial SchemaField GetUpdaterCountField();

    [NativeSchemaField("CCSPlayerController", "m_bSwitchTeamsOnNextRoundReset", typeof(bool))]
    private partial SchemaField GetSwitchTeamsOnNextRoundResetField();

    [NativeSchemaField("CCSPlayerController", "m_bRemoveAllItemsOnNextRoundReset", typeof(bool))]
    private partial SchemaField GetRemoveAllItemsOnNextRoundResetField();

    [NativeSchemaField("CCSPlayerController", "m_iCompetitiveRanking", typeof(int))]
    private partial SchemaField GetCompetitiveRankingField();

    [NativeSchemaField("CCSPlayerController", "m_iCompetitiveWins", typeof(int))]
    private partial SchemaField GetCompetitiveWinsField();

    [NativeSchemaField("CCSPlayerController", "m_iCompetitiveRankType", typeof(CompetitiveRankType))]
    private partial SchemaField GetCompetitiveRankTypeField();

    [NativeSchemaField("CCSPlayerController", "m_DesiredObserverMode", typeof(int))]
    private partial SchemaField GetDesiredObserverModeField();

    [NativeSchemaField("CBasePlayerController", "m_iConnected", typeof(PlayerConnectedState))]
    private partial SchemaField GetConnectedStateField();

    [NativeSchemaField("CCSPlayerController", "m_pDamageServices", typeof(DamageService))]
    private partial SchemaField GetDamageServiceField();

    [NativeSchemaField("CCSPlayerController", "m_pInGameMoneyServices", typeof(InGameMoneyService))]
    private partial SchemaField GetInGameMoneyServiceField();

    [NativeSchemaField("CCSPlayerController", "m_pInventoryServices", typeof(InventoryService))]
    private partial SchemaField GetInventoryServiceField();

    [NativeSchemaField("CCSPlayerController", "m_pActionTrackingServices", typeof(ControllerActionTrackingService))]
    private partial SchemaField GetActionTrackingServiceField();

    [NativeSchemaField("CBasePlayerController", "m_iDesiredFOV", typeof(uint))]
    private partial SchemaField GetDesiredFOVField();

#endregion
}
