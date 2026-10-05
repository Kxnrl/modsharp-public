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

using Sharp.Core.Utilities;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Core.GameEntities;

internal partial class CustomPlayerCamera : BaseEntity, ICustomPlayerCamera
{
    public IBasePlayerPawn? Pawn => BasePlayerPawn.Create(PawnHandle.GetEntityPtr());

    public IBaseEntity? FollowEntity => BaseEntity.Create(FollowEntityHandle.GetEntityPtr());

#region Schemas

    [NativeSchemaField("CCSCustomPlayerCamera", "m_hPawn", typeof(CEntityHandle<IBasePlayerPawn>))]
    private partial SchemaField GetPawnHandleField();

    [NativeSchemaField("CCSCustomPlayerCamera", "m_nCameraMode", typeof(CustomCameraMode))]
    private partial SchemaField GetCameraModeField();

    [NativeSchemaField("CCSCustomPlayerCamera", "m_hFollowEntity", typeof(CEntityHandle<IBaseEntity>))]
    private partial SchemaField GetFollowEntityHandleField();

    [NativeSchemaField("CCSCustomPlayerCamera", "m_bFollowEyes", typeof(bool))]
    private partial SchemaField GetFollowEyesField();

    [NativeSchemaField("CCSCustomPlayerCamera", "m_vecFollowOffset", typeof(Vector))]
    private partial SchemaField GetFollowOffsetField();

    [NativeSchemaField("CCSCustomPlayerCamera", "m_vecCameraOffset", typeof(Vector))]
    private partial SchemaField GetCameraOffsetField();

    [NativeSchemaField("CCSCustomPlayerCamera", "m_bClipCameraOffset", typeof(bool))]
    private partial SchemaField GetClipCameraOffsetField();

    [NativeSchemaField("CCSCustomPlayerCamera", "m_flCameraOffsetReturnStrength", typeof(float))]
    private partial SchemaField GetCameraOffsetReturnStrengthField();

#endregion
}
