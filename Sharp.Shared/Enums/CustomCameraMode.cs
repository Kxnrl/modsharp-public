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

namespace Sharp.Shared.Enums;

/// <summary>
///     CustomCameraMode_t
/// </summary>
public enum CustomCameraMode : byte
{
    /// <summary>
    ///     Position and angles come from the eye position and angles of the player.
    /// </summary>
    Disabled = 0,

    /// <summary>
    ///     Position and angles come from the origin and angles of the camera entity.
    /// </summary>
    Controlled = 1,

    /// <summary>
    ///     Position comes from the origin of camera entity. Angles are player controlled.
    /// </summary>
    ControlledPosition = 2,

    /// <summary>
    ///     Position comes from an offset around a followed position. Angles are player controlled.
    /// </summary>
    FollowPosition = 3,
}
