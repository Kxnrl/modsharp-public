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
using Sharp.Shared.Attributes;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Shared.GameEntities;

[NetClass("CCSPlayerController")]
public interface IPlayerController : IBaseEntity
{
    /// <summary>
    ///     取得Controller对应的PlayerPawn
    /// </summary>
    /// <returns>
    ///     <see cref="IBasePlayerPawn" />
    /// </returns>
    IBasePlayerPawn? GetPawn();

    /// <summary>
    ///     取得Controller对应的PlayerPawn
    /// </summary>
    /// <returns>
    ///     <see cref="IPlayerPawn" />
    /// </returns>
    IPlayerPawn? GetPlayerPawn();

    /// <summary>
    ///     取得Controller对应的ObserverPawn
    /// </summary>
    /// <returns>
    ///     <see cref="IObserverPawn" />
    /// </returns>
    IObserverPawn? GetObserverPawn();

    /// <summary>
    ///     设置PlayerPawn <br />
    ///     <remarks>你调用之前最好知道你在做什么</remarks>
    /// </summary>
    void SetPlayerPawn(IPlayerPawn pawn);

    /// <summary>
    ///     获取<see cref="IGameClient" /> <br />
    ///     <remarks>如果玩家不在游戏内, 哪怕是实体存在, 也会返回<c>null</c></remarks>
    /// </summary>
    IGameClient? GetGameClient();

    /// <summary>
    ///     打印对应的消息 (HudMsg)
    /// </summary>
    void Print(HudPrintChannel channel,
        string                 message,
        string?                param1 = null,
        string?                param2 = null,
        string?                param3 = null,
        string?                param4 = null);

    /// <summary>
    ///     切换队伍 (存活状态)
    /// </summary>
    void SwitchTeam(CStrikeTeam team);

    /// <summary>
    ///     复活
    /// </summary>
    void Respawn();

    /// <summary>
    ///     设置组标
    /// </summary>
    void SetClanTag(string tag);

    /// <summary>
    ///     检查Pawn
    /// </summary>
    void CheckPawn();

    /// <summary>
    ///     设置PlayerPawn
    /// </summary>
    void SetPawn(IBasePlayerPawn pawn, bool unknown1, bool unknown2, bool unknown3, bool unknown4);

    /// <summary>
    ///     是否已在游戏
    /// </summary>
    bool IsConnected()
        => ConnectedState is PlayerConnectedState.PlayerConnected;

    /// <summary>
    ///     正在退游戏
    /// </summary>
    bool IsDisconnecting()
        => ConnectedState is PlayerConnectedState.PlayerDisconnecting;

    /// <summary>
    ///     SteamId 64位
    /// </summary>
    SteamID SteamId { get; }

    /// <summary>
    ///     PlayerSlot
    /// </summary>
    PlayerSlot PlayerSlot { get; }

    /// <summary>
    ///     组标
    /// </summary>
    string ClanTag { get; }

    /// <summary>
    ///     名字 (Set不提供StateChange)
    /// </summary>
    string PlayerName { get; set; }

    /// <summary>
    ///     Pawn存活状态 <br />
    ///     <remarks>该功能是给计分板用的</remarks>
    /// </summary>
    [Obsolete("Use GetPawn().IsAlive, will be removed in 2.2")]
    bool IsPawnAlive { get; }

    /// <summary>
    ///     Pawn血量 <br />
    ///     <remarks>该功能是给计分板用的</remarks>
    /// </summary>
    [Obsolete("Use GetPawn().Health, will be removed in 2.2")]
    uint PawnHealth { get; }

    /// <summary>
    ///     是否为Hltv
    /// </summary>
    bool IsHltv { get; }

    /// <summary>
    ///     是否是机器人
    /// </summary>
    bool IsFakeClient => Flags.HasFlag(EntityFlags.FakeClient);

    /// <summary>
    ///     计分板Score
    /// </summary>
    int Score { get; set; }

    /// <summary>
    ///     计分板MVP
    /// </summary>
    int MvpCount { get; set; }

    /// <summary>
    ///     更新计数器
    /// </summary>
    int UpdaterCount { get; set; }

    /// <summary>
    ///     对应Source1里的 m_flLaggedMovementValue
    /// </summary>
    float LaggedMovement { get; set; }

    /// <summary>
    ///     m_iCompetitiveRanking
    /// </summary>
    int CompetitiveRanking { get; set; }

    /// <summary>
    ///     m_iCompetitiveRankType
    /// </summary>
    CompetitiveRankType CompetitiveRankType { get; set; }

    /// <summary>
    ///     m_iCompetitiveWins
    /// </summary>
    int CompetitiveWins { get; set; }

    /// <summary>
    ///     m_iDesiredFOV
    /// </summary>
    uint DesiredFOV { get; set; }

    /// <summary>
    ///     连线状态
    /// </summary>
    PlayerConnectedState ConnectedState { get; }

    /// <summary>
    ///     是否在下一回合切换队伍
    /// </summary>
    bool SwitchTeamsOnNextRoundReset { get; set; }

    /// <summary>
    ///     m_bRemoveAllItemsOnNextRoundReset
    /// </summary>
    bool RemoveAllItemsOnNextRoundReset { get; set; }

    /// <summary>
    ///     期望的OB模式
    /// </summary>
    int DesiredObserverMode { get; set; }

    /// <summary>
    ///     m_bHasCommunicationAbuseMute
    /// </summary>
    bool HasCommunicationAbuseMute { get; set; }

    /// <summary>
    ///     m_iPendingTeamNum
    /// </summary>
    CStrikeTeam PendingTeamNum { get; set; }

    /// <summary>
    ///     m_bControllingBot
    /// </summary>
    bool ControllingBot { get; set; }

    /// <summary>
    ///     m_iRoundsWon
    /// </summary>
    int RoundsWon { get; set; }

    /// <summary>
    ///     DamageService实例
    /// </summary>
    IDamageService? GetDamageService();

    /// <summary>
    ///     MoneyService实例
    /// </summary>
    IInGameMoneyService? GetInGameMoneyService();

    /// <summary>
    ///     InventoryService实例
    /// </summary>
    IInventoryService? GetInventoryService();

    /// <summary>
    ///     ActionTrackingService实例
    /// </summary>
    IControllerActionTrackingService? GetActionTrackingService();

    /// <summary>
    ///     只给当前玩家播放本地音频
    /// </summary>
    SoundOpEventGuid EmitSoundClient(string sound, float? volume = null);

    /// <summary>
    ///     获取装备的库存物品
    /// </summary>
    IEconItemView? GetItemInLoadoutFromInventory(CStrikeTeam team, int slot);
}
