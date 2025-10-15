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
#include "bridge/forwards/forward.h"
#include "global.h"
#include "manager/HookManager.h"

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CTakeDamageInfo.h"

#include <safetyhook.hpp>

BeginMemberHookScope(CBaseEntity)
{
    DeclareMemberDetourHook(DispatchTraceAttack, int64_t, (CBaseEntity * pEntity, const CTakeDamageInfo* pInfo, void* a3))
    {
        if (CBaseEntity::InBypassHook())
            return DispatchTraceAttack(pEntity, pInfo, a3);

        CCSPlayerPawn*       pPlayer;
        CCSPlayerController* pController;
        CServerSideClient*   pClient;

        if (sv
            && pEntity->IsPlayerPawn()
            && (pPlayer = reinterpret_cast<CCSPlayerPawn*>(pEntity)) != nullptr
            && (pController = pPlayer->GetController<CCSPlayerController*>()) != nullptr
            && (pClient = sv->GetClient(pController->GetPlayerSlot())) != nullptr)
        {
            const auto action = forwards::OnPlayerDispatchTraceAttackPre->Invoke(pClient, pController, pPlayer, pInfo);
            if (action == EHookAction::SkipCallReturnOverride)
            {
                forwards::OnPlayerDispatchTraceAttackPost->Invoke(pClient, pController, pPlayer, pInfo, action, 0);
                return 0;
            }
            if (action != EHookAction::Ignored)
            {
                FatalError("Not impl yet");
            }
            const auto result = DispatchTraceAttack(pEntity, pInfo, a3);
            forwards::OnPlayerDispatchTraceAttackPost->Invoke(pClient, pController, pPlayer, pInfo, action, result);
            return result;
        }

        const auto action = forwards::OnEntityDispatchTraceAttackPre->Invoke(pEntity, pInfo);
        if (action == EHookAction::SkipCallReturnOverride)
        {
            forwards::OnEntityDispatchTraceAttackPost->Invoke(pEntity, pInfo, action, 0);
            return 0;
        }
        if (action != EHookAction::Ignored)
        {
            FatalError("Not impl yet");
        }
        const auto result = DispatchTraceAttack(pEntity, pInfo, a3);
        forwards::OnEntityDispatchTraceAttackPost->Invoke(pEntity, pInfo, action, result);
        return result;
    }
}

void InstallDamageManagerHooks()
{
    InstallMemberDetourManual(CBaseEntity, DispatchTraceAttack, address::server::CBaseEntity_DispatchTraceAttack);
}
