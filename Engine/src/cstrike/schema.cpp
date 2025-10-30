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

#include "address.h"
#include "bridge/adapter.h"
#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "types.h"
#include "vhook/call.h"

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/interface/ISchemaSystem.h"
#include "cstrike/schema.h"

#include "cstrike/type/CNetworkStateChangedFieldInfo.h"
#include "cstrike/type/CUtlString.h"
#include "cstrike/type/CUtlTSHash.h"
#include "cstrike/type/CUtlVector.h"

#include <cstdint>
#include <unordered_map>
#include <unordered_set>

// for coreclr
struct SchemaClassField_t
{
    CUtlString           name;
    CUtlString           type;
    int32_t              offset;
    bool                 networked;
    SchemaTypeCategory_t category;
};

struct SchemaClass_t
{
    CUtlString                     name;
    int32_t                        chain;
    CUtlVector<SchemaClassField_t> fields;
    CUtlVector<CUtlString*>        baseClassList;
};

using SchemaKeyValueMap_t = std::unordered_map<uint64_t, SchemaKey>;
using SchemaTableMap_t    = std::unordered_map<uint64_t, SchemaKeyValueMap_t>;

static CUtlVector<SchemaClass_t*> g_SchemaList;
static SchemaKeyValueMap_t        g_SchemaMap{};

static bool IsFieldNetworked(const SchemaClassFieldData_t& field)
{
    static auto networkEnabled = MurmurHash2("MNetworkEnable", MURMURHASH_SEED_MODSHARP);

    for (int i = 0; i < field.m_nMetadataCount; i++)
    {
        if (networkEnabled == MurmurHash2(field.m_pMetadata[i].m_name, MURMURHASH_SEED_MODSHARP))
            return true;
    }

    return false;
}

int32_t schemas::FindChainOffset(const char* className)
{
    char keyBuffer[256];
    snprintf(keyBuffer, sizeof(keyBuffer), "%s->__m_pChainEntity", className);

    if (const auto& it = g_SchemaMap.find(MurmurHash2(keyBuffer, MURMURHASH_SEED_MODSHARP)); it != g_SchemaMap.end())
    {
        return it->second.offset;
    }

    return 0;
}

SchemaKey schemas::GetOffset(const char* className, const char* memberName)
{
    char key_buffer[256];
    snprintf(key_buffer, sizeof(key_buffer), "%s->%s", className, memberName);

    if (const auto& it = g_SchemaMap.find(MurmurHash2(key_buffer, MURMURHASH_SEED_MODSHARP)); it != g_SchemaMap.end())
    {
        return it->second;
    }

    FatalError("GetFieldOffset(): '%s' was not found in '%s'!", memberName, className);

    return {.offset = 0, .networked = false, .valid = false};
}

SchemaKey schemas::GetOffset(uint32_t hashKey)
{
    if (const auto& it = g_SchemaMap.find(hashKey); it != g_SchemaMap.end())
    {
        // it->second is the SchemaOffsetInfo_t struct
        return it->second;
    }

    return {.offset = 0, .networked = false, .valid = false};
}

void NetworkStateChanged(uintptr_t chainEntity, uint32_t offset, uint32_t nArrayIndex, uint32_t nPathIndex)
{
    CNetworkStateChangedInfo info(offset, nArrayIndex, nPathIndex);

    address::server::NetworkStateChanged(reinterpret_cast<void*>(chainEntity), info);
}

void SetStateChanged(CBaseEntity* pEntity, uint32_t offset, uint32_t nArrayIndex, uint32_t nPathIndex)
{
    CNetworkStateChangedInfo info(offset, nArrayIndex, nPathIndex);

    static auto fnOffset = g_pGameData->GetVFunctionIndex("CBaseEntity::StateChanged");
    CALL_VIRTUAL(void, fnOffset, pEntity, &info);

    /*if (gpGlobals)
        pEntity->m_lastNetworkChange(gpGlobals->flCurTime);

    pEntity->m_isSteadyState(0);*/
}

void SetStructStateChanged(void* pEntity, uint32_t offset)
{
    CNetworkStateChangedInfo info(offset, 0xFFFFFFFF, 0xFFFFFFFF);
    CALL_VIRTUAL(void, 1, pEntity, &info);
}

static void BuildClassSchemaRecursive(SchemaClass_t*                                derived_schema_class,
                                      SchemaClassInfoData_t*                        current_class_info,
                                      std::unordered_set<std::string_view>&         added_field_names,
                                      std::unordered_map<std::string, const char*>& override_fields)
{
    std::string_view class_name = current_class_info->GetName();
    if (strcmp(derived_schema_class->name.Get(), current_class_info->GetName()) != 0)
    {
        derived_schema_class->baseClassList.AddToTail(new CUtlString(current_class_info->GetName()));
    }

    constexpr auto type_override = MurmurHash2("MNetworkVarTypeOverride", MURMURHASH_SEED_MODSHARP);

    auto metadata = current_class_info->GetStaticMetadata();
    for (int i = 0; i < current_class_info->GetMetadataSize(); i++)
    {
        auto& m = metadata[i];

        if (MurmurHash2(m.m_name, MURMURHASH_SEED_MODSHARP) == type_override)
        {
            override_fields[reinterpret_cast<char**>(m.m_value)[0]] = reinterpret_cast<char**>(m.m_value)[1];
        }
    }

    const auto* fields = current_class_info->GetFields();
    for (int i = 0; i < current_class_info->GetFieldsSize(); ++i)
    {
        const auto&            field = fields[i];
        const std::string_view field_name(field.m_pszName);

        if (field_name == "__m_pChainEntity")
        {
            derived_schema_class->chain = field.m_nSingleInheritanceOffset;
        }

        if (added_field_names.contains(field_name))
        {
            continue;
        }

        const int32_t field_offset = field.m_nSingleInheritanceOffset;
        if (field_offset < 0) [[unlikely]]
        {
            FatalError("Offset of '%s' in class '%s' is negative!", field.m_pszName, current_class_info->GetName());
        }

        const auto is_field_networked = IsFieldNetworked(field);

        auto new_field = derived_schema_class->fields.AddToTailGetPtr();

        auto it = override_fields.find(field_name.data());
        if (it != override_fields.end())
        {
            new_field->type = it->second;
            if (field.m_pType->m_eTypeCategory == SCHEMA_TYPE_POINTER)
            {
                new_field->type.Append("*");
            }
        }
        else
        {
            new_field->type = field.m_pType->m_pszTypeName;
        }

        new_field->name      = field.m_pszName;
        new_field->offset    = field_offset;
        new_field->networked = is_field_networked;
        new_field->category  = field.m_pType->m_eTypeCategory;

        char key_buffer[512];
        snprintf(key_buffer, sizeof(key_buffer), "%s->%s", derived_schema_class->name.Get(), field.m_pszName);
        const auto hashed_name   = MurmurHash2(key_buffer, MURMURHASH_SEED_MODSHARP);
        g_SchemaMap[hashed_name] = {.offset = field_offset, .networked = is_field_networked, .valid = true};

        added_field_names.insert(field_name);
    }

    auto base_class_count = current_class_info->GetBaseClassSize();
    auto base_classes     = current_class_info->GetBaseClasses();
    for (auto i = 0; i < base_class_count; i++)
    {
        auto& base_class = base_classes[i];
        BuildClassSchemaRecursive(derived_schema_class, base_class.m_pClass, added_field_names, override_fields);
    }
}

static void ScanSchemaScopeType(CSchemaSystemTypeScope* type_scope)
{
#ifdef PLATFORM_WINDOWS
    static constexpr int32_t class_bindings_offset = 0x500;
#else
    static constexpr int32_t class_bindings_offset = 0x560;
#endif

    const auto class_bindings = reinterpret_cast<CUtlTSHash<SchemaClassInfoData_t*, 256, unsigned int>*>(
        reinterpret_cast<std::uintptr_t>(type_scope) + class_bindings_offset);

    if (class_bindings->Count() == 0)
    {
        return;
    }

    std::vector<UtlTSHashHandle_t> handles(class_bindings->Count());
    if (class_bindings->GetElements(0, class_bindings->Count(), handles.data()) == 0)
    {
        return;
    }

    for (const UtlTSHashHandle_t handle : handles)
    {
        SchemaClassInfoData_t* class_info = class_bindings->Element(handle);
        if (!class_info)
        {
            continue;
        }

        auto* schema_class = new SchemaClass_t();
        schema_class->name = class_info->GetName();
        schema_class->baseClassList.AddToTail(new CUtlString(class_info->GetName()));
        g_SchemaList.AddToTail(schema_class);

        std::unordered_set<std::string_view>         added_field_names{};
        std::unordered_map<std::string, const char*> override_fields{};

        BuildClassSchemaRecursive(schema_class, class_info, added_field_names, override_fields);
    }
}

void InitSchemaSystem()
{
    const auto pType = schemaSystem->FindTypeScopeForModule(LIB_FILE_PREFIX "server" LIB_FILE_EXTENSION);
    if (!pType)
    {
        FatalError("Failed to find type in schemaSystem!\n");
        return;
    }

    ScanSchemaScopeType(pType);
    ScanSchemaScopeType(schemaSystem->GetGlobalTypeScope());
}

static CUtlVector<SchemaClass_t*>* SchemaGetSchemas()
{
    return &g_SchemaList;
}

namespace natives::schemasystem
{
void Init()
{
    bridge::CreateNative("Schema.GetSchemas", reinterpret_cast<void*>(SchemaGetSchemas));
}
} // namespace natives::schemasystem
