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

namespace Sharp.Shared.Types.CppProtobuf;

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x24)]
public unsafe struct CSGOInterpolationInfoPB
{
    [FieldOffset(0x18)]
    public float Fraction;

    [FieldOffset(0x1C)]
    public int SrcTick;

    [FieldOffset(0x20)]
    public int DestTick;
}

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x1C)]
public unsafe struct CSGOInterpolationInfoPB_CL
{
    [FieldOffset(0x18)]
    public float Fraction;
}

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x78)]
public unsafe struct CCSGOInputHistoryEntryPb
{
    [FieldOffset(0x18)]
    public CMsgVector* ViewAngles;

    [FieldOffset(0x20)]
    public CSGOInterpolationInfoPB_CL* ClInterp;

    [FieldOffset(0x28)]
    public CSGOInterpolationInfoPB* SvInterp0;

    [FieldOffset(0x30)]
    public CSGOInterpolationInfoPB* SvInterp1;

    [FieldOffset(0x38)]
    public CSGOInterpolationInfoPB* PlayerInterp;

    [FieldOffset(0x40)]
    public CMsgVector* ShootPosition;

    [FieldOffset(0x48)]
    public CMsgVector* TargetHeadPositionCheck;

    [FieldOffset(0x50)]
    public CMsgVector* TargetAbsPositionCheck;

    [FieldOffset(0x58)]
    public CMsgVector* TargetAngPositionCheck;

    [FieldOffset(0x60)]
    public int RenderTickCount;

    [FieldOffset(0x64)]
    public float RenderTickFraction;

    [FieldOffset(0x68)]
    public int PlayerTickCount;

    [FieldOffset(0x6C)]
    public float PlayerTickFraction;

    [FieldOffset(0x70)]
    public int FrameNumber;

    [FieldOffset(0x74)]
    public int TargetEntIndex;
}

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x38)]
public struct CSubtickMoveStepPb
{
    [FieldOffset(0x18)]
    public UserCommandButtons Buttons;

    [FieldOffset(0x20)]
    public bool Pressed;

    [FieldOffset(0x24)]
    public float When;

    [FieldOffset(0x28)]
    public float AnalogForwardDelta;

    [FieldOffset(0x2C)]
    public float AnalogLeftDelta;

    [FieldOffset(0x30)]
    public float AnalogPitchDelta;

    [FieldOffset(0x34)]
    public float AnalogYawDelta;
}

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 32)]
public ref struct CInButtonStatePb
{
    [FieldOffset(0x18)]
    public UserCommandButtons ButtonPressed;

    [FieldOffset(0x18 + 8)]
    public UserCommandButtons ButtonChanged;

    [FieldOffset(0x18 + 8 + 8)]
    public UserCommandButtons ButtonScroll;
}

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x88)]
public unsafe ref struct CBaseUserCmdPb
{
    [FieldOffset(0x18)]
    public RepeatedField<CSubtickMoveStepPb> SubtickMoves;

    [FieldOffset(0x38)]
    public CInButtonStatePb* ButtonState;

    [FieldOffset(0x40)]
    public CMsgVector* ViewAngles;

    [FieldOffset(0x48)]
    public void* ExecutionNotes;

    [FieldOffset(0x50)]
    public int LegacyCommandNumber;

    [FieldOffset(0x54)]
    public int ClientTick;

    [FieldOffset(0x58)]
    public float ForwardMove;

    [FieldOffset(0x5C)]
    public float SideMove;

    [FieldOffset(0x60)]
    public float UpMove;

    [FieldOffset(0x64)]
    public int Impulse;

    [FieldOffset(0x68)]
    public int WeaponSelect;

    [FieldOffset(0x6C)]
    public int RandomSeed;

    [FieldOffset(0x70)]
    public int MouseX;

    [FieldOffset(0x74)]
    public int MouseY;

    [FieldOffset(0x78)]
    public uint PredictionOffsetTicks;

    [FieldOffset(0x7C)]
    public uint ConsumedServerAngleChanges;

    [FieldOffset(0x80)]
    public int CmdFlags;

    [FieldOffset(0x84)]
    public CEntityHandle<IPlayerPawn> PawnEntityHandle;
}

[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x48)]
public unsafe ref struct CCSGOUserCmdPb
{
    [FieldOffset(0x18)]
    public RepeatedField<CCSGOInputHistoryEntryPb> InputHistoryEntry;

    [FieldOffset(0x30)]
    public CBaseUserCmdPb* BaseUserCmd;

    [FieldOffset(0x38)]
    public bool LeftHandDesired;

    [FieldOffset(0x39)]
    public bool IsPredictingBodyShot;

    [FieldOffset(0x3A)]
    public bool IsPredictingHeadShot;

    [FieldOffset(0x3B)]
    public bool IsPredictingKillShot;

    [FieldOffset(0x3C)]
    public int Attack1StartHistoryIndex;

    [FieldOffset(0x40)]
    public int Attack2StartHistoryIndex;
}
