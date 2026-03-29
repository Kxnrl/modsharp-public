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
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameEntities;

[NetClass("CBaseModelEntity")]
public interface IBaseModelEntity : IBaseEntity
{
    /// <summary>
    ///     m_flDissolveStartTime
    /// </summary>
    float DissolveStartTime { get; set; }

    /// <summary>
    ///     m_nRenderMode
    /// </summary>
    RenderMode RenderMode { get; set; }

    /// <summary>
    ///     m_nRenderFX
    /// </summary>
    RenderFx RenderFx { get; set; }

    /// <summary>
    ///     m_clrColor
    /// </summary>
    Color32 RenderColor { get; set; }

    /// <summary>
    ///     m_bAllowFadeInView
    /// </summary>
    bool AllowFadeInView { get; set; }

    /// <summary>
    ///     m_fadeMinDist
    /// </summary>
    float FadeMinDistance { get; set; }

    /// <summary>
    ///     m_fadeMaxDist
    /// </summary>
    float FadeMaxDistance { get; set; }

    /// <summary>
    ///     m_flFadeScale
    /// </summary>
    float FadeScale { get; set; }

    /// <summary>
    ///     m_flShadowStrength
    /// </summary>
    float ShadowStrength { get; set; }

    /// <summary>
    ///     m_vecViewOffset
    /// </summary>
    Vector ViewOffset { get; }

    /// <summary>
    ///     Sets a BodyGroup by name.
    /// </summary>
    void SetBodyGroupByName(string name, int value);

    /// <summary>
    ///     Set the mesh group mask of this entity.
    /// </summary>
    void SetMaterialGroupMask(ulong mask);

    /// <summary>
    ///     Get the given attachment's index
    /// </summary>
    /// <returns>It returns 0 when the given attachment does not exist</returns>
    int LookupAttachment(string attachment);

    /// <summary>
    ///     Get the given attachment's origin and angles
    /// </summary>
    void GetAttachment(int attachment, out Vector origin, out Vector angles);

    /// <summary>
    ///     Get the index of the bone
    /// </summary>
    /// <returns>Starts with 0</returns>
    int LookupBone(string bone);

    /// <summary>
    /// </summary>
    void GetBoneTransform(int bone, out Matrix3x4 matrix);

    /// <summary>
    ///     Set model scale, will not work if the model has BoneMerge flag
    /// </summary>
    void SetModelScale(float scale);

    /// <summary>
    ///     Set collision bounding box
    /// </summary>
    void SetCollisionBounds(Vector mins, Vector maxs);

    /// <summary>
    ///     GlowProperty
    /// </summary>
    IGlowProperty GetGlowProperty();
}
