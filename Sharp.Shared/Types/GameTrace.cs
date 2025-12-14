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

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 40)]
public readonly ref struct RnCollisionAttr
{
    [FieldOffset(0)]
    public readonly InteractionLayers InteractsAs;

    [FieldOffset(8)]
    public readonly InteractionLayers InteractsWith;

    [FieldOffset(16)]
    public readonly InteractionLayers InteractsExclude;

    [FieldOffset(24)]
    public readonly uint EntityId;

    [FieldOffset(28)]
    public readonly uint OwnerId;

    [FieldOffset(32)]
    public readonly ushort HierarchyId;

    [FieldOffset(34)]
    public readonly CollisionGroupType CollisionGroup;

    [FieldOffset(35)]
    public readonly byte CollisionFunctionMask;
}

/// <summary>
///     CGameTrace must be readonly, changing it can cause crashes
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly unsafe ref struct GameTrace
{
    [FieldOffset(0)]
    public readonly PhysicsSurfaceProperties* SurfaceProp;

    [FieldOffset(8)]
    public readonly nint Entity;

    [FieldOffset(16)]
    public readonly HitBoxData* HitBoxData;

    [FieldOffset(24)]
    public readonly nint PhysicsBody;

    [FieldOffset(32)]
    public readonly nint PhysicsShape;

    [FieldOffset(40)]
    public readonly uint Contents;

    [FieldOffset(80)]
    public readonly RnCollisionAttr ShapeAttributes;

    [FieldOffset(120)]
    public readonly Vector StartPosition;

    [FieldOffset(132)]
    public readonly Vector EndPosition;

    [FieldOffset(144)]
    public readonly Vector PlaneNormal;

    [FieldOffset(156)]
    public readonly Vector HitPoint;

    [FieldOffset(168)]
    public readonly float HitOffset;

    [FieldOffset(172)]
    public readonly float Fraction;

    [FieldOffset(176)]
    public readonly float Triangle;

    [FieldOffset(180)]
    public readonly short HitBoxBoneIndex;

    [FieldOffset(182)]
    public readonly TraceRayType RayType;

    [FieldOffset(183)]
    public readonly bool StartInSolid;

    [FieldOffset(184)]
    public readonly bool ExactHitPoint;

    public bool DidHit()
        => Fraction < 1.0f || StartInSolid;
}

[StructLayout(LayoutKind.Explicit)]
public readonly unsafe ref struct HitBoxData
{
    [FieldOffset(0)]
    public readonly sbyte* pName;

    [FieldOffset(56)]
    public readonly HitGroupType HitGroup;

    [FieldOffset(72)]
    public readonly int HitBoxId;
}

[StructLayout(LayoutKind.Explicit)]
public readonly unsafe ref struct PhysicsSurfaceProperties
{
    [FieldOffset(0)]
    public readonly sbyte* pName;
}

[StructLayout(LayoutKind.Explicit, Size = 48)]
public readonly struct TraceShapeRay
{
    [FieldOffset(0)]
    public readonly TraceShapeLine Line;

    [FieldOffset(0)]
    public readonly TraceShapeSphere Sphere;

    [FieldOffset(0)]
    public readonly TraceShapeHull Hull;

    [FieldOffset(0)]
    public readonly TraceShapeCapsule Capsule;

    [FieldOffset(40)]
    public readonly TraceRayType Type;

    public TraceShapeRay(TraceShapeLine line)
    {
        Line = line;
        Type = TraceRayType.Line;
    }

    public TraceShapeRay(TraceShapeSphere sphere)
    {
        Sphere = sphere;
        Type   = TraceRayType.Sphere;
    }

    public TraceShapeRay(TraceShapeHull hull)
    {
        Hull = hull;
        Type = TraceRayType.Hull;
    }

    public TraceShapeRay(TraceShapeCapsule capsule)
    {
        Capsule = capsule;
        Type    = TraceRayType.Capsule;
    }
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct TraceShapeLine
{
    public Vector Start;
    public float  Radius;
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct TraceShapeSphere
{
    public Vector Center;
    public float  Radius;
}

[StructLayout(LayoutKind.Sequential, Size = 24)]
public struct TraceShapeHull
{
    public Vector Mins;
    public Vector Maxs;
}

[StructLayout(LayoutKind.Sequential, Size = 28)]
public struct TraceShapeCapsule
{
    public Vector m_vCenter1;
    public Vector m_vCenter2;
    public float  Radius;
}
