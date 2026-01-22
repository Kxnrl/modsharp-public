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
#include "global.h"
#include "manager/HookManager.h"
#include "module.h"
#include "vhook/hook.h"

#include "cstrike/component/PlayerPawnComponent.h"
#include "cstrike/entity/CBaseWeapon.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CTakeDamageInfo.h"
#include "cstrike/type/CTakeDamageResult.h"

#include <safetyhook.hpp>

// #define PLAYER_EVENT_ASSERT
// #define WEAPON_EVENT_ASSERT

// forward to movement
extern float GetPlayerRunSpeedValue(CCSPlayerController* pController);

#define ValidateCC()                                                       \
    const auto pController = pPawn->GetController<CCSPlayerController*>(); \
    AssertPtr(pController);                                                \
    const auto pClient = sv->GetClient(pController->GetPlayerSlot());      \
    AssertPtr(pClient)

BeginMemberHookScope(CCSPlayerPawn)
{
    DeclareVirtualHook(Event_Killed, uint8_t, (CCSPlayerPawn * pPawn, CTakeDamageResult * pResult))
    {
        const auto pController = pPawn->GetController<CCSPlayerController*>();
        const auto pClient     = pController ? sv->GetClient(pController->GetPlayerSlot()) : nullptr;

        if (pClient)
        {
            g_pHookManager->Call_PlayerKilled(HookType_Pre, pPawn, pClient);
            forwards::OnPlayerKilledPre->Invoke(pClient, pController, pPawn, pResult);
        }

        const auto ret = Event_Killed(pPawn, pResult);

        if (pClient)
        {
            g_pHookManager->Call_PlayerKilled(HookType_Post, pPawn, pClient);
            forwards::OnPlayerKilledPost->Invoke(pClient, pController, pPawn, pResult);
        }

#ifdef PLAYER_EVENT_ASSERT
        LOG("CCSPlayerPawn::Event_Killed %p", pPawn);
#endif

        return ret;
    }

    DeclareVirtualHook(PlayerSpawn, void, (CCSPlayerPawn * pPawn, void* a2))
    {
        const auto pController = pPawn->GetController<CCSPlayerController*>();
        const auto pClient     = pController ? sv->GetClient(pController->GetPlayerSlot()) : nullptr;

        if (pClient)
        {
            g_pHookManager->Call_PlayerSpawned(HookType_Pre, pPawn, pClient);
            forwards::OnPlayerSpawnPre->Invoke(pClient, pController, pPawn);
        }

        PlayerSpawn(pPawn, a2);

        if (pClient)
        {
            g_pHookManager->Call_PlayerSpawned(HookType_Post, pPawn, pClient);
            forwards::OnPlayerSpawnPost->Invoke(pClient, pController, pPawn);
        }

#ifdef PLAYER_EVENT_ASSERT
        LOG("CCSPlayerPawn::PlayerSpawn %p", pPawn);
#endif
    }

    DeclareVirtualHook(PlayerPreThink, void, (CCSPlayerPawn * pPawn))
    {
        ValidateCC();

        PlayerPreThink(pPawn);

        forwards::OnPlayerPreThink->Invoke(pClient, pController, pPawn);
#ifdef PLAYER_EVENT_ASSERT
        // LOG("CCSPlayerPawn::PlayerPreThink %p", pPawn);
#endif
    }

    DeclareVirtualHook(PlayerPostThink, void, (CCSPlayerPawn * pPawn))
    {
        ValidateCC();

        PlayerPostThink(pPawn);

        forwards::OnPlayerPostThink->Invoke(pClient, pController, pPawn);
#ifdef PLAYER_EVENT_ASSERT
        // LOG("CCSPlayerPawn::PlayerPostThink %p", pPawn);
#endif
    }

#ifdef PLATFORM_WINDOWS
    static SafetyHookMid hk_ProcessMovement_AssignMaxSpeed{};

    static void ProcessMovement_AssignMaxSpeed(SafetyHookContext & ctx)
    {
        AssertPtr(gpGlobals);

        const auto pServices = reinterpret_cast<CCSPlayer_MovementServices*>(ctx.rdi);
        const auto pPawn     = pServices->GetPawn<CCSPlayerPawn*>();
        AssertPtr(pPawn);
        ValidateCC();

        const float originalSpeed = ctx.xmm6.f32[0];
        auto        speed         = originalSpeed;
        const auto  action        = forwards::OnPlayerGetMaxSpeed->Invoke(pClient, pController, pPawn, originalSpeed, &speed);
        const auto  runSpeed      = GetPlayerRunSpeedValue(pController);

        if (action == EHookAction::SkipCallReturnOverride)
        {
            ctx.xmm6.f32[0] = speed * runSpeed;
        }
        else
        {
            ctx.xmm6.f32[0] = originalSpeed * runSpeed;
        }

        if (action == EHookAction::ChangeParamReturnDefault || action == EHookAction::ChangeParamReturnOverride || action == EHookAction::IgnoreParamReturnOverride)
        {
            FatalError("Not impl yet");
        }
    }
#else
    DeclareMemberDetourHook(GetPlayerMaxSpeed, float, (CCSPlayerPawn * pPawn))
    {
        const float originalSpeed = GetPlayerMaxSpeed(pPawn);

        ValidateCC();

        float      speed  = originalSpeed;
        const auto action = forwards::OnPlayerGetMaxSpeed->Invoke(pClient, pController, pPawn, originalSpeed, &speed);
        if (action == EHookAction::SkipCallReturnOverride)
        {
            return speed * GetPlayerRunSpeedValue(pController);
        }
        if (action == EHookAction::ChangeParamReturnDefault || action == EHookAction::ChangeParamReturnOverride || action == EHookAction::IgnoreParamReturnOverride)
        {
            FatalError("Not impl yet");
        }

        return originalSpeed * GetPlayerRunSpeedValue(pController);
    }
#endif
}

BeginMemberHookScope(CCSPlayer_WeaponServices)
{
    DeclareVirtualHook(SwitchWeapon, void, (CCSPlayer_WeaponServices * pService, CBaseWeapon * pWeapon))
    {
        SwitchWeapon(pService, pWeapon);

        const auto pPawn = pService->GetPawn<CCSPlayerPawn*>();
        if (const auto pController = pPawn->GetController<CCSPlayerController*>())
            forwards::OnPlayerSwitchWeapon->Invoke(sv->GetClient(pController->GetPlayerSlot()), pController, pPawn, pService, pWeapon);

#ifdef WEAPON_EVENT_ASSERT
        LOG("CCSPlayer_WeaponServices::SwitchWeapon %p", pWeapon);
#endif
    }

    DeclareMemberDetourHook(EquipWeapon, int64_t, (CCSPlayer_WeaponServices * pService, CBaseWeapon * pWeapon))
    {
        const auto ret   = EquipWeapon(pService, pWeapon);
        const auto pPawn = pService->GetPawn<CCSPlayerPawn*>();
        if (const auto pController = pPawn->GetController<CCSPlayerController*>())
            forwards::OnPlayerEquipWeapon->Invoke(sv->GetClient(pController->GetPlayerSlot()), pController, pPawn, pService, pWeapon, ret);

#ifdef WEAPON_EVENT_ASSERT
        LOG("CCSPlayer_WeaponServices::EquipWeapon %p", pWeapon);
#endif

        return ret;
    }

    DeclareVirtualHook(DropWeapon, void, (CCSPlayer_WeaponServices * pService, CBaseWeapon * pWeapon, Vector * pVecTarget, Vector * pVelocity))
    {
        // 提前中断, 与游戏原本行为一致
        if (!pWeapon)
        {
            return;
        }

        DropWeapon(pService, pWeapon, pVecTarget, pVelocity);

        const auto pPawn = pService->GetPawn<CCSPlayerPawn*>();
        if (const auto pController = pPawn->GetController<CCSPlayerController*>())
            forwards::OnPlayerDropWeapon->Invoke(sv->GetClient(pController->GetPlayerSlot()), pController, pPawn, pService, pWeapon);

#ifdef WEAPON_EVENT_ASSERT
        LOG("CCSPlayer_WeaponServices::DropWeapon %p", pWeapon);
#endif
    }

    DeclareVirtualHook(CanUse, bool, (CCSPlayer_WeaponServices * pService, CBaseWeapon * pWeapon))
    {
        // 提前中断, 与游戏原本行为一致
        if (!pWeapon)
        {
            return false;
        }

#ifdef WEAPON_EVENT_ASSERT
        LOG("CCSPlayer_WeaponServices::CanUse %p", pWeapon);
#endif

        const auto pPawn = pService->GetPawn<CCSPlayerPawn*>();

        if (const auto pController = pPawn->GetController<CCSPlayerController*>())
        {
            if (const auto pClient = sv->GetClient(pController->GetPlayerSlot()))
            {
                auto       result = true;
                const auto action = forwards::OnPlayerWeaponCanUse->Invoke(pClient, pController, pPawn, pWeapon, &result);
                if (action == EHookAction::SkipCallReturnOverride)
                {
                    return result;
                }

                if (action == EHookAction::IgnoreParamReturnOverride)
                {
                    CanUse(pService, pWeapon);
                    return result;
                }

                if (action == EHookAction::ChangeParamReturnDefault || action == EHookAction::ChangeParamReturnOverride)
                {
                    FatalError("Not impl yet");
                }
            }
        }

        return CanUse(pService, pWeapon);
    }

    DeclareMemberDetourHook(CanSwitch, bool, (CCSPlayer_WeaponServices * pService, CBaseWeapon * pWeapon))
    {
        // 提前中断, 与游戏原本行为一致
        if (!pWeapon)
        {
            return false;
        }

        const auto pPawn = pService->GetPawn<CCSPlayerPawn*>();

        if (const auto pController = pPawn->GetController<CCSPlayerController*>())
        {
            if (const auto pClient = sv->GetClient(pController->GetPlayerSlot()))
            {
                auto       result = true;
                const auto action = forwards::OnPlayerWeaponCanSwitch->Invoke(pClient, pController, pPawn, pWeapon, &result);
                if (action == EHookAction::SkipCallReturnOverride)
                {
                    return result;
                }

                if (action == EHookAction::ChangeParamReturnDefault || action == EHookAction::ChangeParamReturnOverride || action == EHookAction::IgnoreParamReturnOverride)
                {
                    FatalError("Not impl yet");
                }
            }
        }

        return CanSwitch(pService, pWeapon);
    }

    DeclareMemberDetourHook(CanEquip, bool, (CCSPlayer_WeaponServices * pService, CBaseWeapon * pWeapon))
    {
        const auto pPawn = pService->GetPawn<CCSPlayerPawn*>();

        if (const auto pController = pPawn->GetController<CCSPlayerController*>())
        {
            if (const auto pClient = sv->GetClient(pController->GetPlayerSlot()))
            {
                auto       result = true;
                const auto action = forwards::OnPlayerWeaponCanEquip->Invoke(pClient, pController, pPawn, pWeapon, &result);
                if (action == EHookAction::SkipCallReturnOverride)
                {
                    return result;
                }

                if (action == EHookAction::ChangeParamReturnDefault || action == EHookAction::ChangeParamReturnOverride || action == EHookAction::IgnoreParamReturnOverride)
                {
                    FatalError("Not impl yet");
                }
            }
        }

        return CanEquip(pService, pWeapon);
    }
}

void InstallPlayerHooks()
{
#ifdef PLATFORM_WINDOWS

    auto address = g_pGameData->GetAddress<void*>("CCSPlayer_MovementServices::ProcessMovement_AssignMaxSpeed");
    AssertPtr(address);

    auto result = safetyhook::MidHook::create(address, CCSPlayerPawn_Hooks::ProcessMovement_AssignMaxSpeed);
    if (!result.has_value())
    {
        FatalError("Failed to CCSPlayer_MovementServices::ProcessMovement_AssignMaxSpeed. ERROR: %s", g_szMidFuncHookErrors[result.error().type]);
    }

    CCSPlayerPawn_Hooks::hk_ProcessMovement_AssignMaxSpeed = std::move(*result);
    g_pHookManager->Register(&CCSPlayerPawn_Hooks::hk_ProcessMovement_AssignMaxSpeed);
#else
    InstallMemberDetourAutoSig(CCSPlayerPawn, GetPlayerMaxSpeed);
#endif

    InstallMemberDetourAutoSig(CCSPlayer_WeaponServices, EquipWeapon);
    InstallMemberDetourAutoSig(CCSPlayer_WeaponServices, CanSwitch);
    InstallMemberDetourAutoSig(CCSPlayer_WeaponServices, CanEquip);

    InstallVirtualHookAutoWithVTableAuto(CCSPlayer_WeaponServices, SwitchWeapon, server);
    InstallVirtualHookAutoWithVTableAuto(CCSPlayer_WeaponServices, DropWeapon, server);
    InstallVirtualHookAutoWithVTableAuto(CCSPlayer_WeaponServices, CanUse, server);

    InstallVirtualHookManualWithVTableAuto(CCSPlayerPawn, Event_Killed, server, "CBaseEntity::Event_Killed");
    InstallVirtualHookAutoWithVTableAuto(CCSPlayerPawn, PlayerSpawn, server);
    InstallVirtualHookAutoWithVTableAuto(CCSPlayerPawn, PlayerPreThink, server);
    InstallVirtualHookAutoWithVTableAuto(CCSPlayerPawn, PlayerPostThink, server);
}