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

#include "gamedata.h"
#include "global.h"

#include "cstrike/entity/PlayerController.h"
#include "cstrike/interface/IServerGameClient.h"
#include "cstrike/schema.h"

void IServerGameClient::ClientCommand(PlayerSlot_t slot, const CCommand& command)
{
    // CSource2GameClients::Command itself; calling it still runs our Command detour like the vcall did
    using fn         = void (*)(IServerGameClient*, int32_t, const CCommand*);
    static auto func = g_pGameData->GetAddress<fn>("CSource2GameClients::Command");

    func(this, slot, &command);
}

int IServerGameClient::GetPlayerScore(PlayerSlot_t slot)
{
    // the engine one: controller->m_pActionTrackingServices->m_matchStats.m_iKills, 0 without a controller
    const auto pController = CBasePlayerController::FindBySlot(slot);
    if (pController == nullptr)
        return 0;

    static auto services = schemas::GetOffset("CCSPlayerController", "m_pActionTrackingServices").offset;
    static auto stats    = schemas::GetOffset("CCSPlayerController_ActionTrackingServices", "m_matchStats").offset;
    static auto kills    = schemas::GetOffset("CSPerRoundStats_t", "m_iKills").offset;

    const auto pServices = *reinterpret_cast<uint8_t**>(reinterpret_cast<uintptr_t>(pController) + services);
    return pServices != nullptr ? *reinterpret_cast<int32_t*>(pServices + stats + kills) : 0;
}
