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

#ifndef CSTRIKE_INTERFACE_ENGINESERVER_H
#define CSTRIKE_INTERFACE_ENGINESERVER_H

#include "definitions.h"

// Source2EngineToServer001 (vtable CEngineServer). The interface version never changes while
// Valve keeps inserting slots, so nothing here goes through its vtable: each method calls what
// the engine wrapper forwards to.
class IEngineServer
{
public:
    void ChangeLevel(const char* map);
    bool IsDedicatedServer();

    const char* GetPlayerNetworkIDString(PlayerSlot_t slot);

    void ServerCommand(const char* command);
    void ClientCommand(PlayerSlot_t slot, const char* command);

    const char*      GetClientConVarValue(PlayerSlot_t slot, const char* name);
    const SteamId_t* GetClientSteamId(PlayerSlot_t slot);

    // Server 跟 disconnect时的提示有关系, 我也不知道取值
    void KickClient(PlayerSlot_t slot, const char* reason, int disconnectReason);
};

#endif
