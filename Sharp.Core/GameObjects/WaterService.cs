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
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Core.GameObjects;

internal partial class WaterService : PlayerPawnComponent, IWaterService
{
#region Schemas

    [NativeSchemaField("CCSPlayer_WaterServices", "m_NextDrownDamageTime", typeof(float))]
    private partial SchemaField GetNextDrownDamageTimeField();

    [NativeSchemaField("CCSPlayer_WaterServices", "m_nDrownDmgRate", typeof(int))]
    private partial SchemaField GetDrownDmgRateField();

    [NativeSchemaField("CCSPlayer_WaterServices", "m_AirFinishedTime", typeof(float))]
    private partial SchemaField GetAirFinishedTimeField();

    [NativeSchemaField("CCSPlayer_WaterServices", "m_flWaterJumpTime", typeof(float))]
    private partial SchemaField GetWaterJumpTimeField();

    [NativeSchemaField("CCSPlayer_WaterServices", "m_vecWaterJumpVel", typeof(Vector))]
    private partial SchemaField GetWaterJumpVelField();

    [NativeSchemaField("CCSPlayer_WaterServices", "m_flSwimSoundTime", typeof(float))]
    private partial SchemaField GetSwimSoundTimeField();

#endregion

    public override string GetSchemaClassname()
        => "CCSPlayer_WaterServices";
}
