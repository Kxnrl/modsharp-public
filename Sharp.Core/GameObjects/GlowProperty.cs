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

using Sharp.Core.Attributes;
using Sharp.Core.CStrike;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Core.GameObjects;

[EntityObjectInfo(0x18)] // m_pOuter 0x18
internal partial class GlowProperty : EntityObject, IGlowProperty
{
#region Schemas

    [NativeSchemaField("CGlowProperty", "m_iGlowType", typeof(int), IsStruct = true)]
    private partial SchemaField GetGlowTypeField();

    [NativeSchemaField("CGlowProperty", "m_iGlowTeam", typeof(int), IsStruct = true)]
    private partial SchemaField GetGlowTeamField();

    [NativeSchemaField("CGlowProperty", "m_flGlowTime", typeof(float), IsStruct = true)]
    private partial SchemaField GetGlowTimeField();

    [NativeSchemaField("CGlowProperty", "m_flGlowStartTime", typeof(float), IsStruct = true)]
    private partial SchemaField GetGlowStartTimeField();

    [NativeSchemaField("CGlowProperty", "m_bEligibleForScreenHighlight", typeof(bool), IsStruct = true)]
    private partial SchemaField GetEligibleForScreenHighlightField();

    [NativeSchemaField("CGlowProperty", "m_bGlowing", typeof(bool), IsStruct = true)]
    private partial SchemaField GetGlowingField();

    [NativeSchemaField("CGlowProperty", "m_bFlashing", typeof(bool), IsStruct = true)]
    private partial SchemaField GetFlashingField();

    [NativeSchemaField("CGlowProperty", "m_nGlowRangeMin", typeof(int), IsStruct = true)]
    private partial SchemaField GetGlowRangeMinField();

    [NativeSchemaField("CGlowProperty", "m_nGlowRange", typeof(int), IsStruct = true)]
    private partial SchemaField GetGlowRangeMaxField();

    [NativeSchemaField("CGlowProperty", "m_fGlowColor", typeof(Vector), IsStruct = true)]
    private partial SchemaField GetGlowColorField();

    [NativeSchemaField("CGlowProperty", "m_glowColorOverride", typeof(Color32), IsStruct = true)]
    private partial SchemaField GetGlowColorOverrideField();

#endregion

    public override string GetSchemaClassname()
        => "CGlowProperty";
}
