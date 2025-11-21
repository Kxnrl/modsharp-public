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

#include "bridge/forwards/forward.h"
#include "bridge/natives/EntityNatives.h"
#include "gamedata.h"
#include "global.h"
#include "manager/ConVarManager.h"
#include "manager/HookManager.h"
#include "module.h"
#include "sdkproxy.h"
#include "strtool.h"
#include "vhook/call.h"
#include "vhook/hook.h"

#include "cstrike/component/CBodyComponent.h"
#include "cstrike/component/CGameSceneNode.h"
#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/entity/CEnvEntityMaker.h"
#include "cstrike/entity/CHudHint.h"
#include "cstrike/entity/CPhysBox.h"
#include "cstrike/entity/CPhysForce.h"
#include "cstrike/entity/CTriggerPush.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/interface/IEngineServer.h"
#include "cstrike/interface/IScriptVM.h"
#include "cstrike/type/CEntityInstance.h"
#include "cstrike/type/CEntityKeyValues.h"
#include "cstrike/type/CEntityPrecacheContext.h"
#include "cstrike/type/CGlobalVars.h"
#include "cstrike/type/CUtlSymbolLarge.h"
#include "cstrike/type/CUtlVector.h"
#include "cstrike/type/EntityIO.h"
#include "cstrike/type/Variant.h"

#include <safetyhook.hpp>

#include <algorithm>

// #define ENTITY_HOOK_ASSERT
// #define ENTITY_LISTENER_ASSERT

static CConVarBaseData* ms_trigger_push_fixes_enabled = nullptr;
static CConVarBaseData* ms_trigger_push_scale         = nullptr;
static CConVarBaseData* ms_entity_io_enhancement      = nullptr;
static CConVarBaseData* ms_entity_io_verbose_logging  = nullptr;
static CConVarBaseData* ms_fix_entities_touching_list = nullptr;

static bool EntityInputEnhancement(const CEntityIdentity* pEntity, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue);

static CBaseEntity* s_pInputCallerOverride = nullptr;

// extern for movement manager
extern void SetPlayerLaggedMovementValue(CCSPlayerController* pController, float flValue);
extern void SetPlayerRunSpeedValue(CCSPlayerController* pController, float flValue);

// 这里涉及到安全问题, 所以暂时全局Hook
// 现在Mapper可以通过该实体传入任意命令!
BeginMemberHookScope(CPointServerCommand)
{
    DeclareMemberDetourHook(InputCommand, void, (CBaseEntity * pEntity, const InputData_t* pInput))
    {
        if (!pInput)
            return;

        const auto pCommand = pInput->value.AutoCastString();

        if (!pCommand)
            return;

        if (forwards::OnPointServerCommand->Invoke(pEntity, pCommand) == EHookAction::SkipCallReturnOverride)
            return;

        engine->ServerCommand(pCommand);
    }
}

// 这里涉及到安全问题, 直接屏蔽这个Function
BeginStaticHookScope(SendToServerConsole)
{
    DeclareStaticDetourHook(SendToServerConsole, void, (const char* pCommand))
    {
        // Blocked!
    }
}

// CTriggerGravity不会触发EndTouch重置
// FGD 里需要修改 gravity(integer) 为 gravity(float)
BeginMemberHookScope(CTriggerGravity)
{
    static std::unordered_map<uint32_t, float> s_gravityMap;

    DeclareVirtualHook(Precache, void, (CBaseEntity * pEntity, const CEntityPrecacheContext* pContext))
    {
        const auto pGravity  = pContext->m_pKeyValues->FindKeyValues("gravity");
        const auto pHammerId = pContext->m_pKeyValues->FindKeyValues("hammerUniqueId");
        if (!pGravity || !pHammerId)
            return Precache(pEntity, pContext);

        const auto flGravity   = pGravity->GetFloat();
        const auto pszHammerId = pHammerId->GetString();
        const auto hEntity     = MurmurHash2Lowercase(pszHammerId, MURMURHASH_SEED);

        s_gravityMap[hEntity] = flGravity;

        Precache(pEntity, pContext);
    }

    DeclareMemberDetourHook(GravityTouch, void, (CBaseEntity * pEntity, CBaseEntity * pOther))
    {
#ifdef ENTITY_HOOK_ASSERT
        WARN("%10s: 0x%p\n"
             "%10s: 0x%p",
             "this", pEntity,
             "pOther", pOther);
#endif

        const auto pszHammerId = pEntity->GetHammerId();
        if (!pszHammerId || !pszHammerId[0])
            return;

        const auto hEntity = MurmurHash2Lowercase(pszHammerId, MURMURHASH_SEED);
        const auto gravity = s_gravityMap.find(hEntity);
        if (gravity != s_gravityMap.end() && pOther->IsPlayerPawn())
        {
            pOther->SetGravityScale(gravity->second);
        }
    }

    DeclareVirtualHook(EndTouch, void, (CBaseEntity * pEntity, CBaseEntity * pOther))
    {
#ifdef ENTITY_HOOK_ASSERT
        WARN("%10s: 0x%p\n"
             "%10s: 0x%p",
             "this", pEntity,
             "pOther", pOther);
#endif

        EndTouch(pEntity, pOther);

        if (pOther->IsPlayerPawn())
        {
            pOther->SetGravityScale(1);
        }
    }

    static void PurgeAllCache()
    {
        s_gravityMap.clear();
    }
}

// CTriggerPush每帧都会Call
BeginMemberHookScope(CTriggerPush)
{
    // SF_TRIG_PUSH_ONCE is handled fine already
    constexpr uint32_t SF_TRIG_PUSH_ONCE = 0x80;

    static bool IsSolid(SolidType_t solidType, int nSolidFlags)
    {
        return (solidType != SOLID_NONE) && ((nSolidFlags & FSOLID_NOT_SOLID) == 0);
    }

    DeclareVirtualHook(Touch, void, (CTriggerPush * pEntity, CBaseEntity * pOther))
    {
#ifdef ENTITY_HOOK_ASSERT
        WARN("%10s: 0x%p"  // CTriggerPush*
             "%10s: 0x%p", // CBaseEntity*
             "this", pEntity,
             "pOther", pOther);
#endif

        if (!ms_trigger_push_fixes_enabled->GetValue<bool>() || pEntity->GetSpawnFlags() & SF_TRIG_PUSH_ONCE)
            return Touch(pEntity, pOther);

        if (pEntity->m_bTriggerOnStartTouch())
            return Touch(pEntity, pOther);

        const auto moveType = pOther->m_nActualMoveType();

        // VPhysics handling doesn't need any changes
        if (moveType == MOVETYPE_VPHYSICS)
            return Touch(pEntity, pOther);

        if (moveType == MOVETYPE_NONE || moveType == MOVETYPE_PUSH || moveType == MOVETYPE_NOCLIP || moveType == MOVETYPE_LADDER)
            return;

        const auto pCollision = pOther->m_pCollision();
        if (!IsSolid(pCollision->m_nSolidType(), pCollision->m_usSolidFlags()))
            return;

        if (pOther->m_CBodyComponent()->m_pSceneNode()->m_pParent())
            return;

        if (!pEntity->PassesTriggerFilters(pOther))
            return;

        Vector vecAbsDir;
        vecAbsDir.Init();

        auto mat = pEntity->m_CBodyComponent()->m_pSceneNode()->EntityToWorldTransform();

        const auto pushDir = pEntity->m_vecPushDirEntitySpace();

        // I had issues with VectorRotate on linux, so I did it here
        vecAbsDir.x = pushDir.x * mat[0][0] + pushDir.y * mat[0][1] + pushDir.z * mat[0][2];
        vecAbsDir.y = pushDir.x * mat[1][0] + pushDir.y * mat[1][1] + pushDir.z * mat[1][2];
        vecAbsDir.z = pushDir.x * mat[2][0] + pushDir.y * mat[2][1] + pushDir.z * mat[2][2];

        auto vecPush = vecAbsDir * pEntity->m_flSpeed() * ms_trigger_push_scale->GetValue<float>();
        auto flags   = pOther->GetFlags();

        if (flags & (FL_BASEVELOCITY))
        {
            vecPush = vecPush + pOther->m_vecBaseVelocity();
        }

        if (vecPush.z > 0 && (flags & FL_ONGROUND))
        {
            pOther->SetGroundEntity(nullptr, nullptr);
            auto origin = pOther->GetAbsOrigin();
            origin.z += 1.0f;

#ifdef USE_ABS
            pOther->SetAbsOrigin(&origin);
#else
            pOther->Teleport(&origin, nullptr, nullptr);
#endif
        }

        pOther->m_vecBaseVelocity(vecPush);
        pOther->AddFlags(FL_BASEVELOCITY);
    }
}

// 实现EventIOOutputHook
BeginMemberHookScope(CEntityIOOutput)
{
    DeclareMemberDetourHook(FireOutput, void, (CEntityIOOutput * pIO, CBaseEntity * pActivator, CBaseEntity * pCaller, const Variant_t& value, float flDelay, void* a6, void* a7))
    {
        if (!pCaller)
            return FireOutput(pIO, pActivator, pCaller, value, flDelay, a6, a7);

        if (natives::entity::OnEntityFireOutput(s_pInputCallerOverride ? s_pInputCallerOverride : pCaller, pIO, pActivator, flDelay) == EHookAction::SkipCallReturnOverride)
            return;

        FireOutput(pIO, pActivator, pCaller, value, flDelay, a6, a7);
    }
}

// 实现劫持AcceptInput
BeginMemberHookScope(CEntityIdentity)
{
    DeclareMemberDetourHook(AcceptInput, bool, (CEntityIdentity * pInstance, CUtlSymbolLarge * pInput, CBaseEntity * pActivator, CBaseEntity * pCaller, Variant_t * pValue, int nOutputId, void* a7, void* a8))
    {
        if (!pInstance || !pInput)
            return AcceptInput(pInstance, pInput, pActivator, pCaller, pValue, nOutputId, a7, a8);

        if (natives::entity::OnEntityAcceptInput(pInstance, pInput->Get(), pActivator, pCaller, pValue, nOutputId) == EHookAction::SkipCallReturnOverride)
            return false;

        if (ms_entity_io_enhancement->GetValue<bool>() && EntityInputEnhancement(pInstance, pInput->Get(), pActivator, pCaller, pValue))
            return true;

        if (g_pScriptVM && (pActivator || pCaller) && strcasecmp(pInput->Get(), "RunScriptCode") == 0)
        {
            const auto pEntity = pInstance->GetBaseEntity();
            const auto pScope  = pEntity->GetPrivateScriptScope();
            if (pEntity && pScope)
            {
                if (pActivator)
                {
                    if (const auto hActivator = pActivator->GetPublicScriptScope())
                    {
                        ScriptVariant_t activator(hActivator);
                        CALL_VIRTUAL(bool, 33, g_pScriptVM, pScope, "activator", &activator);

                        // WARN("Fix Activator %p(%s) for %s -> %s", pActivator, pActivator->GetClassname(), pInstance->GetName(), pInput->Get());
                    }
                }
                if (pCaller)
                {
                    if (const auto hCaller = pCaller->GetPublicScriptScope())
                    {
                        ScriptVariant_t caller(hCaller);
                        CALL_VIRTUAL(bool, 33, g_pScriptVM, pScope, "caller", &caller);

                        // WARN("Fix Caller %p(%s) for %s -> %s", pCaller, pCaller->GetClassname(), pInstance->GetName(), pInput->Get());
                    }
                }
            }

            // g_pScriptVM->SetValueVariant(pScope, "caller", &caller);
        }

        return AcceptInput(pInstance, pInput, pActivator, pCaller, pValue, nOutputId, a7, a8);
    }
}

BeginMemberHookScope(CPhysBox)
{
    // 这里没法直接替换 pInput 里面的东西, 因为他是递归调用, 先调用BaseClass::Use
    // 如果直接替换了, 可能会导致不可预料的问题
    DeclareVirtualHook(Use, void, (CPhysBox * pBreakable, int64_t pInput))
    {
        if (pBreakable->m_bEnableUseOutput())
        {
            // Activator 在这里是没问题的
            s_pInputCallerOverride = pBreakable;
            Use(pBreakable, pInput);
            s_pInputCallerOverride = nullptr;
        }
        else
        {
            Use(pBreakable, pInput);
        }
    }
}

BeginMemberHookScope(CVPhys2World)
{
    struct TouchLinked_t
    {
        uint32_t TouchFlags;

    private:
        uint8_t padding_0[20];

    public:
        CBaseHandle SourceHandle;
        CBaseHandle TargetHandle;

    private:
        uint8_t padding_1[224];

    public:
        [[nodiscard]] bool IsUnTouching() const
        {
            return !!(TouchFlags & 0x10);
        }

        [[nodiscard]] bool IsTouching() const
        {
            return (!!(TouchFlags & 4)) || (!!(TouchFlags & 8));
        }
    };
    static_assert(sizeof(TouchLinked_t) == 256, "Touch_t size mismatch");

    DeclareVirtualHook(GetTouchingList, void, (void* pVphysicsWorld, CUtlVector<TouchLinked_t>* pList, bool unknown))
    {
        GetTouchingList(pVphysicsWorld, pList, unknown);

        if (pList->Count() <= 1 || !ms_fix_entities_touching_list->GetValue<bool>())
            return;

        std::srand(gpGlobals->nTickCount);

#ifdef DEBUG

        std::string dump{};

        FOR_EACH_VEC(*pList, i)
        {
            char       buffer[128];
            const auto source = entList->FindEntityByEHandle(pList->Element(i).SourceHandle);
            const auto target = entList->FindEntityByEHandle(pList->Element(i).TargetHandle);
            snprintf(buffer, sizeof(buffer), "#%d flags: %d | source: %d.%s | target: %d.%s\n",
                     i,
                     pList->Element(i).TouchFlags,
                     pList->Element(i).SourceHandle.GetEntryIndex(), source ? source->GetClassname() : "<null>",
                     pList->Element(i).TargetHandle.GetEntryIndex(), target ? target->GetClassname() : "<null>");
            dump.append(buffer);
        }
#endif

        std::vector<TouchLinked_t> touchingLinks{};
        std::vector<TouchLinked_t> unTouchLinks{};

        for (auto i = 0; i < pList->Count(); i++)
        {
            const auto& link = pList->Element(i);
            if (link.IsUnTouching())
                unTouchLinks.push_back(link);
            else
                touchingLinks.push_back(link);
        }

#ifdef DEBUG
        if (touchingLinks.empty())
            return;

        bool foundUnTouch = false;
        FOR_EACH_VEC(*pList, i)
        {
            const auto& link = pList->Element(i);
            if (link.IsUnTouching())
                foundUnTouch = true;
            else if (foundUnTouch)
            {
                const auto source = entList->FindEntityByEHandle(pList->Element(i).SourceHandle);
                const auto target = entList->FindEntityByEHandle(pList->Element(i).TargetHandle);
                WARN("@%d Regular Touching after UnTouch: #%d flags: %d | source: %d.%s | target: %d.%s",
                     gpGlobals->nTickCount,
                     i,
                     pList->Element(i).TouchFlags,
                     pList->Element(i).SourceHandle.GetEntryIndex(), source ? source->GetClassname() : "<null>",
                     pList->Element(i).TargetHandle.GetEntryIndex(), target ? target->GetClassname() : "<null>");
            }
        }
#endif

        if (touchingLinks.size() <= 1)
        {
#ifdef DEBUG
            LOG("@%d Kept Physics List -> %d pairs\n%s", gpGlobals->nTickCount, pList->Count(), dump.c_str());
#endif
            return;
        }

        for (size_t i = touchingLinks.size() - 1; i > 0; --i)
        {
            const auto j = std::rand() % (i + 1);
            std::swap(touchingLinks[i], touchingLinks[j]);
        }

        pList->Purge();

        for (const auto& link : touchingLinks)
            pList->AddToTail(link);
        for (const auto& link : unTouchLinks)
            pList->AddToTail(link);

#ifdef DEBUG
        LOG("@%d Fixup Physics List -> %d pairs\n%s", gpGlobals->nTickCount, pList->Count(), dump.c_str());
#endif
    }
}

class CEntityListener : public IEntityListener
{
    void OnEntityCreated(CBaseEntity* pEntity) override
    {
#ifdef ENTITY_LISTENER_ASSERT
        const auto i = pEntity->GetEntityIndexNative();
        const auto n = pEntity->GetClassname();

        ConColorMsg(ConsoleColor(35, 132, 239), "OnEntityCreated -> entity: %32s -> %d\n", n, i);
#endif

#if 0
        static auto bHookPlayerCharacter = false;
        if (!bHookPlayerCharacter)
        {
            extern bool HookCharacterEvent(CBaseEntity * pEntity);
            bHookPlayerCharacter = HookCharacterEvent(pEntity);
        }
#endif

#ifdef ENTITY_LISTENER_ASSERT
        // Assert Test
        {
            const auto de = pEntity->GetActualEHandle();
            const auto ne = pEntity->GetRefEHandleNative();

            const auto di = pEntity->GetEntityIndex();
            const auto ni = pEntity->GetEntityIndexNative();

            AssertBool(de.GetSerialNum() == ne.GetSerialNum());
            AssertBool(di == ni);
        }
#endif

        forwards::OnEntityCreated->Invoke(pEntity);
    }

    void OnEntitySpawned(CBaseEntity* pEntity) override
    {
#ifdef ENTITY_LISTENER_ASSERT
        const auto i = pEntity->GetEntityIndexNative();
        const auto n = pEntity->GetClassname();

        ConColorMsg(ConsoleColor(65, 250, 65), "OnEntitySpawned -> entity: %32s -> %d\n", n, i);
#endif

        forwards::OnEntitySpawned->Invoke(pEntity);
    }

    void OnEntityDeleted(CBaseEntity* pEntity) override
    {
#ifdef ENTITY_LISTENER_ASSERT
        const auto i = pEntity->GetEntityIndexNative();
        const auto n = pEntity->GetClassname();

        ConColorMsg(ConsoleColor(201, 8, 66), "OnEntityDeleted -> entity: %32s -> %d\n", n, i);
#endif

        forwards::OnEntityDeleted->Invoke(pEntity);
    }

    void OnEntityFollowed(CBaseEntity* pEntity, CBaseEntity* pOwner) override
    {
#ifdef ENTITY_LISTENER_ASSERT
        const auto i = pEntity->GetEntityIndexNative();
        const auto n = pEntity->GetClassname();

        const auto v = pOwner ? pOwner->GetEntityIndexNative() : -1;
        const auto x = pOwner ? pOwner->GetClassname() : "<null>";

        ConColorMsg(ConsoleColor(235, 142, 21), "OnEntityFollow  -> entity: %32s -> %d\n", n, i);
        ConColorMsg(ConsoleColor(235, 142, 21), "         Owner  -> entity: %32s -> %d\n", x, v);
#endif

        forwards::OnEntityFollowed->Invoke(pEntity, pOwner);
    }

} g_EntityListener;

void InstallEntityHooks()
{
    InstallMemberDetourAutoSig(CPointServerCommand, InputCommand);
    InstallMemberDetourAutoSig(CEntityIOOutput, FireOutput);
    InstallMemberDetourAutoSig(CEntityIdentity, AcceptInput);
    InstallMemberDetourAutoSig(CTriggerGravity, GravityTouch);

    InstallStaticDetourAutoSig(SendToServerConsole);

    InstallVirtualHookManualWithVTableAuto(CTriggerGravity, Precache, server, "CBaseEntity::Precache");
    InstallVirtualHookManualWithVTableAuto(CTriggerGravity, EndTouch, server, "CBaseEntity::EndTouch");
    InstallVirtualHookManualWithVTableAuto(CTriggerPush, Touch, server, "CBaseEntity::Touch");
    InstallVirtualHookManualWithVTableAuto(CPhysBox, Use, server, "CBaseEntity::Use");
    InstallVirtualHookAutoWithVTableAuto(CVPhys2World, GetTouchingList, vphysics2);

    g_pHookManager->Hook_PlayerSpawned(HookType_Post, [](CCSPlayerPawn* pPawn, CServerSideClient* pClient) {
        if (g_pScriptVM)
        {
            const auto pPrivate = pPawn->GetOrCreatePrivateScriptScope();
            const auto pPublic  = pPawn->GetOrCreatePublicScriptScope();

            AssertPtr(pPrivate);
            AssertPtr(pPublic);
        }
    });

    g_pHookManager->Hook_GameShutdown(HookType_Post, []() {
        CTriggerGravity_Hooks::PurgeAllCache();
    });

    g_pGameEntitySystem->AddListenerEntity(&g_EntityListener);

    ms_trigger_push_fixes_enabled = g_ConVarManager.CreateConVar("ms_trigger_push_fixes_enabled", false, "Enabled trigger_push fix", FCVAR_RELEASE);
    ms_trigger_push_scale         = g_ConVarManager.CreateConVar("ms_trigger_push_scale", 1.f, true, 0.001f, true, 1000.f, "Set Trigger push scale", FCVAR_RELEASE);
    ms_entity_io_enhancement      = g_ConVarManager.CreateConVar("ms_entity_io_enhancement", false, "Enabled entity IO enhancement", FCVAR_RELEASE);
    ms_entity_io_verbose_logging  = g_ConVarManager.CreateConVar("ms_entity_io_verbose_logging", false, "Enabled entity IO verbose logging", FCVAR_RELEASE);
    ms_fix_entities_touching_list = g_ConVarManager.CreateConVar("ms_fix_entities_touching_list", true, "Fix entities touching list and shuffle", FCVAR_RELEASE);
}

using InputEnhancement_t = bool (*)(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue);
using AddOutputHandler_t = void (*)(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs);

struct AddOutputKey_t
{
    AddOutputKey_t(const char* pName, size_t parts)
    {
        m_sName  = std::string(pName);
        m_nParts = parts;
    }

    AddOutputKey_t(const AddOutputKey_t& other) :
        m_sName(other.m_sName), m_nParts(other.m_nParts) {}

    std::string m_sName;
    size_t      m_nParts;
};

inline static bool GetVariantInt(const Variant_t* pValue, int& value)
{
    if (pValue->FieldType() == FieldType_t::FIELD_INT32)
    {
        value = pValue->Int();
        return true;
    }

    if (const auto pszValue = pValue->AutoCastString())
    {
        value = atoi(pszValue);
        return true;
    }

    return false;
}

inline static bool GetVariantFloat(const Variant_t* pValue, float& value)
{
    if (pValue->FieldType() == FieldType_t::FIELD_FLOAT32)
    {
        value = pValue->Float();
        return true;
    }

    if (const auto pszValue = pValue->AutoCastString())
    {
        value = static_cast<float>(atof(pszValue));
        return true;
    }

    return false;
}

#define InputError() \
    WARN("%d.%s Error Input '%s': failed to parse param", pInstance->GetEntityIndex(), pInstance->GetName(), pInput)

#define InputErrorParam() \
    WARN("%d.%s Error Input '%s' with params: %s", pInstance->GetEntityIndex(), pInstance->GetName(), pInput, param)

#define InputErrorCustom(error) \
    WARN("%d.%s Error Input '%s' with params: %s -> %s", pInstance->GetEntityIndex(), pInstance->GetName(), pInput, param, error)

#define InputErrorCustomNoParam(error) \
    WARN("%d.%s Error Input '%s' -> %s", pInstance->GetEntityIndex(), pInstance->GetName(), pInput, error)

struct AddOutputInfo_t
{
    AddOutputInfo_t(const AddOutputKey_t& key, const AddOutputHandler_t& handler) :
        m_Key(key), m_Handler(handler) {}

    AddOutputKey_t     m_Key;
    AddOutputHandler_t m_Handler;
};

struct CustomInputInfo_t
{
    CustomInputInfo_t(const char* key, const InputEnhancement_t& handler) :
        m_Key(key), m_Handler(handler) {}

    const char*        m_Key;
    InputEnhancement_t m_Handler;
};

static void AddOutputCustom_Targetname(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    pInstance->GetBaseEntity()->SetName(vecArgs[1].c_str());
}

static void AddOutputCustom_Origin(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    Vector origin(std::clamp(static_cast<float>(atof(vecArgs[1].c_str())), -16384.f, 16384.f), std::clamp(static_cast<float>(atof(vecArgs[2].c_str())), -16384.f, 16384.f), std::clamp(static_cast<float>(atof(vecArgs[3].c_str())), -16384.f, 16384.f));
    pInstance->GetBaseEntity()->Teleport(&origin, nullptr, nullptr);
}

static void AddOutputCustom_Angles(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    QAngle angles(std::clamp(static_cast<float>(atof(vecArgs[1].c_str())), -360.f, 360.f), std::clamp(static_cast<float>(atof(vecArgs[2].c_str())), -360.f, 360.f), std::clamp(static_cast<float>(atof(vecArgs[3].c_str())), -360.f, 360.f));
    pInstance->GetBaseEntity()->Teleport(nullptr, &angles, nullptr);
}

static void AddOutputCustom_MaxHealth(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    pInstance->GetBaseEntity()->m_iMaxHealth(std::clamp((int)atoi(vecArgs[1].c_str()), 1, 99999999));
}

static void AddOutputCustom_Health(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const auto pEntity = pInstance->GetBaseEntity();
    const auto value   = std::clamp((int)atoi(vecArgs[1].c_str()), 1, 99999999);

    if (value < 0 && pEntity->IsPlayerPawn())
    {
        reinterpret_cast<CCSPlayerPawn*>(pEntity)->CommitSuicide(false);
    }
    else
    {
        pEntity->m_iHealth(value);
    }
}

static void AddOutputCustom_Solid(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const auto value = std::clamp(static_cast<SolidType_t>(atoi(vecArgs[1].c_str())), SOLID_NONE, SOLID_VPHYSICS);

    const auto pEntity = pInstance->GetBaseEntity();
    if (const auto pCollision = pEntity->m_pCollision())
    {
        pCollision->SetSolid(static_cast<SolidType_t>(value));
        pEntity->CollisionRulesChanged();

        if (ms_entity_io_verbose_logging->GetValue<bool>())
        {
            LOG("SetSolid %s to %d", pEntity->GetName(), value);
        }
    }
    else
    {
        InputErrorCustomNoParam("Entity is not BaseModelEntity");
    }
}

static void AddOutputCustom_MoveType(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    static Vector stopVelocity(0, 0, 0);
    const auto    value = std::clamp(static_cast<MoveType_t>(atoi(vecArgs[1].c_str())), MOVETYPE_NONE, MOVETYPE_LAST);
    if (const auto type = static_cast<MoveType_t>(std::clamp(value, MOVETYPE_NONE, MOVETYPE_LAST)); type == MOVETYPE_NONE || type == MOVETYPE_OBSOLETE || type == MOVETYPE_WALK)
    {
        const auto pEntity = pInstance->GetBaseEntity();
        pEntity->SetMoveType(type);
        if (type == MOVETYPE_NONE || type == MOVETYPE_OBSOLETE)
            pEntity->Teleport(nullptr, nullptr, &stopVelocity);
    }
    else
        InputErrorCustomNoParam("Only MOVETYPE_NONE and MOVETYPE_WALK are supported");
}

static void AddOutputCustom_EntityTemplate(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    if (strcmp(pInstance->GetClassname(), "env_entity_maker") == 0)
    {
        const auto pEntity = pInstance->GetBaseEntity<CEnvEntityMaker*>();

        const auto& value = g_pGameEntitySystem->AllocPooledString(vecArgs[1].c_str());
        pEntity->m_iszTemplate(value);

        if (ms_entity_io_verbose_logging->GetValue<bool>())
        {
            LOG("Set EntityTemplate to %s for %s", value.Get(), pInstance->GetName());
        }
    }
    else
        InputErrorCustomNoParam("Only env_entity_maker is supported");
}

static void AddOutputCustom_BaseVelocity(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const Vector velocity(std::clamp(static_cast<float>(atof(vecArgs[1].c_str())), -3500.f, 3500.f), std::clamp(static_cast<float>(atof(vecArgs[2].c_str())), -3500.f, 3500.f), std::clamp(static_cast<float>(atof(vecArgs[3].c_str())), -3500.f, 3500.f));
    const auto   pEntity = pInstance->GetBaseEntity();
    pEntity->m_vecBaseVelocity(velocity);
}

static void AddOutputCustom_AbsVelocity(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    Vector velocity(std::clamp(static_cast<float>(atof(vecArgs[1].c_str())), -3500.f, 3500.f), std::clamp(static_cast<float>(atof(vecArgs[2].c_str())), -3500.f, 3500.f), std::clamp(static_cast<float>(atof(vecArgs[3].c_str())), -3500.f, 3500.f));
    pInstance->GetBaseEntity()->SetAbsVelocity(&velocity);
}

static void AddOutputCustom_Target(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    if (const auto pTarget = g_pGameEntitySystem->FindByName(nullptr, vecArgs[1].c_str()))
    {
        const auto pEntity = pInstance->GetBaseEntity();
        pEntity->m_target(const_cast<char*>(pTarget->GetName()));

        if (ms_entity_io_verbose_logging->GetValue<bool>())
        {
            LOG("Set Target to %s for %s", pTarget->GetName(), pEntity->GetName());
        }
    }
    else
    {
        InputErrorCustomNoParam("Target not found");
    }
}

static void AddOutputCustom_FilterName(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    if (strncasecmp(pInstance->GetClassname(), "trigger_", 8) == 0)
    {
        if (const auto pTarget = g_pGameEntitySystem->FindByName(nullptr, vecArgs[1].c_str()))
        {
            if (strncasecmp(pTarget->GetClassname(), "filter_", 7) == 0)
            {
                const auto pTrigger = pInstance->GetBaseEntity<CBaseTrigger*>();
                pTrigger->m_iFilterName(const_cast<char*>(pTarget->GetName()));
                pTrigger->m_hFilter(pTarget->GetActualEHandle());

                if (ms_entity_io_verbose_logging->GetValue<bool>())
                {
                    LOG("Set FilterName to %s for %s", pTarget->GetName(), pTrigger->GetName());
                }
            }
            else
            {
                InputErrorCustomNoParam("Target is not filter");
            }
        }
        else
        {
            InputErrorCustomNoParam("Filter not found");
        }
    }
    else
    {
        InputErrorCustomNoParam("Entity is not trigger");
    }
}

static void AddOutputCustom_Force(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const auto value = static_cast<float>(atof(vecArgs[1].c_str()));

    const auto pEntity = pInstance->GetBaseEntity<CPhysThruster*>();
    if (strcasecmp(pEntity->GetClassname(), "phys_thruster") == 0)
    {
        pEntity->m_force(value);

        if (ms_entity_io_verbose_logging->GetValue<bool>())
        {
            LOG("Set force to %f for %s", value, pEntity->GetName());
        }
    }
    else
    {
        InputErrorCustomNoParam("Only phys_thruster is supported");
    }
}

static void AddOutputCustom_Gravity(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const auto value = static_cast<float>(atof(vecArgs[1].c_str()));
    pInstance->GetBaseEntity()->SetGravityScale(value);
}

static void AddOutputCustom_Timescale(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const auto value = static_cast<float>(atof(vecArgs[1].c_str()));

    // pInstance->m_flTimeScale = value;

    WARN("Blocked Set timescale<Blocked> to %f for %s", value, pInstance->GetName());
}

static void AddOutputCustom_Friction(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const auto value = static_cast<float>(atof(vecArgs[1].c_str()));

    pInstance->GetBaseEntity()->m_flFriction(value);
}

static void AddOutputCustom_Speed(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const auto pEntity = pInstance->GetBaseEntity();

    if (!pEntity->IsPlayerPawn())
        return;

    const auto pPawn = reinterpret_cast<CCSPlayerPawn*>(pEntity);
    if (!pPawn->IsPlayer())
        return;

    const auto pController = pPawn->GetOriginalController<CCSPlayerController*>();
    if (!pController || !pController->IsConnected())
        return;

    const auto value = std::clamp(static_cast<float>(atof(vecArgs[1].c_str())), 0.01f, 5.0f);

    SetPlayerLaggedMovementValue(pController, value);
}

static void AddOutputCustom_RunSpeed(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const auto pEntity = pInstance->GetBaseEntity();

    if (!pEntity->IsPlayerPawn())
        return;

    const auto pPawn = reinterpret_cast<CCSPlayerPawn*>(pEntity);
    if (!pPawn->IsPlayer())
        return;

    const auto pController = pPawn->GetOriginalController<CCSPlayerController*>();
    if (!pController || !pController->IsConnected())
        return;

    const auto value = std::clamp(static_cast<float>(atof(vecArgs[1].c_str())), 0.01f, 5.0f);

    SetPlayerRunSpeedValue(pController, value);
}

static void AddOutputCustom_CanBePickedUp(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, const std::vector<std::string>& vecArgs)
{
    const auto pEntity = reinterpret_cast<CBaseWeapon*>(pInstance->GetBaseEntity());

    if (pEntity->IsWeapon())
    {
        const auto value = !!std::clamp((int)atoi(vecArgs[1].c_str()), 0, 1);
        pEntity->SetCanBePickedUp(value);
    }
    else
    {
        InputErrorCustomNoParam("Only weapon is supported");
    }
}

const std::vector<AddOutputInfo_t> s_AddOutputHandlers = {
    {{"targetname", 2},     AddOutputCustom_Targetname    },
    {{"origin", 4},         AddOutputCustom_Origin        },
    {{"angles", 4},         AddOutputCustom_Angles        },
    {{"angle", 4},          AddOutputCustom_Angles        },
    {{"max_health", 2},     AddOutputCustom_MaxHealth     },
    {{"health", 2},         AddOutputCustom_Health        },
    {{"solid", 2},          AddOutputCustom_Solid         },
    {{"movetype", 2},       AddOutputCustom_MoveType      },
    {{"EntityTemplate", 2}, AddOutputCustom_EntityTemplate},
    {{"basevelocity", 4},   AddOutputCustom_BaseVelocity  },
    {{"absvelocity", 4},    AddOutputCustom_AbsVelocity   },
    {{"target", 2},         AddOutputCustom_Target        },
    {{"filtername", 2},     AddOutputCustom_FilterName    },
    {{"force", 2},          AddOutputCustom_Force         },
    {{"gravity", 2},        AddOutputCustom_Gravity       },
    {{"timescale", 2},      AddOutputCustom_Timescale     },
    {{"friction", 2},       AddOutputCustom_Friction      },
    {{"speed", 2},          AddOutputCustom_Speed         },
    {{"runspeed", 2},       AddOutputCustom_RunSpeed      },
    {{"canbepickedup", 2},  AddOutputCustom_CanBePickedUp },
};

static bool CustomInput_CustomAddOutput(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (const auto param = pValue->AutoCastString())
    {
        for (const auto& [input, handler] : s_AddOutputHandlers)
        {
            if (strncasecmp(param, input.m_sName.c_str(), input.m_sName.size()) == 0 && param[input.m_sName.size()] == ' ')
            {
                const auto split = StringSplit(param, " ");

                if (split.size() == input.m_nParts)
                {
                    handler(pInstance, pInput, pActivator, pCaller, split);
                }
                else
                {
                    InputErrorParam();
                }

                return true;
            }
        }

        WARN("Unknown param '%s' for entity %d.%s.%s", param, pInstance->GetEntityIndex(), pInstance->GetClassname(), pInstance->GetName());
    }

    // always handled;
    return true;
}
static bool CustomInput_AddEffects(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (int flags; GetVariantInt(pValue, flags))
    {
        const auto pEntity = pInstance->GetBaseEntity();
        if (const auto effects = pEntity->m_fEffects(); (effects & flags) == 0)
            pEntity->m_fEffects(effects | flags);
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}
static bool CustomInput_RemoveEffects(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (int value; GetVariantInt(pValue, value))
    {
        const auto flags   = static_cast<uint32_t>(value);
        const auto pEntity = pInstance->GetBaseEntity();
        auto       effects = pEntity->m_fEffects();
        if ((effects & flags) == flags)
        {
            effects &= ~flags;
            pEntity->m_fEffects(effects);
        }
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}
static bool CustomInput_SetName(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (const auto param = pValue->AutoCastString())
    {
        pInstance->GetBaseEntity()->SetName(param);
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}
static bool CustomInput_SetGravity(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (float value; GetVariantFloat(pValue, value))
    {
        pInstance->GetBaseEntity()->SetGravityScale(std::clamp(value, 0.f, 10.f));
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}
static bool CustomInput_SetMaxHealth(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (int value; GetVariantInt(pValue, value))
    {
        pInstance->GetBaseEntity()->m_iMaxHealth(std::clamp(value, 1, 99999999));
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}
static bool CustomInput_SetModel(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (const auto param = pValue->AutoCastString())
    {
        const auto pEntity = pInstance->GetBaseEntity();

        std::string model(param);
        model.append("_c");

        if (GameFileExists(model.c_str()))
        {
            if (pEntity->m_pCollision())
                pEntity->SetModel(param);
            else
                InputErrorCustom("Entity is not BaseModelEntity");
        }
        else
        {
            InputErrorCustom("Model does not exists");
        }
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}
static bool CustomInput_SetAbsAngles(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (const auto param = pValue->AutoCastString())
    {
        if (const auto split = StringSplit(param, " "); split.size() == 3)
        {
            QAngle angles(std::clamp(static_cast<float>(atof(split[0].c_str())), -360.f, 360.f), std::clamp(static_cast<float>(atof(split[1].c_str())), -360.f, 360.f), std::clamp(static_cast<float>(atof(split[2].c_str())), -360.f, 360.f));
            pInstance->GetBaseEntity()->Teleport(nullptr, &angles, nullptr);
        }
        else
        {
            InputErrorParam();
        }
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}
static bool CustomInput_SetAbsOrigin(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (const auto param = pValue->AutoCastString())
    {
        if (const auto split = StringSplit(param, " "); split.size() == 3)
        {
            Vector origin(std::clamp(static_cast<float>(atof(split[0].c_str())), -16384.f, 16384.f), std::clamp(static_cast<float>(atof(split[1].c_str())), -16384.f, 16384.f), std::clamp(static_cast<float>(atof(split[2].c_str())), -16384.f, 16384.f));
            pInstance->GetBaseEntity()->Teleport(&origin, nullptr, nullptr);
        }
        else
        {
            InputErrorParam();
        }
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}
static bool CustomInput_SetSolidType(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (int value; GetVariantInt(pValue, value))
    {
        const auto pEntity = pInstance->GetBaseEntity();
        if (const auto pCollision = pEntity->m_pCollision())
        {
            value = std::clamp(static_cast<SolidType_t>(value), SOLID_NONE, SOLID_VPHYSICS);
            pCollision->SetSolid(static_cast<SolidType_t>(value));
            pEntity->CollisionRulesChanged();
        }
        else
        {
            InputErrorCustomNoParam("Entity is not BaseModelEntity");
        }
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}
static bool CustomInput_SetMoveType(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    static Vector stopVelocity(0, 0, 0);

    if (int value; GetVariantInt(pValue, value))
    {
        if (const auto type = static_cast<MoveType_t>(std::clamp(static_cast<MoveType_t>(value), MOVETYPE_NONE, MOVETYPE_LAST)); type == MOVETYPE_NONE || type == MOVETYPE_OBSOLETE || type == MOVETYPE_WALK)
        {
            const auto pEntity = pInstance->GetBaseEntity();
            pEntity->SetMoveType(type);
            if (type == MOVETYPE_NONE || type == MOVETYPE_OBSOLETE)
                pEntity->Teleport(nullptr, nullptr, &stopVelocity);
        }
        else
            InputErrorCustomNoParam("Only MOVETYPE_NONE and MOVETYPE_WALK are supported");
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}

static bool CustomInput_ModifySpeed(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    const auto pEntity = pInstance->GetBaseEntity();
    if (float value; GetVariantFloat(pValue, value))
    {
        if (const auto pPawn = pEntity->ToPlayerPawn())
        {
            if (const auto pController = pPawn->GetController<CCSPlayerController*>())
            {
                SetPlayerLaggedMovementValue(pController, value);
            }
        }
        else
        {
            InputErrorCustomNoParam("Only PlayerPawn is supported");
        }
    }
    else
    {
        InputError();
    }

    // always handled;
    return true;
}

static bool CustomInput_SetMessage(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    if (const auto param = pValue->AutoCastString())
    {
        const auto pEntity = reinterpret_cast<CEnvHudHint*>(pInstance->GetBaseEntity());
        if (strcasecmp(pEntity->GetClassname(), "env_hudhint") == 0)
        {
            const auto& message = g_pGameEntitySystem->AllocPooledString(param);
            pEntity->m_iszMessage(message);

            if (ms_entity_io_verbose_logging->GetValue<bool>())
            {
                LOG("Set HudMessage to %s for %s", message.Get(), pInstance->GetName());
            }

            return true;
        }
    }
    else
    {
        InputError();
    }

    // probably handle to other entity
    return false;
}

const std::vector<CustomInputInfo_t> s_InputEnhancementInputs = {
    {"CustomAddOutput", CustomInput_CustomAddOutput},
    {"AddCustomOutput", CustomInput_CustomAddOutput},
    {"KeyValues",       CustomInput_CustomAddOutput},
    {"KeyValue",        CustomInput_CustomAddOutput},
    {"AddEffects",      CustomInput_AddEffects     },
    {"RemoveEffects",   CustomInput_RemoveEffects  },
    {"SetName",         CustomInput_SetName        },
    {"SetGravity",      CustomInput_SetGravity     },
    {"SetMaxHealth",    CustomInput_SetMaxHealth   },
    {"SetModel",        CustomInput_SetModel       },
    {"SetAbsAngles",    CustomInput_SetAbsAngles   },
    {"SetAbsOrigin",    CustomInput_SetAbsOrigin   },
    {"SetSolidType",    CustomInput_SetSolidType   },
    {"SetMoveType",     CustomInput_SetMoveType    },
    {"ModifySpeed",     CustomInput_ModifySpeed    },
    {"SetMessage",      CustomInput_SetMessage     },
};

// Handler bool
// - true : intercept action
// - false: allow passes to game io

static bool EntityInputEnhancement(const CEntityIdentity* pInstance, const char* pInput, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue)
{
    for (auto& [input, handler] : s_InputEnhancementInputs)
    {
        if (strcasecmp(pInput, input) == 0)
        {
            return handler(pInstance, pInput, pActivator, pCaller, pValue);
        }
    }
    return false;
}