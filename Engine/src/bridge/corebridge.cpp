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

#include "bridge/corebridge.h"

#include "global.h"

static CoreBridge s_bridge;
CoreBridge*       g_pCoreBridge = &s_bridge;

void InitCoreBridge()
{
    g_pCoreBridge->FullFileSystem           = g_pFullFileSystem;
    g_pCoreBridge->SteamApi                 = g_pSteamApiProxy;
    g_pCoreBridge->KeyValuesHelper          = g_pKeyValuesHelper;
    g_pCoreBridge->NetworkStringTableHelper = g_pNetworkStringTableHelper;
    g_pCoreBridge->KeyValues3Helper         = g_pKeyValues3Helper;
    g_pCoreBridge->MemAlloc                 = g_pMemAlloc;
    g_pCoreBridge->EngineServer             = engine;
}
