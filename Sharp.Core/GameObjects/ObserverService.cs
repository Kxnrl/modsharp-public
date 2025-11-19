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

using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Core.GameObjects;

internal partial class ObserverService : PlayerPawnComponent, IObserverService
{
#region Schemas

    [NativeSchemaField("CPlayer_ObserverServices", "m_bForcedObserverMode", typeof(bool))]
    private partial SchemaField GetForcedObserverModeField();

    [NativeSchemaField("CPlayer_ObserverServices", "m_iObserverLastMode", typeof(ObserverMode))]
    private partial SchemaField GetObserverLastModeField();

    [NativeSchemaField("CPlayer_ObserverServices",
                       "m_hObserverTarget",
                       typeof(CEntityHandle<IBaseEntity>))]
    private partial SchemaField GetObserverTargetField();

    [NativeSchemaField("CPlayer_ObserverServices", "m_iObserverMode", typeof(ObserverMode))]
    private partial SchemaField GetObserverModeField();

#endregion

    public override string GetSchemaClassname()
        => "CCSObserver_ObserverServices";
}
