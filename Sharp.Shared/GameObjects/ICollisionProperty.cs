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
using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameObjects;

[NetClass("CCollisionProperty")]
public interface ICollisionProperty : IEntityObject
{
    /// <summary>
    ///     Solid type, typically related to collision
    /// </summary>
    SolidType SolidType { get; set; }

    /// <summary>
    ///     VPhysics type to enable
    /// </summary>
    byte EnablePhysics { get; set; }

    /// <summary>
    ///     Solid Flags
    /// </summary>
    byte SolidFlags { get; set; }

    /// <summary>
    ///     Collision group
    /// </summary>
    CollisionGroupType CollisionGroup { get; set; }

    /// <summary>
    ///     VPhysics collision attribute
    /// </summary>
    IVPhysicsCollisionAttribute CollisionAttribute { get; }

    /// <summary>
    ///     Bounding Box Mins
    /// </summary>
    Vector Mins { get; set; }

    /// <summary>
    ///     Bounding Box Maxs
    /// </summary>
    Vector Maxs { get; set; }

    /// <summary>
    ///     Bounding capsule/sphere radius
    /// </summary>
    float BoundingRadius { get; set; }

    /// <summary>
    ///     m_nSurroundType
    /// </summary>
    SurroundingBoundsType SurroundType { get; set; }
}

public interface IVPhysicsCollisionAttribute : ISchemaObject
{
    /// <summary>
    ///     Collision group
    /// </summary>
    CollisionGroupType CollisionGroup { get; set; }

    uint   EntityId    { get; set; }
    uint   OwnerId     { get; set; }
    ushort HierarchyId { get; set; }

    InteractionLayers InteractsAs      { get; set; }
    InteractionLayers InteractsWith    { get; set; }
    InteractionLayers InteractsExclude { get; set; }
}
