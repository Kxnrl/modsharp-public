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

using System;
using System.Runtime.InteropServices;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 48)]
public unsafe ref struct RnQueryShapeAttr
{
    [FieldOffset(0x0)]
    public InteractionLayers m_nInteractsWith;

    [FieldOffset(0x8)]
    public InteractionLayers m_nInteractsExclude;

    [FieldOffset(0x10)]
    public InteractionLayers m_nInteractsAs;

    [FieldOffset(0x18)]
    public fixed uint m_nEntityIdsToIgnore[2];

    [FieldOffset(0x20)]
    public fixed uint m_nOwnerIdsToIgnore[2];

    [FieldOffset(0x28)]
    public fixed ushort m_nHierarchyIds[2];

    [FieldOffset(0x2C)]
    private ushort m_nIncludedDetailLayers;

    [FieldOffset(0x2F)]
    public RnQueryObjectSet m_nObjectSetMask;

    [FieldOffset(0x30)]
    public CollisionGroupType m_nCollisionGroup;

    [FieldOffset(0x31)]
    private byte _flags;

    public RnQueryShapeAttr()
    {
        m_nEntityIdsToIgnore[0] = m_nEntityIdsToIgnore[1] = uint.MaxValue;
        m_nOwnerIdsToIgnore[0]  = m_nOwnerIdsToIgnore[1]  = uint.MaxValue;
        m_nHierarchyIds[0]      = m_nHierarchyIds[1]      = 0;

        m_nObjectSetMask = RnQueryObjectSet.All;

        HitSolid                  = true;
        ShouldIgnoreDisabledPairs = true;
        Unknown                   = true;
        m_nCollisionGroup         = CollisionGroupType.ConditionallySolid;

        m_nIncludedDetailLayers = ushort.MaxValue;
    }

    public void SetEntityToIgnore(IBaseEntity entityToIgnore, int index)
    {
        if (index is < 0 or >= 2)
        {
            throw new IndexOutOfRangeException("Index should be [0, 1]");
        }

        if (entityToIgnore.IsPlayerController)
        {
            throw new InvalidOperationException("YOU SHOULD USE IPlayerPawn NOT IPlayerController");
        }

        m_nEntityIdsToIgnore[index] = entityToIgnore.RefHandle.GetValue();

        if (entityToIgnore.OwnerEntity is { } owner)
        {
            m_nOwnerIdsToIgnore[index] = owner.RefHandle.GetValue();
        }

        if (entityToIgnore.GetCollisionProperty() is { } collisionProperty)
        {
            m_nHierarchyIds[index] = collisionProperty.CollisionAttribute.HierarchyId;
        }
    }

    private const byte HitSolidMask                         = 0x01; // 1 << 0
    private const byte HitSolidRequiresGenerateContactsMask = 0x02; // 1 << 1
    private const byte HitTriggerMask                       = 0x04; // 1 << 2
    private const byte ShouldIgnoreDisabledPairsMask        = 0x08; // 1 << 3
    private const byte IgnoreIfBothInteractWithHitBoxesMask = 0x10; // 1 << 4
    private const byte ForceHitEverythingMask               = 0x20; // 1 << 5
    private const byte UnknownMask                          = 0x40; // 1 << 6

    public bool HitSolid
    {
        get => (_flags & HitSolidMask) != 0;
        set
        {
            if (value)
            {
                _flags |= HitSolidMask;
            }
            else
            {
                _flags &= unchecked((byte) ~HitSolidMask);
            }
        }
    }

    public bool HitSolidRequiresGenerateContacts
    {
        get => (_flags & HitSolidRequiresGenerateContactsMask) != 0;
        set
        {
            if (value)
            {
                _flags |= HitSolidRequiresGenerateContactsMask;
            }
            else
            {
                _flags &= unchecked((byte) ~HitSolidRequiresGenerateContactsMask);
            }
        }
    }

    public bool HitTrigger
    {
        get => (_flags & HitTriggerMask) != 0;
        set
        {
            if (value)
            {
                _flags |= HitTriggerMask;
            }
            else
            {
                _flags &= unchecked((byte) ~HitTriggerMask);
            }
        }
    }

    public bool ShouldIgnoreDisabledPairs
    {
        get => (_flags & ShouldIgnoreDisabledPairsMask) != 0;
        set
        {
            if (value)
            {
                _flags |= ShouldIgnoreDisabledPairsMask;
            }
            else
            {
                _flags &= unchecked((byte) ~ShouldIgnoreDisabledPairsMask);
            }
        }
    }

    public bool IgnoreIfBothInteractWithHitBoxes
    {
        get => (_flags & IgnoreIfBothInteractWithHitBoxesMask) != 0;
        set
        {
            if (value)
            {
                _flags |= IgnoreIfBothInteractWithHitBoxesMask;
            }
            else
            {
                _flags &= unchecked((byte) ~IgnoreIfBothInteractWithHitBoxesMask);
            }
        }
    }

    public bool ForceHitEverything
    {
        get => (_flags & ForceHitEverythingMask) != 0;
        set
        {
            if (value)
            {
                _flags |= ForceHitEverythingMask;
            }
            else
            {
                _flags &= unchecked((byte) ~ForceHitEverythingMask);
            }
        }
    }

    public bool Unknown
    {
        get => (_flags & UnknownMask) != 0;
        set
        {
            if (value)
            {
                _flags |= UnknownMask;
            }
            else
            {
                _flags &= unchecked((byte) ~UnknownMask);
            }
        }
    }

    public static RnQueryShapeAttr PlayerMovement(InteractionLayers interactsWithLayers)
        => new ()
        {
            m_nObjectSetMask                 = RnQueryObjectSet.All,
            m_nCollisionGroup                = CollisionGroupType.PlayerMovement,
            HitSolid                         = true,
            HitSolidRequiresGenerateContacts = true,
            ShouldIgnoreDisabledPairs        = true,
            Unknown                          = true,
            m_nInteractsWith                 = interactsWithLayers,
            m_nInteractsAs                   = InteractionLayers.Player,
            m_nInteractsExclude              = InteractionLayers.None,
        };

    public static RnQueryShapeAttr Bullets()
        => new ()
        {
            m_nObjectSetMask          = RnQueryObjectSet.All,
            m_nCollisionGroup         = CollisionGroupType.ConditionallySolid,
            HitSolid                  = true,
            ShouldIgnoreDisabledPairs = true,
            Unknown                   = true,
            m_nInteractsWith          = UsefulInteractionLayers.FireBullets,
            m_nInteractsAs            = InteractionLayers.None,
            m_nInteractsExclude       = InteractionLayers.None,
        };

    public static RnQueryShapeAttr Knife()
        => new ()
        {
            m_nObjectSetMask          = RnQueryObjectSet.All,
            m_nCollisionGroup         = CollisionGroupType.Default,
            HitSolid                  = true,
            ShouldIgnoreDisabledPairs = true,
            Unknown                   = true,
            m_nInteractsWith          = UsefulInteractionLayers.SwingOrStab,
            m_nInteractsAs            = InteractionLayers.None,
            m_nInteractsExclude       = InteractionLayers.None,
        };
}
