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

#include "bridge/forwards/forward.h"
#include "global.h"
#include "hook/installer.h"

#include "cstrike/entity/PlayerController.h"
#include "cstrike/type/CNetworkGameServer.h"

#include <safetyhook.hpp>

BeginMemberHookScope(CBasePlayerController)
{
    DeclareMemberDetourHook(HandleCommandJoinTeam, bool, (CCSPlayerController * pController, int32_t team, bool queue))
    {
        const auto pClient = sv->GetClient(pController->GetPlayerSlot());

        auto refTeam   = team;
        auto refQueue  = queue;
        auto refCoach  = 0;
        auto refReturn = false;

        const auto action = forwards::OnHandleCommandJoinTeamPre->Invoke(pClient, pController, &refTeam, &refQueue, &refCoach, &refReturn);

        if (action == EHookAction::Ignored)
        {
            goto postHook;
        }

        if (action == EHookAction::SkipCallReturnOverride)
        {
            return refReturn;
        }

        if (action == EHookAction::ChangeParamReturnDefault || action == EHookAction::ChangeParamReturnOverride)
        {
            team  = refTeam;
            queue = refQueue;
        }

    postHook:

        auto ret = HandleCommandJoinTeam(pController, team, queue);

        if (action == EHookAction::ChangeParamReturnOverride)
        {
            ret = refReturn;
        }

        forwards::OnHandleCommandJoinTeamPost->Invoke(pClient, pController, team, queue, refCoach, action, ret);

        return ret;
    }
}

void InstallHandleCommandJoinTeamHooks()
{
    HOOK(CBasePlayerController, HandleCommandJoinTeam);
}