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

#ifndef CSTRIKE_TYPE_SCRIPTVALUE_H
#define CSTRIKE_TYPE_SCRIPTVALUE_H

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/type/Vector.h"

#include <cstddef>
#include <cstdint>

// Mirror of Sharp.Shared.Types.ScriptValue (C#). The byte layout MUST match:
//   type    @ 0x00 (int32)
//   payload @ 0x08 (union; 8-byte aligned because double/pointer are present)
//   total     24 bytes
// Always passed across the C#<->C++ boundary by pointer (never by value), like CVValue_t.
enum class ScriptValueType : int32_t
{
    Null   = 0,
    Bool   = 1,
    Number = 2,
    String = 3,
    Entity = 4,
    Vector = 5,
    Object = 6,  // handle = index into the per-call V8 handle table (a live JS object)
    Array  = 7,  // handle = index into the per-call V8 handle table (a live JS array)
    Error  = -1, // wire-only sentinel: a thrown error (str = message), not a value; outside the value range
};

struct ScriptValue
{
    ScriptValueType type; // 0x00

    union // 0x08
    {
        bool        b;
        double      number;
        const char* str;    // UTF8; for JS->C# args valid only during the call
        uint32_t    entity; // packed CEntityHandle value
        uint32_t    handle; // Object/Array: index into the per-call V8 handle table
        float       vec[3]; // mirrors C# Vector (3x float)
    };

    ScriptValue() = delete;

    ScriptValue(ScriptValueType t) : type(t), b(false) {}

    ScriptValue(const bool value) : type(ScriptValueType::Bool), b(value) {}
    ScriptValue(const double value) : type(ScriptValueType::Number), number(value) {}
    ScriptValue(const char* value) : type(ScriptValueType::String), str(value) {}
    ScriptValue(const CBaseEntity* value) : type(ScriptValueType::Entity), entity(value->GetActualEHandle().ToInt()) {}
    ScriptValue(const uint32_t value, ScriptValueType t) : type(t), handle(value) {}
    ScriptValue(const Vector& value) : type(ScriptValueType::Vector), vec{value.x, value.y, value.z} {}
};

static_assert(sizeof(ScriptValue) == 24, "ScriptValue must be 24 bytes to match the C# layout");
static_assert(offsetof(ScriptValue, type) == 0, "ScriptValue::type must be at offset 0");
static_assert(offsetof(ScriptValue, number) == 8, "ScriptValue payload union must be at offset 8 (C# pins [FieldOffset(8)])");

#endif