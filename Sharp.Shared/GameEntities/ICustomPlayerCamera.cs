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
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameEntities;

/// <summary>
///     custom_player_camera <br />
///     Move this to control a player's view without moving their pawn.
/// </summary>
[NetClass("CCSCustomPlayerCamera")]
public interface ICustomPlayerCamera : IBaseEntity
{
    /// <summary>
    ///     m_hPawn
    /// </summary>
    CEntityHandle<IBasePlayerPawn> PawnHandle { get; set; }

    /// <summary>
    ///     m_hPawn
    /// </summary>
    IBasePlayerPawn? Pawn { get; }

    /// <summary>
    ///     m_nCameraMode
    /// </summary>
    CustomCameraMode CameraMode { get; set; }

    /// <summary>
    ///     m_hFollowEntity <br />
    ///     Entity to follow when <see cref="CameraMode" /> is <see cref="CustomCameraMode.FollowPosition" />.
    /// </summary>
    CEntityHandle<IBaseEntity> FollowEntityHandle { get; set; }

    /// <summary>
    ///     m_hFollowEntity
    /// </summary>
    IBaseEntity? FollowEntity { get; }

    /// <summary>
    ///     m_bFollowEyes <br />
    ///     Follow the eye position of the follow entity instead of its origin.
    /// </summary>
    bool FollowEyes { get; set; }

    /// <summary>
    ///     m_vecFollowOffset <br />
    ///     Offset from the followed position.
    /// </summary>
    Vector FollowOffset { get; set; }

    /// <summary>
    ///     m_vecCameraOffset <br />
    ///     Offset of the camera relative to the followed position, in view space (e.g. third-person shoulder).
    /// </summary>
    Vector CameraOffset { get; set; }

    /// <summary>
    ///     m_bClipCameraOffset <br />
    ///     Trace the camera offset against the world so the camera does not clip into geometry.
    /// </summary>
    bool ClipCameraOffset { get; set; }

    /// <summary>
    ///     m_flCameraOffsetReturnStrength
    /// </summary>
    float CameraOffsetReturnStrength { get; set; }
}
