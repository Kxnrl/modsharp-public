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
#include "cstrike/type/Variant.h"

#include <cstdint>
#include <unordered_map>
#include <unordered_set>

static constexpr const char* FieldTypeToString(FieldType_t type)
{
    constexpr const char* names[] = {
        "void",                          // FIELD_VOID
        "float32",                       // FIELD_FLOAT32
        "const char*",                   // FIELD_STRING
        "Vector",                        // FIELD_VECTOR
        "Quaternion",                    // FIELD_QUATERNION
        "int32",                         // FIELD_INT32
        "bool",                          // FIELD_BOOLEAN
        "int16",                         // FIELD_INT16
        "char",                          // FIELD_CHARACTER
        "Color",                         // FIELD_COLOR32
        "<embedded>",                    // FIELD_EMBEDDED
        "<custom>",                      // FIELD_CUSTOM
        "<classptr>",                    // FIELD_CLASSPTR
        "CHandle",                       // FIELD_EHANDLE
        "Vector",                        // FIELD_POSITION_VECTOR
        "float32",                       // FIELD_TIME
        "int32",                         // FIELD_TICK
        "soundname",                     // FIELD_SOUNDNAME
        "<input>",                       // FIELD_INPUT
        "<function>",                    // FIELD_FUNCTION
        "VMatrix",                       // FIELD_VMATRIX
        "VMatrix",                       // FIELD_VMATRIX_WORLDSPACE
        "matrix3x4_t",                   // FIELD_MATRIX3X4_WORLDSPACE
        "interval",                      // FIELD_INTERVAL
        "<unused>",                      // FIELD_UNUSED
        "Vector2d",                      // FIELD_VECTOR2D
        "int64",                         // FIELD_INT64
        "Vector4D",                      // FIELD_VECTOR4D
        "<resource>",                    // FIELD_RESOURCE
        "unknown",                       // FIELD_TYPEUNKNOWN
        "const char*",                   // FIELD_CSTRING
        "HSCRIPT",                       // FIELD_HSCRIPT
        "CVariant",                      // FIELD_VARIANT
        "uint64",                        // FIELD_UINT64
        "float64",                       // FIELD_FLOAT64
        "positive_or_null",              // FIELD_POSITIVEINTEGER_OR_NULL
        "HSCRIPT-new",                   // FIELD_HSCRIPT_NEW_INSTANCE
        "uint32",                        // FIELD_UINT32
        "CUtlStringToken",               // FIELD_UTLSTRINGTOKEN
        "QAngle",                        // FIELD_QANGLE
        "Vector",                        // FIELD_NETWORK_ORIGIN_CELL_QUANTIZED_VECTOR
        "HMaterial",                     // FIELD_HMATERIAL
        "HModel",                        // FIELD_HMODEL
        "Vector",                        // FIELD_NETWORK_QUANTIZED_VECTOR
        "float32",                       // FIELD_NETWORK_QUANTIZED_FLOAT
        "Vector",                        // FIELD_DIRECTION_VECTOR_WORLDSPACE
        "QAngle",                        // FIELD_QANGLE_WORLDSPACE
        "Quaternion",                    // FIELD_QUATERNION_WORLDSPACE
        "HSCRIPT-light",                 // FIELD_HSCRIPT_LIGHTBINDING
        "v8value",                       // FIELD_V8_VALUE
        "v8object",                      // FIELD_V8_OBJECT
        "v8array",                       // FIELD_V8_ARRAY
        "v8callback",                    // FIELD_V8_CALLBACK_INFO
        "CUtlString",                    // FIELD_UTLSTRING
        "Vector",                        // FIELD_NETWORK_ORIGIN_CELL_QUANTIZED_POSITION_VECTOR
        "HRenderTexture",                // FIELD_HRENDERTEXTURE
        "HParticleSystemDefinition",     // FIELD_HPARTICLESYSTEMDEFINITION
        "uint8",                         // FIELD_UINT8
        "uint16",                        // FIELD_UINT16
        "CTransform",                    // FIELD_CTRANSFORM
        "CTransform",                    // FIELD_CTRANSFORM_WORLDSPACE
        "HPostProcessing",               // FIELD_HPOSTPROCESSING
        "matrix3x4_t",                   // FIELD_MATRIX3X4
        "<shim>",                        // FIELD_SHIM
        "CMotionTransform",              // FIELD_CMOTIONTRANSFORM
        "CMotionTransform",              // FIELD_CMOTIONTRANSFORM_WORLDSPACE
        "AttachmentHandle_t",            // FIELD_ATTACHMENT_HANDLE
        "int8",                          // FIELD_AMMO_INDEX
        "ConditionId_t",                 // FIELD_CONDITION_ID
        "CAI_ScheduleBits",              // FIELD_AI_SCHEDULE_BITS
        "CModifierHandleTyped",          // FIELD_MODIFIER_HANDLE
        "RotationVector",                // FIELD_ROTATION_VECTOR
        "RotationVector",                // FIELD_ROTATION_VECTOR_WORLDSPACE
        "HVDataResource",                // FIELD_HVDATA
        "scale32",                       // FIELD_SCALE32
        "CUtlStringAndTokenWithStorage", // FIELD_STRING_AND_TOKEN
        "float64",                       // FIELD_ENGINE_TIME
        "int32",                         // FIELD_ENGINE_TICK
        "uint32",                        // FIELD_WORLD_GROUP_ID
        "uint64",                        // FIELD_GLOBALSYMBOL
        "HNmGraphDefinition",            // FIELD_HNMGRAPHDEFINITION
    };
    if (type < FieldType_t::FIELD_TYPECOUNT)
        return names[static_cast<uint8_t>(type)];
    return "UNKNOWN";
}

// for coreclr
struct SchemaClassField_t
{
    CUtlString           name;
    CUtlString           type;
    int32_t              offset;
    bool                 networked;
    SchemaTypeCategory_t category;
};

struct DataMapField_t
{
    CUtlString name;
    void*      inputFunc;
};
struct SchemaClass_t
{
    CUtlString                     name;
    int32_t                        chain;
    int32_t                        size;
    int8_t                         align;
    CUtlVector<SchemaClassField_t> fields;
    CUtlVector<CUtlString*>        baseClassList;
    CUtlVector<DataMapField_t>     dataMapFields;
};

using SchemaKeyValueMap_t = std::unordered_map<uint64_t, SchemaKey>;
using SchemaTableMap_t    = std::unordered_map<uint64_t, SchemaKeyValueMap_t>;

static CUtlVector<SchemaClass_t*>          g_SchemaList;
static SchemaKeyValueMap_t                 g_SchemaMap{};
static std::unordered_map<uint64_t, void*> g_DataMapInputFuncMap{};

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

void* schemas::FindDataMapInputFunc(const char* className, const char* fieldName)
{
    char key_buffer[512];
    snprintf(key_buffer, sizeof(key_buffer), "%s->%s", className, fieldName);

    if (const auto it = g_DataMapInputFuncMap.find(MurmurHash2(key_buffer, MURMURHASH_SEED_MODSHARP)); it != g_DataMapInputFuncMap.end())
    {
        return it->second;
    }

    return nullptr;
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

static void ProcessDataMapFields(SchemaClass_t*                        derived_schema_class,
                                 const SchemaClassInfoData_t*          current_class_info,
                                 std::unordered_set<std::string_view>& added_field_names)
{
    const auto dataMap = current_class_info->GetDataMap();
    if (dataMap == nullptr)
        return;

    for (auto i = 0; i < dataMap->dataNumFields; i++)
    {
        const auto& dataMap_field = dataMap->dataDesc[i];

        const auto* field_name = dataMap_field.fieldName;
        if (field_name == nullptr)
            continue;

        if (dataMap_field.inputFunc != nullptr)
        {
            auto new_dm_field  = derived_schema_class->dataMapFields.AddToTailGetPtr();
            new_dm_field->name = field_name;
            memcpy(&new_dm_field->inputFunc, &dataMap_field.inputFunc, sizeof(void*));

            char key_buffer[512];
            snprintf(key_buffer, sizeof(key_buffer), "%s->%s", derived_schema_class->name.Get(), field_name);
            g_DataMapInputFuncMap[MurmurHash2(key_buffer, MURMURHASH_SEED_MODSHARP)] = new_dm_field->inputFunc;

            continue;
        }

        constexpr int32_t invalid_offset = 0x7fffffff;
        const auto        offset         = dataMap_field.fieldOffset;

        if (offset == invalid_offset || dataMap_field.fieldType == FieldType_t::FIELD_EMBEDDED)
            continue;

        if (added_field_names.contains(field_name))
            continue;

        auto* new_field      = derived_schema_class->fields.AddToTailGetPtr();
        new_field->name      = field_name;
        new_field->type      = FieldTypeToString(dataMap_field.fieldType);
        new_field->offset    = offset;
        new_field->networked = false;
        new_field->category  = SCHEMA_TYPE_BUILTIN;

        char key_buffer[512];
        snprintf(key_buffer, sizeof(key_buffer), "%s->%s", derived_schema_class->name.Get(), field_name);
        const auto hashed_name = MurmurHash2(key_buffer, MURMURHASH_SEED_MODSHARP);

        g_SchemaMap[hashed_name] = {.offset = offset, .networked = false, .valid = true};

        added_field_names.insert(field_name);
    }
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

    ProcessDataMapFields(derived_schema_class, current_class_info, added_field_names);

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
    static constexpr int32_t class_bindings_offset = 0x560;

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
