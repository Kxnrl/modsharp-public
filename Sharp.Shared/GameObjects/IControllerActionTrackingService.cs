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

using Sharp.Shared.Attributes;
using Sharp.Shared.CStrike;

namespace Sharp.Shared.GameObjects;

[NetClass("CSPerRoundStats_t")]
public interface IPerRoundStats : ISchemaObject
{
    int Kills { get; set; }

    int Deaths { get; set; }

    int Assists { get; set; }

    int Damage { get; set; }

    int EquipmentValue { get; set; }

    int MoneySaved { get; set; }

    int KillReward { get; set; }

    int LiveTime { get; set; }

    int HeadShotKills { get; set; }

    int Objective { get; set; }

    int CashEarned { get; set; }

    int UtilityDamage { get; set; }

    int EnemiesFlashed { get; set; }
}

[NetClass("CSMatchStats_t")]
public interface IMatchStats : IPerRoundStats
{
    int Enemy5Ks { get; set; }

    int Enemy4Ks { get; set; }

    int Enemy3Ks { get; set; }

    int Enemy2Ks { get; set; }

    int EnemyKnifeKills { get; set; }

    int EnemyTaserKills { get; set; }

    int UtilityCount { get; set; }

    int UtilitySuccesses { get; set; }

    int UtilityEnemies { get; set; }

    int FlashCount { get; set; }

    int FlashSuccesses { get; set; }

    float HealthPointsRemovedTotal { get; set; }

    float HealthPointsDealtTotal { get; set; }

    int ShotsFiredTotal { get; set; }

    int ShotsOnTargetTotal { get; set; }

    int R1V1Count { get; set; }

    int R1V1Wins { get; set; }

    int R1V2Count { get; set; }

    int R1V2Wins { get; set; }

    int EntryCount { get; set; }

    int EntryWins  { get; set; }
}

[NetClass("CSAdditionalPerRoundStats_t")]
public interface IAdditionalPerRoundStats : ISchemaObject
{
    int NumChickensKilled { get; set; }

    int KillsWhileBlind { get; set; }

    int BombCarrierkills { get; set; }

    float BurnDamageInflicted { get; set; }

    float BlastDamageInflicted { get; set; }

    int Dinks { get; set; }

    bool FreshStartThisRound { get; set; }

    bool BombPlantedAndAlive { get; set; }

    int DefuseStarts { get; set; }

    int HostagePickUps { get; set; }

    int NumTeammatesFlashed { get; set; }
}

[NetClass("CSAdditionalMatchStats_t")]
public interface IAdditionalMatchStats : IAdditionalPerRoundStats
{
    int NumRoundsSurvivedStreak { get; set; }

    int MaxNumRoundsSurvivedStreak { get; set; }

    int NumRoundsSurvivedTotal { get; set; }

    int RoundsWonWithoutPurchase { get; set; }

    int RoundsWonWithoutPurchaseTotal { get; set; }

    int NumFirstKills { get; set; }

    int NumClutchKills { get; set; }

    int NumPistolKills { get; set; }

    int NumSniperKills { get; set; }

    int NumSuicides { get; set; }

    int NumTeamKills { get; set; }

    float TeamDamage { get; set; }
}

[NetClass("CCSPlayerController_ActionTrackingServices")]
public interface IControllerActionTrackingService : IPlayerControllerComponent
{
    /// <summary>
    ///     Number of kills in current round
    /// </summary>
    int NumRoundKills { get; set; }

    /// <summary>
    ///     Number of headshot kills in current round
    /// </summary>
    int NumRoundKillsHeadshots { get; set; }

    /// <summary>
    ///     Total damage dealt in current round
    /// </summary>
    float TotalRoundDamageDealt { get; set; }

    /// <summary>
    ///     Gets the MatchStats instance
    /// </summary>
    IMatchStats GetMatchStats();

    /// <summary>
    ///     Get additional match stats
    /// </summary>
    IAdditionalMatchStats GetAdditionalMatchStats();
}
