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
#include "cstrike/type/CTakeDamageResult.h"

#include <safetyhook.hpp>

BeginMemberHookScope(CBaseEntity)
{
    DeclareMemberDetourHook(DispatchTraceAttack, int64_t, (CBaseEntity * pEntity, CTakeDamageInfo * pInfo, CTakeDamageResult * pResult))
    {
        if (CBaseEntity::InBypassHook())
            return DispatchTraceAttack(pEntity, pInfo, pResult);

        // maybe call in flow
        // IDA::2025/10/30 server.i64
        CTakeDamageResult damageResult(0);

        if (pResult == nullptr)
        {
            damageResult.CopyFrom(pInfo);
            pResult = &damageResult;
        }

        CCSPlayerPawn*       pPlayer;
        CCSPlayerController* pController;
        CServerSideClient*   pClient;

        if (sv
            && pEntity->IsPlayerPawn()
            && (pPlayer = reinterpret_cast<CCSPlayerPawn*>(pEntity)) != nullptr
            && (pController = pPlayer->GetController<CCSPlayerController*>()) != nullptr
            && (pClient = sv->GetClient(pController->GetPlayerSlot())) != nullptr)
        {
            const auto action = forwards::OnPlayerDispatchTraceAttackPre->Invoke(pClient, pController, pPlayer, pInfo, pResult);
            if (action == EHookAction::SkipCallReturnOverride)
            {
                pResult->m_bWasDamageSuppressed = true;
                forwards::OnPlayerDispatchTraceAttackPost->Invoke(pClient, pController, pPlayer, pInfo, pResult, action);
                return 0;
            }
            if (action != EHookAction::Ignored)
            {
                FatalError("Not impl yet");
            }
            const auto result = DispatchTraceAttack(pEntity, pInfo, pResult);
            forwards::OnPlayerDispatchTraceAttackPost->Invoke(pClient, pController, pPlayer, pInfo, pResult, action);
            return result;
        }

        const auto action = forwards::OnEntityDispatchTraceAttackPre->Invoke(pEntity, pInfo, pResult);
        if (action == EHookAction::SkipCallReturnOverride)
        {
            pResult->m_bWasDamageSuppressed = true;
            forwards::OnEntityDispatchTraceAttackPost->Invoke(pEntity, pInfo, pResult, action);
            return 0;
        }
        if (action != EHookAction::Ignored)
        {
            FatalError("Not impl yet");
        }
        const auto result = DispatchTraceAttack(pEntity, pInfo, pResult);
        forwards::OnEntityDispatchTraceAttackPost->Invoke(pEntity, pInfo, pResult, action);
        return result;
    }
}

void InstallDamageManagerHooks()
{
    InstallMemberDetourManual(CBaseEntity, DispatchTraceAttack, address::server::CBaseEntity_DispatchTraceAttack);
}