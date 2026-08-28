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

#ifndef CSTRIKE_TYPE_CCSSCRIPT_H
#define CSTRIKE_TYPE_CCSSCRIPT_H

#include "murmurhash.h"

#include "cstrike/cstrike.h"
#include "cstrike/type/CUtlAutoList.h"
#include "cstrike/type/CUtlHashTable.h"
#include "cstrike/type/CUtlSymbolLarge.h"
#include "cstrike/type/CUtlVector.h"

#include <v8.h>

#include <cstdint>
#include <string_view>

class CUtlString;
using CGlobalSymbol = CUtlSymbolLarge;

// Hash/Equal functors for CCSBaseScript hash tables
struct GlobalSymbolHashFunctor
{
    unsigned int operator()(CGlobalSymbol s) const
    {
        // GlobalSymbol stores a pointer; the hash is stored at ptr[-4]
        const char* p = s.Get();
        if (!p || !s.IsValid())
            return 0;
        return *reinterpret_cast<const uint32_t*>(p - 4);
    }
};

struct PointerEqualFunctor
{
    bool operator()(CGlobalSymbol a, CGlobalSymbol b) const
    {
        // Pointer identity comparison (interned symbols)
        return a == b;
    }
};

// m_mapEntityWrappers hashes its 4-byte key with MurmurHash2 under a table-specific
// seed (MURMURHASH_SCRIPTWRAPPER_SEED).
struct ScriptEntityWrapperHashFunctor
{
    unsigned int operator()(uint32_t key) const
    {
        return MurmurHash2(std::string_view(reinterpret_cast<const char*>(&key), sizeof(key)),
                           MURMURHASH_SCRIPTWRAPPER_SEED);
    }
};

// m_mapObjectWrappers does NOT hash at all — the engine feeds the key straight through
// as the hash, with no mixing whatsoever.
struct ScriptObjectWrapperHashFunctor
{
    unsigned int operator()(uint32_t key) const { return key; }
};

// CCSBaseScript — base class for cs_script entity scripts.
//
// Inherits: IEntityListener (vtable +0x00), CUtlAutoList<CCSBaseScript> (vtable +0x08)
struct CCSBaseScript
{
private:
    void* vtable_IEntityListener; // 0x00
public:
    CCSBaseScript() = delete;

    CUtlAutoListNode<CCSBaseScript> m_AutoListNode; // 0x08  (vtable, next, prev)

    // 0x20 monotonically increasing, never resets. The ctor writes it 32-bit and the
    // global counter is itself a dword, so this is NOT 64-bit.
    uint32_t                m_nScriptIndex;             // 0x20
    // The ctor never writes 0x24. Reading m_nScriptIndex as 64-bit would splice this
    // uninitialised gap into the high half and print garbage.
    uint32_t                m_nUnused0x24;              // 0x24
    v8::Global<v8::Context> m_hContext;                 // 0x28
    bool                    m_bActive;                  // 0x30
    // UNVERIFIED: the ctor never writes this byte, and nothing in the cs_script code
    // range touches [reg+0x31]. Placeholder only — do not rely on it.
    bool                    m_bLegacySourceTS;          // 0x31

    CUtlVector<CUtlString> m_vecRegisteredClasses;      // 0x38

    // Class name → FunctionTemplate (Entity, CSPlayerPawn, CSWeaponBase, etc.)
    CUtlHashtable<CGlobalSymbol, v8::Global<v8::FunctionTemplate>*, GlobalSymbolHashFunctor, PointerEqualFunctor> m_mapClassTemplates; // 0x50

    // Enum name → Object (CSDamageTypes, CSHitGroup, etc.)
    CUtlHashtable<CGlobalSymbol, v8::Global<v8::Object>*, GlobalSymbolHashFunctor, PointerEqualFunctor> m_mapEnums; // 0x70

    // Callback name → Function (OnPlayerJump, OnActivate, OnRoundStart, etc.)
    CUtlHashtable<CGlobalSymbol, v8::Global<v8::Function>*, GlobalSymbolHashFunctor, PointerEqualFunctor> m_mapCallbacks; // 0x90

    // Entity handle → wrapped V8 object (cached entity wrappers)
    CUtlHashtable<CBaseHandle, v8::Global<v8::Object>*, ScriptEntityWrapperHashFunctor> m_mapEntityWrappers; // 0xB0

    // 32-bit key → wrapped V8 object (non-entity wrappers). The engine passes this key
    // as a dword, not a pointer, and uses it unhashed.
    CUtlHashtable<uint32_t, v8::Global<v8::Object>*, ScriptObjectWrapperHashFunctor> m_mapObjectWrappers; // 0xD0
};

static_assert(offsetof(CCSBaseScript, m_AutoListNode) == 0x08);
static_assert(offsetof(CCSBaseScript, m_nScriptIndex) == 0x20);
static_assert(offsetof(CCSBaseScript, m_hContext) == 0x28);
static_assert(offsetof(CCSBaseScript, m_bActive) == 0x30);
static_assert(offsetof(CCSBaseScript, m_vecRegisteredClasses) == 0x38);
static_assert(offsetof(CCSBaseScript, m_mapClassTemplates) == 0x50);
static_assert(offsetof(CCSBaseScript, m_mapEnums) == 0x70);
static_assert(offsetof(CCSBaseScript, m_mapCallbacks) == 0x90);
static_assert(offsetof(CCSBaseScript, m_mapEntityWrappers) == 0xB0);
static_assert(offsetof(CCSBaseScript, m_mapObjectWrappers) == 0xD0);

// Offsets alone are not enough: a field can sit at the right offset and still be
// read at the wrong width (m_nScriptIndex was declared uint64_t while the engine
// writes 32 bits, and no offset assert caught it). Pin the widths too.
static_assert(sizeof(CCSBaseScript::m_nScriptIndex) == 4);
static_assert(sizeof(CCSBaseScript::m_hContext) == 8);
// Each CUtlHashtable is {ptr, ptr, ptr, int32 m_nMinSize = 32, bool m_bSizeLocked},
// 0x20 bytes total — matches the five 0x20-strided init blocks in the ctor.
static_assert(sizeof(CCSBaseScript::m_mapClassTemplates) == 0x20);
static_assert(sizeof(CCSBaseScript::m_mapEntityWrappers) == 0x20);

// m_AutoListNode itself is kept because the engine's layout depends on it, but the
// iteration helpers and the global list pointer are not wired up to anything: nothing
// ever assigned g_pScriptEntityList, so any use would have dereferenced null.

#endif