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

#include "gamedata.h"
#include "global.h"
#include "logging.h"
#include "module.h"
#include "scopetimer.h"
#include "types.h"

#include "cstrike/interface/IGameSystem.h"

#include <Zydis.h>

#include <array>

class CBaseGameSystemFactory;

#define RESOLVE_GAMEDATA_ADDRESS(name, variable) \
    (variable) = g_pGameData->GetAddress<decltype(variable)>(name)

#ifdef PLATFORM_WINDOWS
#    define RELATE_SERVER_LIB_FILE_PATH "../../csgo/bin/win64/"
#else
#    define RELATE_SERVER_LIB_FILE_PATH "../../csgo/bin/linuxsteamrt64/"
#endif

GameData*   g_pGameData;
extern void InitializeInterfaces();

CBaseGameSystemFactory** CBaseGameSystemFactory::sm_ppFirst = nullptr;

static void FindCEntityIdentity_SetEntityName()
{
    const auto set_entity_name_functions = modules::server->FindAllFunctionsFromStringRefs({"CEntityIdentity::SetEntityName called, but there is no entity name string table pointer!\n"});
    if (set_entity_name_functions.empty()) [[unlikely]]
    {
        FatalError("Failed to find CEntityIdentity::SetEntityName");
        return;
    }

    const auto point_script_set_entity_name = modules::server->FindFunctionFromStringRefs({"SetEntityName",
                                                                                           "(name: string)"});
    if (!point_script_set_entity_name.IsValid())
    {
        FatalError("Failed to find CPointScript::SetEntityName");
        return;
    }

    const auto range = modules::server->GetFunctionRange(point_script_set_entity_name);
    if (!range)
    {
        FatalError("Failed to get function range for CPointScript::SetEntityName");
        return;
    }

    ZydisDecoder decoder{};
    if (ZYAN_FAILED(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
    {
        FatalError("Failed to initialize Zydis decoder");
        return;
    }

    ZydisDecodedInstruction instr;

    for (auto ip = range->start; ip < range->end;)
    {
        if (ZYAN_FAILED(ZydisDecoderDecodeInstruction(&decoder, nullptr, reinterpret_cast<const void*>(ip), range->end - ip, &instr)))
        {
            ip++;
            continue;
        }

        if (instr.opcode == 0xE8)
        {
            auto target = ip + instr.length + static_cast<std::int32_t>(instr.raw.imm[0].value.s);
            for (const auto& func : set_entity_name_functions)
            {
                if (target == func)
                {
                    FLOG("Found CEntityIdentity::SetEntityName at server+0x%llx", func - modules::server->Base());
                    address::server::CEntityIdentity_SetEntityName = reinterpret_cast<address::server::CEntityIdentity_SetEntityName_t>(func);
                    return;
                }
            }
        }

        ip += instr.length;
    }

    FatalError("Failed to find CEntityIdentity::SetEntityName call in CPointScript::SetEntityName");
}

static void FindGameSystemFactory()
{
    const auto function_address = modules::server->FindFunctionFromStringRef("Game System %s is defined twice!\n");
    if (!function_address.IsValid()) [[unlikely]]
    {
        FatalError("Failed to find IGameSystem::InitAllSystems");
        return;
    }

    ZydisDecoder decoder;
    if (!ZYAN_SUCCESS(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64))) [[unlikely]]
    {
        FatalError("Failed to initialize Zydis decoder.");
        return;
    }

    ZydisDecodedInstruction instr;
    ZydisDecodedOperand     operands[ZYDIS_MAX_OPERAND_COUNT];

    std::uintptr_t ip = function_address;

    std::uintptr_t pending_addr = 0;
    ZydisRegister  pending_reg  = ZYDIS_REGISTER_NONE;

    for (auto i = 0; i < 50; ++i)
    {
        if (!ZYAN_SUCCESS(ZydisDecoderDecodeFull(&decoder,
            reinterpret_cast<const void*>(ip),
            ZYDIS_MAX_INSTRUCTION_LENGTH,
            &instr,
            operands))) [[unlikely]]
            break;

        // mov reg, cs:CBaseGameSystemFactory::sm_pFirst
        if (instr.mnemonic == ZYDIS_MNEMONIC_MOV && (instr.attributes & ZYDIS_ATTRIB_IS_RELATIVE) && instr.operand_count_visible == 2)
        {
            const auto& dst = operands[0];
            const auto& src = operands[1];

            if (dst.type == ZYDIS_OPERAND_TYPE_REGISTER && src.type == ZYDIS_OPERAND_TYPE_MEMORY)
            {
                pending_reg  = dst.reg.value;
                pending_addr = ip + instr.length + src.mem.disp.value;
            }
        }
        // test reg, reg
        else if (pending_reg != ZYDIS_REGISTER_NONE && instr.mnemonic == ZYDIS_MNEMONIC_TEST && instr.operand_count_visible == 2)
        {
            const auto& op1 = operands[0];
            const auto& op2 = operands[1];

            if (op1.type == ZYDIS_OPERAND_TYPE_REGISTER && op1.reg.value == pending_reg && op2.type == ZYDIS_OPERAND_TYPE_REGISTER && op2.reg.value == pending_reg)
            {
                auto temp  = reinterpret_cast<CBaseGameSystemFactory**>(pending_addr);
                auto first = *temp;
                if (first == nullptr)
                {
                    WARN("Candidate at server+0x%llx rejected: factory pointer is null", pending_addr - modules::server->Base());
                    pending_reg  = ZYDIS_REGISTER_NONE;
                    pending_addr = 0;
                    continue;
                }

                if (!modules::server->IsPointerDerivedFrom(first->m_pInstance, "IGameSystem"))
                {
                    WARN("Candidate at server+0x%llx rejected: m_pInstance is not derived from IGameSystem", pending_addr - modules::server->Base());
                    pending_reg  = ZYDIS_REGISTER_NONE;
                    pending_addr = 0;
                    continue;
                }

                FLOG("Found CBaseGameSystemFactory::sm_ppFirst at sever+0x%llx", pending_addr - modules::server->Base());
                CBaseGameSystemFactory::sm_ppFirst = temp;
                return;
            }

            pending_reg = ZYDIS_REGISTER_NONE;
        }
        else if (pending_reg != ZYDIS_REGISTER_NONE && instr.operand_count_visible > 0)
        {
            if (operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER && operands[0].reg.value == pending_reg && (operands[0].actions & ZYDIS_OPERAND_ACTION_WRITE))
            {
                pending_reg  = ZYDIS_REGISTER_NONE;
                pending_addr = 0;
            }
        }

        ip += instr.length;
    }

    FatalError("Found IGameSystem::InitAllSystems but failed to find instruction sequence within limit(50 times)");
}

bool address::Initialize()
{
    modules::tier0           = new CModule(LIB_FILE_PREFIX "tier0");
    modules::engine          = new CModule(LIB_FILE_PREFIX "engine2");
    modules::server          = new CModule(RELATE_SERVER_LIB_FILE_PATH LIB_FILE_PREFIX "server");
    modules::schemas         = new CModule(LIB_FILE_PREFIX "schemasystem");
    modules::resource        = new CModule(LIB_FILE_PREFIX "resourcesystem");
    modules::vscript         = new CModule(LIB_FILE_PREFIX "vscript");
    modules::vphysics2       = new CModule(LIB_FILE_PREFIX "vphysics2");
    modules::sound           = new CModule(LIB_FILE_PREFIX "soundsystem");
    modules::network         = new CModule(LIB_FILE_PREFIX "networksystem");
    modules::worldrenderer   = new CModule(LIB_FILE_PREFIX "worldrenderer");
    modules::matchmaking     = new CModule(LIB_FILE_PREFIX "matchmaking");
    modules::filesystem      = new CModule(LIB_FILE_PREFIX "filesystem_stdio");
    modules::steamsockets    = new CModule(LIB_FILE_PREFIX "steamnetworkingsockets");
    modules::materialsystem2 = new CModule(LIB_FILE_PREFIX "materialsystem2");
    modules::animationsystem = new CModule(LIB_FILE_PREFIX "animationsystem");

    InitializeInterfaces();

    g_pGameData = new GameData();

    constexpr std::array gamedata_files = {
        "core.games.jsonc",
        "tier0.games.jsonc",
        "engine.games.jsonc",
        "server.games.jsonc",
        "log.games.jsonc",
    };

    bool all_succeeded = true;

    {
        ScopedTimer timer("Register gamedata");
        for (const auto& path : gamedata_files)
        {
            if (!g_pGameData->Register(path))
            {
                all_succeeded = false;
            }
        }
    }

    if (!all_succeeded)
    {
        FatalError("Failed to load one or more gamedata files. See log for details.");
    }

    FindGameSystemFactory();
    FindCEntityIdentity_SetEntityName();

    RESOLVE_GAMEDATA_ADDRESS("Source2_Init", address::engine::Source2_Init);

    // cvar
    RESOLVE_GAMEDATA_ADDRESS("ScriptSetConVarString", address::server::ScriptSetConVarString);
    RESOLVE_GAMEDATA_ADDRESS("ScriptSetConVarNumber", address::server::ScriptSetConVarNumber);
    RESOLVE_GAMEDATA_ADDRESS("ScriptSetConVarDouble", address::server::ScriptSetConVarDouble);

    RESOLVE_GAMEDATA_ADDRESS("NetworkStateChanged", address::server::NetworkStateChanged);
    RESOLVE_GAMEDATA_ADDRESS("StateChanged", address::server::StateChanged);

    // CBaseEntity
    RESOLVE_GAMEDATA_ADDRESS("CreateEntityByName", address::server::CreateEntityByName);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::AbsOrigin", address::server::CBaseEntity_AbsOrigin);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::SetAbsOrigin", address::server::CBaseEntity_SetAbsOrigin);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::AbsAngles", address::server::CBaseEntity_AbsAngles);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::SetAbsAngles", address::server::CBaseEntity_SetAbsAngles);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::LocalVelocity", address::server::CBaseEntity_LocalVelocity);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::AbsVelocity", address::server::CBaseEntity_AbsVelocity);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::SetAbsVelocity", address::server::CBaseEntity_SetAbsVelocity);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::AcceptInput", address::server::CBaseEntity_AcceptInput);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::SetMoveType", address::server::CBaseEntity_SetMoveType);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::SetGravityScale", address::server::CBaseEntity_SetGravityScale);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::ApplyAbsVelocityImpulse", address::server::CBaseEntity_ApplyAbsVelocityImpulse);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::DispatchSpawn", address::server::CBaseEntity_DispatchSpawn);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::SetGroundEntity", address::server::CBaseEntity_SetGroundEntity);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::EmitSoundFilter", address::server::CBaseEntity_EmitSoundFilter);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::StopSound", address::server::CBaseEntity_StopSound);
    RESOLVE_GAMEDATA_ADDRESS("CBaseEntity::DispatchTraceAttack", address::server::CBaseEntity_DispatchTraceAttack);

    // PlayerController
    RESOLVE_GAMEDATA_ADDRESS("CBasePlayerController::SwitchSteam", address::server::CBasePlayerController_SwitchSteam);
    RESOLVE_GAMEDATA_ADDRESS("CBasePlayerController::SetPawn", address::server::CBasePlayerController_SetPawn);
    RESOLVE_GAMEDATA_ADDRESS("CBasePlayerController::CheckPawn", address::server::CBasePlayerController_CheckPawn);

    // services
    RESOLVE_GAMEDATA_ADDRESS("CCSPlayer_ItemServices::GiveGlove", address::server::ServiceGiveGlove);
    RESOLVE_GAMEDATA_ADDRESS("CCSPlayer_ItemServices::GiveNamedItem", address::server::PlayerPawnItemServices_GiveNamedItem);
    RESOLVE_GAMEDATA_ADDRESS("CCSPlayer_ItemServices::RemovePlayerItem", address::server::PlayerPawnWeaponServices_RemovePlayerItem);
    RESOLVE_GAMEDATA_ADDRESS("CCSPlayer_WeaponServices::GetWeaponBySlot", address::server::PlayerPawnWeaponServices_GetWeaponBySlot);
    RESOLVE_GAMEDATA_ADDRESS("CCSPlayer_WeaponServices::DetachWeapon", address::server::PlayerPawnWeaponServices_DetachWeapon);

    // entity list
    RESOLVE_GAMEDATA_ADDRESS("CGameEntitySystem::FindEntityByIndex", address::server::CGameEntitySystem_FindEntityByIndex);
    RESOLVE_GAMEDATA_ADDRESS("CGameEntitySystem::FindByClassname", address::server::CGameEntitySystem_FindByClassname);
    RESOLVE_GAMEDATA_ADDRESS("CGameEntitySystem::FindByName", address::server::CGameEntitySystem_FindByName);
    RESOLVE_GAMEDATA_ADDRESS("CGameEntitySystem::FindInSphere", address::server::CGameEntitySystem_FindInSphere);
    RESOLVE_GAMEDATA_ADDRESS("CGameEntitySystem::AddListenerEntity", address::server::CGameEntitySystem_AddListenerEntity);
    RESOLVE_GAMEDATA_ADDRESS("CGameEntitySystem::RemoveListenerEntity", address::server::CGameEntitySystem_RemoveListenerEntity);
    RESOLVE_GAMEDATA_ADDRESS("CGameEntitySystem::AllocPooledString", address::server::CGameEntitySystem_AllocPooledString);
    RESOLVE_GAMEDATA_ADDRESS("CGameEntitySystem::AddEntityIOEvent", address::server::CGameEntitySystem_AddEntityIOEvent);

    // PlayerPawn
    RESOLVE_GAMEDATA_ADDRESS("CBasePlayerPawn::FindMatchingWeaponsForTeamLoadout", address::server::CBasePlayerPawn_FindMatchingWeaponsForTeamLoadout);

    // GameRules
    RESOLVE_GAMEDATA_ADDRESS("CCSGameRules::PlayerCanHearChat", address::server::CCSGameRules_PlayerCanHearChat);
    RESOLVE_GAMEDATA_ADDRESS("CCSGameRules::TerminateRound", address::server::CCSGameRules_TerminateRound);

    // Utils
    RESOLVE_GAMEDATA_ADDRESS("UTIL_SetModel", address::server::UTIL_SetModel);
    RESOLVE_GAMEDATA_ADDRESS("UTIL_GetEconItemSchema", address::server::GetEconItemSchema);
    RESOLVE_GAMEDATA_ADDRESS("UTIL_RadioMessage", address::server::UTIL_RadioMessage);
    RESOLVE_GAMEDATA_ADDRESS("UTIL_DispatchEffect", address::server::UTIL_DispatchEffect);
    RESOLVE_GAMEDATA_ADDRESS("UTIL_DispatchEffectFilter", address::server::UTIL_DispatchEffectFilter);
    RESOLVE_GAMEDATA_ADDRESS("UTIL_DispatchParticleEffectFilter_Position", address::server::UTIL_DispatchParticleEffectFilterPosition);
    RESOLVE_GAMEDATA_ADDRESS("UTIL_DispatchParticleEffectFilter_Attachment", address::server::UTIL_DispatchParticleEffectFilterAttachment);

    // Misc
    RESOLVE_GAMEDATA_ADDRESS("CAttributeList::SetOrAddAttributeValueByName", address::server::CAttributeList_SetOrAddAttributeValueByName);

    // CEntityInstance
    RESOLVE_GAMEDATA_ADDRESS("CEntityInstance::GetEntityIndex", address::server::CEntityInstance_GetEntityIndex);
    RESOLVE_GAMEDATA_ADDRESS("CEntityInstance::Kill", address::server::CEntityInstance_Kill);
    RESOLVE_GAMEDATA_ADDRESS("CEntityInstance::GetRefEHandle", address::server::CEntityInstance_GetRefEHandle);
    RESOLVE_GAMEDATA_ADDRESS("CEntityInstance::GetOrCreatePublicScriptScope", address::server::CEntityInstance_GetOrCreatePublicScriptScope);
    RESOLVE_GAMEDATA_ADDRESS("CEntityInstance::GetOrCreatePrivateScriptScope", address::server::CEntityInstance_GetOrCreatePrivateScriptScope);

    // CBaseModelEntity
    RESOLVE_GAMEDATA_ADDRESS("CBaseModelEntity::SetBodyGroupByName", address::server::CBaseModelEntity_SetBodyGroupByName);
    RESOLVE_GAMEDATA_ADDRESS("CBaseModelEntity::SetMaterialGroupMask", address::server::CBaseModelEntity_SetMaterialGroupMask);
    RESOLVE_GAMEDATA_ADDRESS("CBaseModelEntity::LookupBone", address::server::CBaseModelEntity_LookupBone);
    RESOLVE_GAMEDATA_ADDRESS("CBaseModelEntity::GetBoneTransform", address::server::CBaseModelEntity_GetBoneTransform);
    RESOLVE_GAMEDATA_ADDRESS("CBaseModelEntity::SetModelScale", address::server::CBaseModelEntity_SetModelScale);
    RESOLVE_GAMEDATA_ADDRESS("CBaseModelEntity::SetCollisionBounds", address::server::CBaseModelEntity_SetCollisionBounds);

    // CPaintKit
    // RESOLVE_GAMEDATA_ADDRESS("CPaintKit::UsesLegacyModel", address::server::CPaintKit_UsesLegacyModel);

    // CGamePhysicsQueryInterface
    RESOLVE_GAMEDATA_ADDRESS("CGamePhysicsQueryInterface::TraceShape", address::server::CGamePhysicsQueryInterface_TraceShape);

    // Studio Model
    RESOLVE_GAMEDATA_ADDRESS("StudioModel::LookupAttachment", address::server::StudioModel_LookupAttachment);
    RESOLVE_GAMEDATA_ADDRESS("StudioModel::GetAttachment", address::server::StudioModel_GetAttachment);

    // Sound OP
    RESOLVE_GAMEDATA_ADDRESS("SoundOpGameSystem::SetSoundEventParamString", address::server::SoundOpGameSystem_SetSoundEventParamString);
    RESOLVE_GAMEDATA_ADDRESS("SoundOpGameSystem::StopSoundEvent", address::server::SoundOpGameSystem_StopSoundEvent);
    RESOLVE_GAMEDATA_ADDRESS("SoundOpGameSystem::StopSoundEventFilter", address::server::SoundOpGameSystem_StopSoundEventFilter);

    // Movement service
    RESOLVE_GAMEDATA_ADDRESS("CCSPlayer_MovementServices::TracePlayerBBox", address::server::CCSPlayer_MovementServices_TracePlayerBBox);

    // Tracefilter
    // RESOLVE_GAMEDATA_ADDRESS("CTraceFilterPlayerMovementCS::ctor", address::server::CTraceFilterPlayerMovementCS_ctor);

    // CCollisionProperty
    RESOLVE_GAMEDATA_ADDRESS("CCollisionProperty::SetSolid", address::server::CCollisionProperty_SetSolid);

    RESOLVE_GAMEDATA_ADDRESS("FindWeaponVDataByName", address::server::FindWeaponVDataByName);
    RESOLVE_GAMEDATA_ADDRESS("GetLegacyGameEventListener", address::server::GetLegacyGameEventListener);
    RESOLVE_GAMEDATA_ADDRESS("CGameEntitySystem::GetSpawnOriginOffset", address::server::CGameEntitySystem_GetSpawnOriginOffset);

    // ResourceSystem
    RESOLVE_GAMEDATA_ADDRESS("CResourceNameTyped::ResolveResourceName", address::resource::CResourceNameTyped_ResolveResourceName);
    return true;
}
