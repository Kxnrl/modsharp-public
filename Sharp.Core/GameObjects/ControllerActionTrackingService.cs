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
using Sharp.Core.CStrike;
using Sharp.Core.Helpers;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.GameObjects;

namespace Sharp.Core.GameObjects;

internal partial class PerRoundStats : SchemaObject, IPerRoundStats
{
#region Schemas

    [NativeSchemaField("CSPerRoundStats_t", "m_iKills", typeof(int), IsStruct = true)]
    private partial SchemaField GetKillsField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iDeaths", typeof(int), IsStruct = true)]
    private partial SchemaField GetDeathsField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iAssists", typeof(int), IsStruct = true)]
    private partial SchemaField GetAssistsField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iDamage", typeof(int), IsStruct = true)]
    private partial SchemaField GetDamageField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iEquipmentValue", typeof(int), IsStruct = true)]
    private partial SchemaField GetEquipmentValueField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iMoneySaved", typeof(int), IsStruct = true)]
    private partial SchemaField GetMoneySavedField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iKillReward", typeof(int), IsStruct = true)]
    private partial SchemaField GetKillRewardField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iLiveTime", typeof(int), IsStruct = true)]
    private partial SchemaField GetLiveTimeField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iHeadShotKills", typeof(int), IsStruct = true)]
    private partial SchemaField GetHeadShotKillsField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iObjective", typeof(int), IsStruct = true)]
    private partial SchemaField GetObjectiveField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iCashEarned", typeof(int), IsStruct = true)]
    private partial SchemaField GetCashEarnedField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iUtilityDamage", typeof(int), IsStruct = true)]
    private partial SchemaField GetUtilityDamageField();

    [NativeSchemaField("CSPerRoundStats_t", "m_iEnemiesFlashed", typeof(int), IsStruct = true)]
    private partial SchemaField GetEnemiesFlashedField();

#endregion

    public override string GetSchemaClassname()
        => "CSPerRoundStats_t";
}

internal partial class MatchStats : PerRoundStats, IMatchStats
{
#region Schemas

    [NativeSchemaField("CSMatchStats_t", "m_iEnemy5Ks", typeof(int), IsStruct = true)]
    private partial SchemaField GetEnemy5KsField();

    [NativeSchemaField("CSMatchStats_t", "m_iEnemy4Ks", typeof(int), IsStruct = true)]
    private partial SchemaField GetEnemy4KsField();

    [NativeSchemaField("CSMatchStats_t", "m_iEnemy3Ks", typeof(int), IsStruct = true)]
    private partial SchemaField GetEnemy3KsField();

    [NativeSchemaField("CSMatchStats_t", "m_iEnemy2Ks", typeof(int), IsStruct = true)]
    private partial SchemaField GetEnemy2KsField();

    [NativeSchemaField("CSMatchStats_t", "m_iEnemyKnifeKills", typeof(int), IsStruct = true)]
    private partial SchemaField GetEnemyKnifeKillsField();

    [NativeSchemaField("CSMatchStats_t", "m_iEnemyTaserKills", typeof(int), IsStruct = true)]
    private partial SchemaField GetEnemyTaserKillsField();

    [NativeSchemaField("CSMatchStats_t", "m_iUtility_Count", typeof(int), IsStruct = true)]
    private partial SchemaField GetUtilityCountField();

    [NativeSchemaField("CSMatchStats_t", "m_iUtility_Successes", typeof(int), IsStruct = true)]
    private partial SchemaField GetUtilitySuccessesField();

    [NativeSchemaField("CSMatchStats_t", "m_iUtility_Enemies", typeof(int), IsStruct = true)]
    private partial SchemaField GetUtilityEnemiesField();

    [NativeSchemaField("CSMatchStats_t", "m_iFlash_Count", typeof(int), IsStruct = true)]
    private partial SchemaField GetFlashCountField();

    [NativeSchemaField("CSMatchStats_t", "m_iFlash_Successes", typeof(int), IsStruct = true)]
    private partial SchemaField GetFlashSuccessesField();

    [NativeSchemaField("CSMatchStats_t", "m_flHealthPointsRemovedTotal", typeof(float), IsStruct = true)]
    private partial SchemaField GetHealthPointsRemovedTotalField();

    [NativeSchemaField("CSMatchStats_t", "m_flHealthPointsDealtTotal", typeof(float), IsStruct = true)]
    private partial SchemaField GetHealthPointsDealtTotalField();

    [NativeSchemaField("CSMatchStats_t", "m_nShotsFiredTotal", typeof(int), IsStruct = true)]
    private partial SchemaField GetShotsFiredTotalField();

    [NativeSchemaField("CSMatchStats_t", "m_nShotsOnTargetTotal", typeof(int), IsStruct = true)]
    private partial SchemaField GetShotsOnTargetTotalField();

    [NativeSchemaField("CSMatchStats_t", "m_i1v1Count", typeof(int), IsStruct = true)]
    private partial SchemaField GetR1V1CountField();

    [NativeSchemaField("CSMatchStats_t", "m_i1v1Wins", typeof(int), IsStruct = true)]
    private partial SchemaField GetR1V1WinsField();

    [NativeSchemaField("CSMatchStats_t", "m_i1v2Count", typeof(int), IsStruct = true)]
    private partial SchemaField GetR1V2CountField();

    [NativeSchemaField("CSMatchStats_t", "m_i1v2Wins", typeof(int), IsStruct = true)]
    private partial SchemaField GetR1V2WinsField();

    [NativeSchemaField("CSMatchStats_t", "m_iEntryCount", typeof(int), IsStruct = true)]
    private partial SchemaField GetEntryCountField();

    [NativeSchemaField("CSMatchStats_t", "m_iEntryWins", typeof(int), IsStruct = true)]
    private partial SchemaField GetEntryWinsField();

#endregion

    public override string GetSchemaClassname()
        => "CSMatchStats_t";
}

internal partial class AdditionalPerRoundStats : SchemaObject, IAdditionalPerRoundStats
{
#region Schema

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_numChickensKilled", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumChickensKilledField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_killsWhileBlind", typeof(int), IsStruct = true)]
    private partial SchemaField GetKillsWhileBlindField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_bombCarrierkills", typeof(int), IsStruct = true)]
    private partial SchemaField GetBombCarrierkillsField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_flBurnDamageInflicted", typeof(float), IsStruct = true)]
    private partial SchemaField GetBurnDamageInflictedField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_flBlastDamageInflicted", typeof(float), IsStruct = true)]
    private partial SchemaField GetBlastDamageInflictedField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_iDinks", typeof(int), IsStruct = true)]
    private partial SchemaField GetDinksField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_bFreshStartThisRound", typeof(bool), IsStruct = true)]
    private partial SchemaField GetFreshStartThisRoundField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_bBombPlantedAndAlive", typeof(bool), IsStruct = true)]
    private partial SchemaField GetBombPlantedAndAliveField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_nDefuseStarts", typeof(int), IsStruct = true)]
    private partial SchemaField GetDefuseStartsField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_nHostagePickUps", typeof(int), IsStruct = true)]
    private partial SchemaField GetHostagePickUpsField();

    [NativeSchemaField("CSAdditionalPerRoundStats_t", "m_numTeammatesFlashed", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumTeammatesFlashedField();

#endregion

    public override string GetSchemaClassname()
        => "CSAdditionalPerRoundStats_t";
}

internal partial class AdditionalMatchStats : AdditionalPerRoundStats, IAdditionalMatchStats
{
#region Schemas

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_numRoundsSurvivedStreak", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumRoundsSurvivedStreakField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_maxNumRoundsSurvivedStreak", typeof(int), IsStruct = true)]
    private partial SchemaField GetMaxNumRoundsSurvivedStreakField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_numRoundsSurvivedTotal", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumRoundsSurvivedTotalField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_iRoundsWonWithoutPurchase", typeof(int), IsStruct = true)]
    private partial SchemaField GetRoundsWonWithoutPurchaseField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_iRoundsWonWithoutPurchaseTotal", typeof(int), IsStruct = true)]
    private partial SchemaField GetRoundsWonWithoutPurchaseTotalField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_numFirstKills", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumFirstKillsField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_numClutchKills", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumClutchKillsField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_numPistolKills", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumPistolKillsField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_numSniperKills", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumSniperKillsField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_iNumSuicides", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumSuicidesField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_iNumTeamKills", typeof(int), IsStruct = true)]
    private partial SchemaField GetNumTeamKillsField();

    [NativeSchemaField("CSAdditionalMatchStats_t", "m_flTeamDamage", typeof(float), IsStruct = true)]
    private partial SchemaField GetTeamDamageField();

#endregion

    public override string GetSchemaClassname()
        => "CSAdditionalMatchStats_t";
}

internal partial class ControllerActionTrackingService : PlayerControllerComponent, IControllerActionTrackingService
{
    private static readonly Lazy<int> AdditionalMatchStatsOffset = new (() =>
    {
        var baseOffset
            = SchemaSystem.GetNetVarOffset("CCSPlayerController_ActionTrackingServices", "m_flTotalRoundDamageDealt");

        const int alignment = 8;
        var       aligned   = ((baseOffset + sizeof(float) + alignment) - 1) & ~(alignment - 1);

        var finalOffset = aligned + SchemaSystem.GetSchemaClassSize("CSAdditionalPerRoundStats_t");

        return finalOffset;
    });

    public IAdditionalMatchStats GetAdditionalMatchStats()
        => AdditionalMatchStats.Create(nint.Add(_this, AdditionalMatchStatsOffset.Value))!;

#region Schemas

    [NativeSchemaField("CCSPlayerController_ActionTrackingServices", "m_iNumRoundKills", typeof(int))]
    private partial SchemaField GetNumRoundKillsField();

    [NativeSchemaField("CCSPlayerController_ActionTrackingServices", "m_iNumRoundKillsHeadshots", typeof(int))]
    private partial SchemaField GetNumRoundKillsHeadshotsField();

    [NativeSchemaField("CCSPlayerController_ActionTrackingServices", "m_flTotalRoundDamageDealt", typeof(float))]
    private partial SchemaField GetTotalRoundDamageDealtField();

    [NativeSchemaField("CCSPlayerController_ActionTrackingServices",
                       "m_matchStats",
                       typeof(MatchStats),
                       InlineObject = true)]
    private partial SchemaField GetMatchStatsField();

#endregion

    public override string GetSchemaClassname()
        => "CCSPlayerController_ActionTrackingServices";
}
