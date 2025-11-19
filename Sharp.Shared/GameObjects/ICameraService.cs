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
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameObjects;

[NetClass("CCSPlayerBase_CameraServices")]
public interface ICameraService : IPlayerPawnComponent
{
    /// <summary>
    ///     CPlayer_CameraServices::m_hViewEntity
    /// </summary>
    CEntityHandle<IBaseEntity> ViewEntityHandle { get; set; }

    /// <summary>
    ///     CPlayer_CameraServices::m_hViewEntity
    /// </summary>
    IBaseEntity? ViewEntity { get; set; }

    /// <summary>
    ///     CCSPlayerBase_CameraServices::m_iFOV
    /// </summary>
    uint FieldOfView { get; set; }

    /// <summary>
    ///     CCSPlayerBase_CameraServices::m_iFOVStart
    /// </summary>
    uint FieldOfViewStart { get; set; }

    /// <summary>
    ///     CCSPlayerBase_CameraServices::m_flFOVTime
    /// </summary>
    float FieldOfViewTime { get; set; }

    /// <summary>
    ///     CCSPlayerBase_CameraServices::m_flFOVRate
    /// </summary>
    float FieldOfViewRate { get; set; }

    /// <summary>
    ///     CCSPlayerBase_CameraServices::m_hZoomOwner
    /// </summary>
    CEntityHandle<IBaseEntity> ZoomOwnerHandle { get; set; }

    /// <summary>
    ///     CPlayer_CameraServices::m_PostProcessingVolumes
    /// </summary>
    ISchemaList<CEntityHandle<IBaseEntity>> GetPostProcessingVolumes();
}
