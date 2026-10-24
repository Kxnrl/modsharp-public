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

namespace Sharp.Shared.Enums;

public enum VariantFieldType : byte
{
    Void                                     = 0x0,
    Float32                                  = 0x1,
    String                                   = 0x2,
    Vector                                   = 0x3,
    Quaternion                               = 0x4,
    Int32                                    = 0x5,
    Boolean                                  = 0x6,
    Int16                                    = 0x7,
    Character                                = 0x8,
    Color32                                  = 0x9,
    Embedded                                 = 0xa,
    Custom                                   = 0xb,
    ClassPointer                             = 0xc,
    EHandle                                  = 0xd,
    PositionVector                           = 0xe,
    Time                                     = 0xf,
    Tick                                     = 0x10,
    SoundName                                = 0x11,
    Input                                    = 0x12,
    Function                                 = 0x13,
    VMatrix                                  = 0x14,
    VMatrixWorldSpace                        = 0x15,
    Matrix3X4WorldSpace                      = 0x16,
    Interval                                 = 0x17,
    Unused                                   = 0x18,
    Vector2D                                 = 0x19,
    Int64                                    = 0x1a,
    Vector4D                                 = 0x1b,
    Resource                                 = 0x1c,
    TypeUnknown                              = 0x1d,
    CString                                  = 0x1e,
    HScript                                  = 0x1f,
    Variant                                  = 0x20,
    Uint64                                   = 0x21,
    Float64                                  = 0x22,
    PositiveIntegerOrNull                    = 0x23,
    HScriptNewInstance                       = 0x24,
    Uint32                                   = 0x25,
    UtlStringToken                           = 0x26,
    QAngle                                   = 0x27,
    NetworkOriginCellQuantizedVector         = 0x28,
    HMaterial                                = 0x29,
    HModel                                   = 0x2a,
    NetworkQuantizedVector                   = 0x2b,
    NetworkQuantizedFloat                    = 0x2c,
    DirectionVectorWorldSpace                = 0x2d,
    QAngleWorldSpace                         = 0x2e,
    QuaternionWorldSpace                     = 0x2f,
    HHcriptLightBinding                      = 0x30,
    V8Value                                  = 0x31,
    V8Object                                 = 0x32,
    V8Array                                  = 0x33,
    V8CallbackInfo                           = 0x34,
    UtlString                                = 0x35,
    NetworkOriginCellQuantizedPositionVector = 0x36,
    HRenderTexture                           = 0x37,
    HParticleSystemDefinition                = 0x38,
    Uint8                                    = 0x39,
    Uint16                                   = 0x3a,
    CTransform                               = 0x3b,
    CTransformWorldSpace                     = 0x3c,
    HPostProcessing                          = 0x3d,
    Matrix3X4                                = 0x3e,
    Shim                                     = 0x3f,
    CMotionTransform                         = 0x40,
    CMotionTransformWorldSpace               = 0x41,
    AttachmentHandle                         = 0x42,
    AmmoIndex                                = 0x43,
    ConditionId                              = 0x44,
    AiScheduleBits                           = 0x45,
    ModifierHandle                           = 0x46,
    RotationVector                           = 0x47,
    RotationVectorWorldSpace                 = 0x48,
    HVData                                   = 0x49,
    Scale32                                  = 0x4a,
    StringAndToken                           = 0x4b,
    EngineTime                               = 0x4c,
    EngineTick                               = 0x4d,
    WorldGroupId                             = 0x4e,
    GlobalSymbol                             = 0x4f,
    HNmGraphDefinition                       = 0x50,
    NetworkQuantizedVectorWS                 = 0x51,
    NetworkOriginCellQuantizedVectorWS       = 0x52,
    TypeCount                                = 0x53,
}
