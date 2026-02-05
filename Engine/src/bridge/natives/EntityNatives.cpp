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

#include "bridge/natives/EntityNatives.h"
#include "bridge/adapter.h"
#include "bridge/forwards/forward.h"

#include "address.h"
#include "global.h"
#include "logging.h"
#include "strtool.h"

#include "CoreCLR/Nullable.h"
#include "CoreCLR/RuntimeRecipientFilter.h"

#include "cstrike/component/CBodyComponent.h"
#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/type/CRecipientFilter.h"
#include "cstrike/type/CUtlVector.h"
#include "cstrike/type/EmitSound.h"
#include "cstrike/type/EntityIO.h"
#include "cstrike/type/Variant.h"

#include <algorithm>
#include <unordered_map>

namespace natives::entity
{

// <classname, <outputName>>
static std::unordered_map<std::string, std::vector<std::string>> s_hookedOutput;
// <classname, <inputName>>
static std::unordered_map<std::string, std::vector<std::string>> s_hookedInput;

static CBaseEntity* FindEntityByEHandle(EHandle_t value)
{
    const CBaseHandle EHandle(value);
    const auto        pEntity = g_pGameEntitySystem->FindEntityByEHandle(EHandle);
    return pEntity;
}

static CBaseEntity* FindEntityByIndex(EntityIndex_t index)
{
    return g_pGameEntitySystem->FindEntityByIndex(index);
}

static CBaseEntity* FindEntityByClassname(CBaseEntity* pStart, const char* classname)
{
    return g_pGameEntitySystem->FindByClassname(pStart, classname);
}

static CBaseEntity* FindEntityByName(CBaseEntity* pStart, const char* name)
{
    return g_pGameEntitySystem->FindByName(pStart, name);
}

static CBaseEntity* FindEntityInSphere(CBaseEntity* pStart, Vector* vCenter, float radius)
{
    return g_pGameEntitySystem->FindInSphere(pStart, vCenter, radius);
}

static CBaseEntity* SpawnEntitySync(const char* classname, KeyValuesVariantItem* items, int count)
{
    return g_pGameEntitySystem->SpawnEntityFromKeyValuesSync(classname, items, count);
}

static CBaseEntity* CreateEntityByName(const char* classname)
{
    return g_pGameEntitySystem->CreateEntityByName(classname);
}

static const char* AllocPooledString(const char* content)
{
    return g_pGameEntitySystem->AllocPooledString(content).Get();
}

static void HookEntityOutput(const char* classname, const char* pszOutput)
{
    auto output = LowercaseString(pszOutput);

    // NOTE
    // 旧版中我们使用 m_ 开头以匹配 SchemaSystem 中的字段
    // 当前我们直接匹配EntityIO的名称, 所以不在需要 m_

    const auto hookedEntity = s_hookedOutput.find(classname);
    if (hookedEntity == s_hookedOutput.end())
    {
        s_hookedOutput[classname] = {output};
        return;
    }

    hookedEntity->second.emplace_back(output);
}

static void HookEntityInput(const char* classname, const char* pszInput)
{
    auto input = LowercaseString(pszInput);

    const auto hookedEntity = s_hookedInput.find(classname);
    if (hookedEntity == s_hookedInput.end())
    {
        s_hookedInput[classname] = {input};
        return;
    }

    hookedEntity->second.emplace_back(input);
}

///////////////////////////////////////////////////////////////////////////////////////

static void EntityKill(CBaseEntity* pEntity)
{
    pEntity->Kill();
}

static void EntityChangeTeam(CBaseEntity* pEntity, CStrikeTeam_t team)
{
    if (pEntity->IsPlayerController())
        reinterpret_cast<CCSPlayerController*>(pEntity)->ForceChangeTeam(team);
    else
        pEntity->ChangeTeam(team);
}

static bool EntityIsValid(EHandle_t value)
{
    return FindEntityByEHandle(value) != nullptr;
}

static void EntityCollisionRulesChanged(CBaseEntity* pEntity)
{
    pEntity->CollisionRulesChanged();
}

static int64_t EntityEmitSound(CBaseEntity* pEntity, const char* sound, Nullable<float>* volume, RuntimeRecipientFilter* pFilter)
{
    CBroadcastRecipientFilter filter(pFilter, true);
    const auto                pVolume = volume->HasValue() ? &volume->GetValue() : nullptr;
    return pEntity->EmitSound(sound, pVolume, &filter);
}

static void EntityStopSound(CBaseEntity* pEntity, const char* sound)
{
    pEntity->StopSound(sound);
}

static int64_t EntityDispatchTraceAttack(CBaseEntity* pEntity, const CTakeDamageInfo* pInfo, bool bypassHook)
{
    CBaseEntity::SetBypassHook(bypassHook);
    const auto result = pEntity->DispatchTraceAttack(pInfo);
    CBaseEntity::SetBypassHook(false);
    return result;
}

static void EntityNetworkStateChanged(CBaseEntity* pEntity, uint16_t offset)
{
    NetworkStateChanged(reinterpret_cast<uintptr_t>(pEntity), offset, 0xFFFFFFFF);
}

static void EntitySetStateChanged(CBaseEntity* pEntity, uint16_t offset)
{
    SetStateChanged(pEntity, offset);
}

static void EntitySetStructStateChanged(void* pEntity, uint16_t offset)
{
    SetStructStateChanged(pEntity, offset);
}

static bool EntityAcceptInput(CBaseEntity* pEntity, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const char* pValue, int outputId)
{
    AssertPtr(pInput);

    if (pInput[0] == 0)
        return false;

    Variant_t variant;
    if (pValue != nullptr)
        variant.SetString(pValue);
    return pEntity->AcceptInput(pInput, pActivator, pCaller, variant, outputId);
}

static bool EntityAcceptInputInt(CBaseEntity* pEntity, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, int pValue, int outputId)
{
    AssertPtr(pInput);

    if (pInput[0] == 0)
        return false;

    Variant_t variant;
    variant.SetInt(pValue);
    return pEntity->AcceptInput(pInput, pActivator, pCaller, variant, outputId);
}

static bool EntityAcceptInputFloat(CBaseEntity* pEntity, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, float pValue, int outputId)
{
    AssertPtr(pInput);

    if (pInput[0] == 0)
        return false;

    Variant_t variant;
    variant.SetFloat(pValue);
    return pEntity->AcceptInput(pInput, pActivator, pCaller, variant, outputId);
}

static void EntityAddIOEvent(CBaseEntity* pEntity, float flDelay, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const char* pValue, int outputId)
{
    AssertPtr(pInput);

    if (pInput[0] == 0)
        return;

    Variant_t variant;
    if (pValue != nullptr)
        variant.SetString(pValue);

    g_pGameEntitySystem->AddEntityIOEvent(pEntity, pInput, pActivator, pCaller, &variant, flDelay, outputId);
}

static void EntityAddIOEventInt(CBaseEntity* pEntity, float flDelay, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, int pValue, int outputId)
{
    AssertPtr(pInput);

    if (pInput[0] == 0)
        return;

    Variant_t variant;
    variant.SetInt(pValue);

    g_pGameEntitySystem->AddEntityIOEvent(pEntity, pInput, pActivator, pCaller, &variant, flDelay, outputId);
}

static void EntityAddIOEventFloat(CBaseEntity* pEntity, float flDelay, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, float pValue, int outputId)
{
    AssertPtr(pInput);

    if (pInput[0] == 0)
        return;

    Variant_t variant;
    variant.SetFloat(pValue);

    g_pGameEntitySystem->AddEntityIOEvent(pEntity, pInput, pActivator, pCaller, &variant, flDelay, outputId);
}

static void EntitySetName(CBaseEntity* pEntity, const char* pszName)
{
    pEntity->SetName(pszName);
}

static void EntitySetModel(CBaseEntity* pEntity, const char* pModel)
{
    pEntity->SetModel(pModel);
}

static void EntitySetOwner(CBaseEntity* pEntity, CBaseEntity* pOwner)
{
    pEntity->SetOwner(pOwner);
}

static void EntitySetGroundEntity(CBaseEntity* pEntity, CBaseEntity* pGround, void* pPhysicsBody)
{
    pEntity->SetGroundEntity(pGround, pPhysicsBody);
}

static void EntitySetSolid(CBaseEntity* pEntity, SolidType_t solid)
{
    if (const auto pCollision = pEntity->m_pCollision())
        pCollision->SetSolid(solid);
}

static void EntityTeleport(CBaseEntity* pEntity, Nullable<Vector>* vPosition, Nullable<QAngle>* vAngles, Nullable<Vector>* vVelocity)
{
    const auto pos = vPosition->HasValue() ? &vPosition->GetValue() : nullptr;
    const auto agl = vAngles->HasValue() ? &vAngles->GetValue() : nullptr;
    const auto vel = vVelocity->HasValue() ? &vVelocity->GetValue() : nullptr;

    pEntity->Teleport(pos, agl, vel);
}

static void EntitySetMoveType(CBaseEntity* pEntity, MoveType_t type)
{
    pEntity->SetMoveType(type);
}

static void EntitySetGravityScale(CBaseEntity* pEntity, float scale)
{
    pEntity->SetGravityScale(scale);
}

static void EntityApplyAbsVelocityImpulse(CBaseEntity* pEntity, Vector* vVelocity)
{
    pEntity->ApplyAbsVelocityImpulse(vVelocity);
}

static const Vector& EntityGetCenter(CBaseEntity* pEntity)
{
    return pEntity->GetCenter();
}

static const QAngle& EntityGetEyeAngles(CBaseEntity* pEntity)
{
    return pEntity->GetEyeAngles();
}

static const Vector& EntityGetEyePosition(CBaseEntity* pEntity)
{
    return pEntity->GetEyePosition();
}

static const QAngle& EntityGetAbsAngles(CBaseEntity* pEntity)
{
    return pEntity->GetAbsAngles();
}

static void EntitySetAbsAngles(CBaseEntity* pEntity, QAngle* vAngles)
{
    return pEntity->SetAbsAngles(vAngles);
}

static const Vector& EntityGetAbsOrigin(CBaseEntity* pEntity)
{
    return pEntity->GetAbsOrigin();
}

static void EntitySetAbsOrigin(CBaseEntity* pEntity, Vector* vOrigin)
{
    return pEntity->SetAbsOrigin(vOrigin);
}

static const Vector& EntityGetLocalVelocity(CBaseEntity* pEntity)
{
    return pEntity->GetLocalVelocity();
}

static void EntitySetLocalVelocity(CBaseEntity* pEntity, Vector* vVelocity)
{
    return pEntity->SetLocalVelocity(vVelocity);
}

static const Vector& EntityGetAbsVelocity(CBaseEntity* pEntity)
{
    return pEntity->GetAbsVelocity();
}

static void EntitySetAbsVelocity(CBaseEntity* pEntity, Vector* vVelocity)
{
    return pEntity->SetAbsVelocity(vVelocity);
}

static void EntityDispatchSpawn(CBaseEntity* pEntity, const KeyValuesVariantItem* items, int count)
{
    pEntity->DispatchSpawn(items, count);
}

static bool EntityIsPlayerController(CBaseEntity* pEntity)
{
    return pEntity->IsPlayerController();
}

static bool EntityIsPlayerPawn(CBaseEntity* pEntity)
{
    return pEntity->IsPlayerPawn();
}

static void EntitySetBodyGroupByName(CBaseModelEntity* pEntity, const char* pName, int value)
{
    pEntity->SetBodyGroupByName(pName, value);
}

static void EntitySetMaterialGroupMask(CBaseModelEntity* pEntity, int mask)
{
    pEntity->SetMaterialGroupMask(mask);
}

static int32_t EntityLookupAttachment(CBaseModelEntity* pEntity, const char* pName)
{
    return pEntity->LookupAttachment(pName);
}

static void EntityGetAttachment(CBaseModelEntity* pEntity, int32_t attachment, Vector* vOrigin, Vector* vAngles)
{
    pEntity->GetAttachment(attachment, vOrigin, vAngles);
}

static int32_t EntityLookupBone(CBaseModelEntity* pEntity, const char* pName)
{
    return pEntity->LookupBone(pName);
}

static void EntityGetBoneTransform(CBaseModelEntity* pEntity, int32_t bone, matrix3x4_t* matrix)
{
    pEntity->GetBoneTransform(bone, matrix);
}

static void EntitySetModelScale(CBaseModelEntity* pEntity, float scale)
{
    pEntity->SetModelScale(scale);
}

static void EntitySetCollisionBounds(CBaseModelEntity* pEntity, Vector* vMins, Vector* vMaxs)
{
    pEntity->SetCollisionBounds(vMins, vMaxs);
}

///////////////////////////////////////////////////////////////////////////////////////

static void WeaponHolster(CBaseWeapon* pEntity)
{
    pEntity->Holster();
}

static void WeaponDeploy(CBaseWeapon* pEntity)
{
    pEntity->Deploy();
}

static CBaseWeapon* WeaponFromEntity(CBaseEntity* pEntity)
{
    return pEntity->ToWeapon();
}

static bool IsWeapon(CBaseEntity* pEntity)
{
    return pEntity->IsWeapon();
}

///////////////////////////////////////////////////////////////////////////////////////

static CBaseEntity* GetGlobalCStrikeTeam(uint8_t team)
{
    CBaseEntity* pEntity = nullptr;
    while ((pEntity = g_pGameEntitySystem->FindByClassname(pEntity, "cs_team_manager")) != nullptr)
    {
        if (pEntity->GetTeam() == team)
            return pEntity;
    }

    return nullptr;
}

///////////////////////////////////////////////////////////////////////////////////////

void Init()
{
    // Entity List
    bridge::CreateNative("Entity.FindByEHandle", reinterpret_cast<void*>(FindEntityByEHandle));
    bridge::CreateNative("Entity.FindByIndex", reinterpret_cast<void*>(FindEntityByIndex));
    bridge::CreateNative("Entity.FindByClassname", reinterpret_cast<void*>(FindEntityByClassname));
    bridge::CreateNative("Entity.FindByName", reinterpret_cast<void*>(FindEntityByName));
    bridge::CreateNative("Entity.FindInSphere", reinterpret_cast<void*>(FindEntityInSphere));
    bridge::CreateNative("Entity.SpawnEntitySync", reinterpret_cast<void*>(SpawnEntitySync));
    bridge::CreateNative("Entity.CreateByName", reinterpret_cast<void*>(CreateEntityByName));
    bridge::CreateNative("Entity.AllocPooledString", reinterpret_cast<void*>(AllocPooledString));
    bridge::CreateNative("Entity.HookOutput", reinterpret_cast<void*>(HookEntityOutput));
    bridge::CreateNative("Entity.HookInput", reinterpret_cast<void*>(HookEntityInput));

    // CBaseEntity
    bridge::CreateNative("Entity.ChangeTeam", reinterpret_cast<void*>(EntityChangeTeam));
    bridge::CreateNative("Entity.Kill", reinterpret_cast<void*>(EntityKill));
    bridge::CreateNative("Entity.IsValid", reinterpret_cast<void*>(EntityIsValid));
    bridge::CreateNative("Entity.NetworkStateChanged", reinterpret_cast<void*>(EntityNetworkStateChanged));
    bridge::CreateNative("Entity.SetStateChanged", reinterpret_cast<void*>(EntitySetStateChanged));
    bridge::CreateNative("Entity.SetStructStateChanged", reinterpret_cast<void*>(EntitySetStructStateChanged));
    bridge::CreateNative("Entity.CollisionRulesChanged", reinterpret_cast<void*>(EntityCollisionRulesChanged));
    bridge::CreateNative("Entity.EmitSound", reinterpret_cast<void*>(EntityEmitSound));
    bridge::CreateNative("Entity.StopSound", reinterpret_cast<void*>(EntityStopSound));
    bridge::CreateNative("Entity.DispatchTraceAttack", reinterpret_cast<void*>(EntityDispatchTraceAttack));

    bridge::CreateNative("Entity.SetName", reinterpret_cast<void*>(EntitySetName));
    bridge::CreateNative("Entity.SetModel", reinterpret_cast<void*>(EntitySetModel));
    bridge::CreateNative("Entity.SetOwner", reinterpret_cast<void*>(EntitySetOwner));
    bridge::CreateNative("Entity.SetGroundEntity", reinterpret_cast<void*>(EntitySetGroundEntity));
    bridge::CreateNative("Entity.SetSolid", reinterpret_cast<void*>(EntitySetSolid));
    bridge::CreateNative("Entity.Teleport", reinterpret_cast<void*>(EntityTeleport));
    bridge::CreateNative("Entity.SetMoveType", reinterpret_cast<void*>(EntitySetMoveType));
    bridge::CreateNative("Entity.SetGravityScale", reinterpret_cast<void*>(EntitySetGravityScale));
    bridge::CreateNative("Entity.ApplyAbsVelocityImpulse", reinterpret_cast<void*>(EntityApplyAbsVelocityImpulse));

    bridge::CreateNative("Entity.AcceptInput", reinterpret_cast<void*>(EntityAcceptInput));
    bridge::CreateNative("Entity.AcceptInputInt", reinterpret_cast<void*>(EntityAcceptInputInt));
    bridge::CreateNative("Entity.AcceptInputFloat", reinterpret_cast<void*>(EntityAcceptInputFloat));

    bridge::CreateNative("Entity.AddIOEvent", reinterpret_cast<void*>(EntityAddIOEvent));
    bridge::CreateNative("Entity.AddIOEventInt", reinterpret_cast<void*>(EntityAddIOEventInt));
    bridge::CreateNative("Entity.AddIOEventFloat", reinterpret_cast<void*>(EntityAddIOEventFloat));

    bridge::CreateNative("Entity.GetCenter", reinterpret_cast<void*>(EntityGetCenter));
    bridge::CreateNative("Entity.GetEyeAngles", reinterpret_cast<void*>(EntityGetEyeAngles));
    bridge::CreateNative("Entity.GetEyePosition", reinterpret_cast<void*>(EntityGetEyePosition));
    bridge::CreateNative("Entity.GetAbsAngles", reinterpret_cast<void*>(EntityGetAbsAngles));
    bridge::CreateNative("Entity.SetAbsAngles", reinterpret_cast<void*>(EntitySetAbsAngles));
    bridge::CreateNative("Entity.GetAbsOrigin", reinterpret_cast<void*>(EntityGetAbsOrigin));
    bridge::CreateNative("Entity.SetAbsOrigin", reinterpret_cast<void*>(EntitySetAbsOrigin));
    bridge::CreateNative("Entity.GetLocalVelocity", reinterpret_cast<void*>(EntityGetLocalVelocity));
    bridge::CreateNative("Entity.SetLocalVelocity", reinterpret_cast<void*>(EntitySetLocalVelocity));
    bridge::CreateNative("Entity.GetAbsVelocity", reinterpret_cast<void*>(EntityGetAbsVelocity));
    bridge::CreateNative("Entity.SetAbsVelocity", reinterpret_cast<void*>(EntitySetAbsVelocity));

    bridge::CreateNative("Entity.DispatchSpawn", reinterpret_cast<void*>(EntityDispatchSpawn));

    // CBaseModelEntity
    bridge::CreateNative("Entity.SetBodyGroupByName", reinterpret_cast<void*>(EntitySetBodyGroupByName));
    bridge::CreateNative("Entity.SetMaterialGroupMask", reinterpret_cast<void*>(EntitySetMaterialGroupMask));
    bridge::CreateNative("Entity.LookupAttachment", reinterpret_cast<void*>(EntityLookupAttachment));
    bridge::CreateNative("Entity.GetAttachment", reinterpret_cast<void*>(EntityGetAttachment));
    bridge::CreateNative("Entity.LookupBone", reinterpret_cast<void*>(EntityLookupBone));
    bridge::CreateNative("Entity.GetBoneTransform", reinterpret_cast<void*>(EntityGetBoneTransform));
    bridge::CreateNative("Entity.SetModelScale", reinterpret_cast<void*>(EntitySetModelScale));
    bridge::CreateNative("Entity.SetCollisionBounds", reinterpret_cast<void*>(EntitySetCollisionBounds));

    // CBaseWeapon
    bridge::CreateNative("Entity.WeaponHolster", reinterpret_cast<void*>(WeaponHolster));
    bridge::CreateNative("Entity.WeaponDeploy", reinterpret_cast<void*>(WeaponDeploy));
    bridge::CreateNative("Entity.WeaponFromEntity", reinterpret_cast<void*>(WeaponFromEntity));

    // CSTeam
    bridge::CreateNative("Entity.GetGlobalCStrikeTeam", reinterpret_cast<void*>(GetGlobalCStrikeTeam));

    // Is A<>
    bridge::CreateNative("Entity.IsWeapon", reinterpret_cast<void*>(IsWeapon));
    bridge::CreateNative("Entity.IsPlayerController", reinterpret_cast<void*>(EntityIsPlayerController));
    bridge::CreateNative("Entity.IsPlayerPawn", reinterpret_cast<void*>(EntityIsPlayerPawn));
}

EHookAction OnEntityFireOutput(CBaseEntity* pCaller, const CEntityIOOutput* pIO, CBaseEntity* pActivator, float flDelay)
{
    const auto pHooked = s_hookedOutput.find(pCaller->GetClassname());
    if (pHooked == s_hookedOutput.end())
        return EHookAction::Ignored;

    const auto name = LowercaseString(pIO->m_pDesc->m_pName);

    if (std::ranges::find(pHooked->second, name) == pHooked->second.end())
    {
        return EHookAction::Ignored;
    }

    return forwards::OnEntityFireOutput->Invoke(pCaller, name.c_str(), pActivator, flDelay);
}

EHookAction OnEntityAcceptInput(CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue, int outputId)
{
    const auto pHooked = s_hookedInput.find(pInstance->GetClassname());
    if (pHooked == s_hookedInput.end())
        return EHookAction::Ignored;

    const auto input = LowercaseString(pInput);
    if (std::ranges::find(pHooked->second, input) == pHooked->second.end())
    {
        return EHookAction::Ignored;
    }

    return forwards::OnEntityAcceptInput->Invoke(pInstance->GetBaseEntity(), pInput, pValue, pActivator, pCaller);
}

} // namespace natives::entity