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

using Sharp.Shared.Attributes;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameEntities;

[NetClass("CCSPlayerPawnBase")]
public interface IBasePlayerPawn : IBaseCombatCharacter
{
    /// <summary>
    ///     Print message to this player
    /// </summary>
    void Print(HudPrintChannel channel,
        string                 message,
        string?                param1 = null,
        string?                param2 = null,
        string?                param3 = null,
        string?                param4 = null);

    /// <summary>
    ///     Check if entity is CCSPlayerPawn or CCSObserverPawn
    /// </summary>
    bool IsPlayer(bool nativeCall = false);

    /// <summary>
    ///     Cast to CCSPlayerPawn
    /// </summary>
    /// <returns></returns>
    IPlayerPawn? AsPlayer();

    /// <summary>
    ///     Cast to CCSObserverPawn
    /// </summary>
    IObserverPawn? AsObserver();

    /// <summary>
    ///     Gets the Controller corresponding to the current PlayerPawn
    /// </summary>
    IPlayerController? GetController();

    /// <summary>
    ///     m_hOriginalController
    /// </summary>
    IPlayerController? GetOriginalController();

    /// <summary>
    ///     Eye angles
    /// </summary>
    Vector GetEyeAngles();

    /// <summary>
    ///     Eye position
    /// </summary>
    Vector GetEyePosition();

    /// <summary>
    ///     Play a soundevent to this player, other player won't hear it
    /// </summary>
    /// <param name="sound">The sound event name to play (e.g., "Player.DamageKevlar")</param>
    /// <param name="volume">Volume. If null, uses default volume</param>
    SoundOpEventGuid EmitSoundClient(string sound, float? volume = null);

    /// <summary>
    ///     Change m_iTeamNum without sending update state to the client <br />
    /// </summary>
    void TransientChangeTeam(CStrikeTeam team);

    /// <summary>
    ///     CameraService
    /// </summary>
    ICameraService? GetCameraService();

    /// <summary>
    ///     MoveService
    /// </summary>
    IMovementService? GetMovementService();

    /// <summary>
    ///     UseService
    /// </summary>
    IUseService? GetUseService();

    /// <summary>
    ///     m_iHideHUD
    /// </summary>
    uint HideHud { get; set; }

    /// <summary>
    ///     m_fTimeLastHurt
    /// </summary>
    float TimeLastHurt { get; set; }

    /// <summary>
    ///     m_flDeathTime
    /// </summary>
    float DeathTime { get; set; }

    /// <summary>
    ///     m_fNextSuicideTime
    /// </summary>
    float NextSuicideTime { get; set; }

    /// <summary>
    ///     PlayerState
    /// </summary>
    PlayerState State { get; set; }

    /// <summary>
    ///     m_bRespawning
    /// </summary>
    bool Respawning { get; }

    /// <summary>
    ///     m_iNumSpawns
    /// </summary>
    int NumSpawns { get; }

    /// <summary>
    ///     Alpha for flashbang effect
    /// </summary>
    float FlashMaxAlpha { get; set; }

    /// <summary>
    ///     How long does the flashbang effect last
    /// </summary>
    float FlashDuration { get; set; }

    /// <summary>
    ///     m_fNextRadarUpdateTime
    /// </summary>
    float NextRadarUpdateTime { get; set; }

    /// <summary>
    ///     m_flProgressBarStartTime
    /// </summary>
    float ProgressBarStartTime { get; set; }

    /// <summary>
    ///     m_iProgressBarDuration
    /// </summary>
    int ProgressBarDuration { get; set; }
}
