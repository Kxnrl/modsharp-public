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

using System.Runtime.InteropServices;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Utilities;

namespace Sharp.Shared.Types;

public readonly unsafe struct TraceResult
{
    public IBaseEntity?              HitEntity       { get; }
    public float                     Fraction        { get; }
    public TraceRayType              RayType         { get; }
    public bool                      StartInSolid    { get; }
    public int                       Triangle        { get; }
    public Vector                    StartPosition   { get; }
    public Vector                    EndPosition     { get; }
    public Vector                    PlaneNormal     { get; }
    public PhysicsSurfaceProperties* SurfaceProp     { get; }
    public HitBoxData*               HitBoxData      { get; }
    public void*                     PhysicsBody     { get; }
    public void*                     PhysicsShape    { get; }
    public Vector                    HitPoint        { get; }
    public float                     HitOffset       { get; }
    public uint                      Contents        { get; }
    public short                     HitBoxBoneIndex { get; }

    public TraceResult(IBaseEntity? entity,
        float                       fraction,
        TraceRayType                rayType,
        bool                        startInSolid,
        int                         triangle,
        Vector                      startPosition,
        Vector                      endPosition,
        Vector                      planeNormal,
        PhysicsSurfaceProperties*   surface,
        HitBoxData*                 hitBox,
        void*                       physicsBody,
        void*                       physicsShape,
        Vector                      hitPoint,
        float                       hitOffset,
        uint                        contents,
        short                       hitBoxBoneIndex)
    {
        HitEntity       = entity;
        Fraction        = fraction;
        RayType         = rayType;
        StartInSolid    = startInSolid;
        Triangle        = triangle;
        StartPosition   = startPosition;
        EndPosition     = endPosition;
        PlaneNormal     = planeNormal;
        SurfaceProp     = surface;
        HitBoxData      = hitBox;
        PhysicsBody     = physicsBody;
        PhysicsShape    = physicsShape;
        HitPoint        = hitPoint;
        HitOffset       = hitOffset;
        Contents        = contents;
        HitBoxBoneIndex = hitBoxBoneIndex;
    }

    /// <summary>
    ///     Did the trace hit anything
    /// </summary>
    public bool DidHit()
        => Fraction < 1 || StartInSolid;

    /// <summary>
    ///     The HitGroup of the entity (e.g., Head, Chest).
    /// </summary>
    /// <returns>Returns <see cref="HitGroupType.Invalid"/> if no entity was hit.</returns>
    public HitGroupType HitGroup => HitEntity is not null ? HitBoxData->HitGroup : HitGroupType.Invalid;

    /// <summary>
    ///     The HitBox ID of the entity.
    /// </summary>
    /// <returns>Returns -1 if no entity was hit.</returns>
    public int HitBoxId => HitEntity is not null ? HitBoxData->HitBoxId : -1;

    /// <summary>
    ///     The HitBox name of the entity.
    /// </summary>
    /// <returns>Returns an empty string if no entity was hit.</returns>
    public string HitBoxName => HitEntity is not null ? Utils.ReadString(HitBoxData->pName) : string.Empty;

    /// <summary>
    ///     The name of the surface material.
    /// </summary>
    public string SurfaceName => SurfaceProp == null ? string.Empty : Utils.ReadString(SurfaceProp->pName);
}

[StructLayout(LayoutKind.Explicit, Size = 112)]
public readonly unsafe struct TraceResultStruct
{
    [FieldOffset(0)]
    public readonly nint HitEntity;

    [FieldOffset(8)]
    public readonly float Fraction;

    [FieldOffset(12)]
    public readonly TraceRayType RayType;

    [FieldOffset(13)]
    public readonly bool StartInSolid;

    [FieldOffset(16)]
    public readonly int Triangle;

    [FieldOffset(20)]
    public readonly Vector StartPosition;

    [FieldOffset(32)]
    public readonly Vector EndPosition;

    [FieldOffset(44)]
    public readonly Vector PlaneNormal;

    [FieldOffset(56)]
    public readonly PhysicsSurfaceProperties* SurfaceProp;

    [FieldOffset(64)]
    public readonly HitBoxData* HitBoxData;

    [FieldOffset(72)]
    public readonly void* PhysicsBody;

    [FieldOffset(80)]
    public readonly void* PhysicsShape;

    [FieldOffset(88)]
    public readonly Vector HitPoint;

    [FieldOffset(100)]
    public readonly float HitOffset;

    [FieldOffset(104)]
    public readonly uint Contents;

    [FieldOffset(108)]
    public readonly short HitGroupHitBoxBoneIndex;
}
