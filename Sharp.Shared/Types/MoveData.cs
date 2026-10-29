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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types.Tier;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct MoveData
{
    // Windows has a 4-byte pad at 0xe4 that Linux does not
    private static readonly nint PlatformOffset =
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? -4 : 0;

    [FieldOffset(0x0)]
    public byte MoveDataFlags;

    [FieldOffset(0x4)]
    public CEntityHandle<IBasePlayerPawn> PlayerHandle;

    [FieldOffset(0x8)]
    public Vector AbsViewAngles;

    [FieldOffset(0x14)]
    public Vector ViewAngles;

    [FieldOffset(0x20)]
    public Vector LastMovementImpulses;

    [FieldOffset(0x2c)]
    public float ForwardMove;

    [FieldOffset(0x30)]
    public float SideMove;

    [FieldOffset(0x34)]
    public float UpMove;

    [FieldOffset(0x38)]
    public Vector Velocity;

    [FieldOffset(0x44)]
    public Vector Angles;

    [FieldOffset(0x60)]
    public CUtlVector<SubTickMove> SubTickMoves;

    [FieldOffset(0x78)]
    public CUtlVector<SubTickAttack> SubTickAttacks;

    [FieldOffset(0x98)]
    public CUtlVector<TouchListEntry> TouchList;

    [FieldOffset(0xb0)]
    public Vector CollisionNormal;

    [FieldOffset(0xbc)]
    public Vector GroundNormal;

    [FieldOffset(0xc8)]
    public Vector AbsOrigin;

    [FieldOffset(0xd4)]
    public int StartTick;

    [FieldOffset(0xd8)]
    public int EndTick;

    [FieldOffset(0xdc)]
    public float SubtickStartFraction;

    [FieldOffset(0xe0)]
    public float SubtickEndFraction;

    private unsafe ref byte Base
    {
        get
        {
            fixed (void* ptr = &this)
            {
                return ref Unsafe.AsRef<byte>(ptr);
            }
        }
    }

    public unsafe Vector OutWishVel // Win: 0xe8
    {
        get => Unsafe.ReadUnaligned<Vector>(ref Unsafe.AddByteOffset(ref Base, 0xe8 + PlatformOffset));
        set => Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref Base,        0xe8 + PlatformOffset), value);
    }

    public unsafe Vector OldAngles // Win: 0xf4
    {
        get => Unsafe.ReadUnaligned<Vector>(ref Unsafe.AddByteOffset(ref Base, 0xf4 + PlatformOffset));
        set => Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref Base,        0xf4 + PlatformOffset), value);
    }

    public unsafe Vector2D WalkWishVel // Win: 0x100
    {
        get => Unsafe.ReadUnaligned<Vector2D>(ref Unsafe.AddByteOffset(ref Base, 0x100 + PlatformOffset));
        set => Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref Base,          0x100 + PlatformOffset), value);
    }

    public unsafe float MaxSpeed // Win: 0x120
    {
        get => Unsafe.ReadUnaligned<float>(ref Unsafe.AddByteOffset(ref Base, 0x120 + PlatformOffset));
        set => Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref Base,       0x120 + PlatformOffset), value);
    }

    public unsafe float ClientMaxSpeed // Win: 0x124
    {
        get => Unsafe.ReadUnaligned<float>(ref Unsafe.AddByteOffset(ref Base, 0x124 + PlatformOffset));
        set => Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref Base,       0x124 + PlatformOffset), value);
    }

    public unsafe bool InAir // Win: 0x138
    {
        get => Unsafe.ReadUnaligned<bool>(ref Unsafe.AddByteOffset(ref Base, 0x138 + PlatformOffset));
        set => Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref Base,      0x138 + PlatformOffset), value);
    }

    public unsafe bool GameCodeMovedPlayer // Win: 0x139
    {
        get => Unsafe.ReadUnaligned<bool>(ref Unsafe.AddByteOffset(ref Base, 0x139 + PlatformOffset));
        set => Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref Base,      0x139 + PlatformOffset), value);
    }
}

[StructLayout(LayoutKind.Explicit, Size = 208, Pack = 8)]
public struct TouchListEntry
{
    [FieldOffset(0)]
    public Vector DeltaVelocity;

    [FieldOffset(16)]
    private byte _trace;

    [UnscopedRef]
    public ref readonly GameTrace Trace => ref Unsafe.As<byte, GameTrace>(ref _trace);
}

[StructLayout(LayoutKind.Explicit, Size = 24, Pack = 8)]
public struct SubTickAttack
{
    [FieldOffset(0)]
    public float When;

    [FieldOffset(8)]
    public UserCommandButtons Button;

    [FieldOffset(16)]
    public bool Pressed;
}

[StructLayout(LayoutKind.Explicit, Size = 32, Pack = 8)]
public struct SubTickMove
{
    [FieldOffset(0)]
    public float When;

    [FieldOffset(8)]
    public UserCommandButtons Button;

    [FieldOffset(16)]
    public bool Pressed;

    [FieldOffset(16)]
    public float AnalogForwardDelta;

    [FieldOffset(20)]
    public float AnalogLeftDelta;

    /// <summary>
    ///     Not a delta: the game accumulates CSubtickMoveStep.pitch_delta step by step and writes the absolute pitch of
    ///     this step here, regardless of Button; every step writes it
    /// </summary>
    [FieldOffset(24)]
    public float AnalogPitchDelta;

    /// <summary>
    ///     Not a delta: the game accumulates CSubtickMoveStep.yaw_delta step by step and writes the absolute yaw of
    ///     this step here, regardless of Button; every step writes it
    /// </summary>
    [FieldOffset(28)]
    public float AnalogYawDelta;

    public bool IsAnalogInput => Button == 0;

    public override string ToString()
        => $"When={When:F2}, Button={Button}, Pressed={Pressed}";
};
