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

using Sharp.Core.Bridges.Natives;
using Sharp.Core.GameObjects;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Core.GameEntities;

internal partial class BaseModelEntity : BaseEntity, IBaseModelEntity
{
    public void SetBodyGroupByName(string name, int value)
        => Entity.SetBodyGroupByName(_this, name, value);

    public void SetMaterialGroupMask(ulong mask)
        => Entity.SetMaterialGroupMask(_this, mask);

    public int LookupAttachment(string attachment)
        => Entity.LookupAttachment(_this, attachment);

    public void GetAttachment(int attachment, out Vector origin, out Vector angles)
        => Entity.GetAttachment(_this, attachment, out origin, out angles);

    public int LookupBone(string bone)
        => Entity.LookupBone(_this, bone);

    public void GetBoneTransform(int bone, out Matrix3x4 matrix)
        => Entity.GetBoneTransform(_this, bone, out matrix);

    public void SetModelScale(float scale)
        => Entity.SetModelScale(_this, scale);

    public unsafe void SetCollisionBounds(Vector mins, Vector maxs)
        => Entity.SetCollisionBounds(_this, &mins, &maxs);

#region Schema

    [NativeSchemaField("CBaseModelEntity", "m_flDissolveStartTime", typeof(float))]
    private partial SchemaField GetDissolveStartTimeField();

    [NativeSchemaField("CBaseModelEntity", "m_nRenderMode", typeof(RenderMode))]
    private partial SchemaField GetRenderModeField();

    [NativeSchemaField("CBaseModelEntity", "m_nRenderFX", typeof(RenderFx))]
    private partial SchemaField GetRenderFxField();

    [NativeSchemaField("CBaseModelEntity", "m_clrRender", typeof(Color32))]
    private partial SchemaField GetRenderColorField();

    [NativeSchemaField("CBaseModelEntity", "m_bAllowFadeInView", typeof(bool))]
    private partial SchemaField GetAllowFadeInViewField();

    [NativeSchemaField("CBaseModelEntity", "m_fadeMinDist", typeof(float))]
    private partial SchemaField GetFadeMinDistanceField();

    [NativeSchemaField("CBaseModelEntity", "m_fadeMaxDist", typeof(float))]
    private partial SchemaField GetFadeMaxDistanceField();

    [NativeSchemaField("CBaseModelEntity", "m_flFadeScale", typeof(float))]
    private partial SchemaField GetFadeScaleField();

    [NativeSchemaField("CBaseModelEntity", "m_flShadowStrength", typeof(float))]
    private partial SchemaField GetShadowStrengthField();

    [NativeSchemaField("CBaseModelEntity", "m_Glow", typeof(GlowProperty), InlineObject = true)]
    private partial SchemaField GetGlowPropertyField();

    [NativeSchemaField("CBaseModelEntity", "m_vecViewOffset", typeof(Vector))]
    private partial SchemaField GetViewOffsetField();

#endregion
}
