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
    ///     打印对应的消息 (HudMsg)
    /// </summary>
    void Print(HudPrintChannel channel,
        string                 message,
        string?                param1 = null,
        string?                param2 = null,
        string?                param3 = null,
        string?                param4 = null);

    /// <summary>
    ///     CCSPlayerPawn或者CCSObserverPawn
    /// </summary>
    bool IsPlayer(bool nativeCall = false);

    /// <summary>
    ///     转换为CCSPlayerPawn
    /// </summary>
    /// <returns></returns>
    IPlayerPawn? AsPlayer();

    /// <summary>
    ///     转换为CCSObserverPawn
    /// </summary>
    IObserverPawn? AsObserver();

    /// <summary>
    ///     取得当前PlayerPawn对应的Controller
    /// </summary>
    IPlayerController? GetController();

    /// <summary>
    ///     m_hOriginalController
    /// </summary>
    IPlayerController? GetOriginalController();

    /// <summary>
    ///     👀角度
    /// </summary>
    Vector GetEyeAngles();

    /// <summary>
    ///     👀位置
    /// </summary>
    Vector GetEyePosition();

    /// <summary>
    ///     只给当前玩家播放本地音频
    /// </summary>
    SoundOpEventGuid EmitSoundClient(string sound, float? volume = null);

    /// <summary>
    ///     瞬态更换队伍 <br />
    ///     <remarks>直接修改m_iTeamNum的值并且不发送网络消息</remarks>
    /// </summary>
    void TransientChangeTeam(CStrikeTeam team);

    /// <summary>
    ///     CameraService实例
    /// </summary>
    ICameraService? GetCameraService();

    /// <summary>
    ///     MoveService实例
    /// </summary>
    IMovementService? GetMovementService();

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
    ///     闪光Alpha
    /// </summary>
    float FlashMaxAlpha { get; set; }

    /// <summary>
    ///     闪光持续时间
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
