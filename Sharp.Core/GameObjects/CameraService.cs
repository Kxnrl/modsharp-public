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

using Sharp.Core.CStrike;
using Sharp.Core.GameEntities;
using Sharp.Core.Utilities;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Core.GameObjects;

internal partial class CameraService : PlayerPawnComponent, ICameraService
{
    public IBaseEntity? ViewEntity
    {
        get => BaseEntity.Create(ViewEntityHandle.GetEntityPtr());
        set => ViewEntityHandle = value?.Handle ?? new CEntityHandle<IBaseEntity>();
    }

#region Schemas

    [NativeSchemaField("CPlayer_CameraServices", "m_hViewEntity", typeof(CEntityHandle<IBaseEntity>))]
    private partial SchemaField GetViewEntityHandleField();

    [NativeSchemaField("CCSPlayerBase_CameraServices", "m_iFOV", typeof(uint))]
    private partial SchemaField GetFieldOfViewField();

    [NativeSchemaField("CCSPlayerBase_CameraServices", "m_iFOVStart", typeof(uint))]
    private partial SchemaField GetFieldOfViewStartField();

    [NativeSchemaField("CCSPlayerBase_CameraServices", "m_flFOVTime", typeof(float))]
    private partial SchemaField GetFieldOfViewTimeField();

    [NativeSchemaField("CCSPlayerBase_CameraServices", "m_flFOVRate", typeof(float))]
    private partial SchemaField GetFieldOfViewRateField();

    [NativeSchemaField("CPlayer_CameraServices",
                       "m_PostProcessingVolumes",
                       typeof(SchemaUnmanagedVector<CEntityHandle<IBaseEntity>>),
                       InlineObject = true,
                       ReturnType = typeof(ISchemaList<CEntityHandle<IBaseEntity>>))]
    private partial SchemaField GetPostProcessingVolumesField();

    [NativeSchemaField("CCSPlayerBase_CameraServices", "m_hZoomOwner", typeof(CEntityHandle<IBaseEntity>))]
    private partial SchemaField GetZoomOwnerHandleField();

#endregion

    public override string GetSchemaClassname()
        => "CCSPlayerBase_CameraServices";
}
