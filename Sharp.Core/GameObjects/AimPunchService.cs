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

using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Core.GameObjects;

internal partial class AimPunchService : PlayerPawnComponent, IAimPunchService
{
#region Schemas

    [NativeSchemaField("CCSPlayer_AimPunchServices", "m_predictableBaseTick", typeof(int))]
    private partial SchemaField GetPredictableBaseTickField();

    [NativeSchemaField("CCSPlayer_AimPunchServices", "m_predictableBaseTickInterpAmount", typeof(float))]
    private partial SchemaField GetPredictableBaseTickInterpAmountField();

    [NativeSchemaField("CCSPlayer_AimPunchServices", "m_predictableBaseAngle", typeof(Vector))]
    private partial SchemaField GetPredictableBaseAngleField();

    [NativeSchemaField("CCSPlayer_AimPunchServices", "m_predictableBaseAngleVel", typeof(Vector))]
    private partial SchemaField GetPredictableBaseAngleVelField();

    [NativeSchemaField("CCSPlayer_AimPunchServices", "m_unpredictableBaseTick", typeof(int))]
    private partial SchemaField GetUnpredictableBaseTickField();

    [NativeSchemaField("CCSPlayer_AimPunchServices", "m_unpredictableBaseAngle", typeof(Vector))]
    private partial SchemaField GetUnpredictableBaseAngleField();

#endregion

    public override string GetSchemaClassname()
        => "CCSPlayer_AimPunchServices";
}
