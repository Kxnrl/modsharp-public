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
#include "gamefactory.h"
#include "global.h"
#include "logging.h"
#include "module.h"

#include "cstrike/interfaces.h"

void InitializeInterfaces()
{
    engine                       = factory::engine->GetInterface<IEngineServer*>("Source2EngineToServer001");
    server                       = factory::server->GetInterface<CSource2Server*>("Source2Server001");
    gameClients                  = factory::server->GetInterface<IServerGameClient*>("Source2GameClients001");
    gameEntities                 = factory::server->GetInterface<IServerGameEntities*>("Source2GameEntities001");
    icvar                        = factory::engine->GetInterface<ICvar*>("VEngineCvar007");
    schemaSystem                 = modules::schemas->FindInterface("SchemaSystem_001").As<ISchemaSystem*>();
    g_pFullFileSystem            = factory::engine->GetInterface<IFileSystem*>("VFileSystem017");
    g_pGameResourceServiceServer = factory::engine->GetInterface<IGameResourceServiceServer*>("GameResourceServiceServerV001");
    g_pNetworkServerService      = factory::engine->GetInterface<INetworkServerService*>("NetworkServerService_001");
    g_pGameEventSystem           = factory::engine->GetInterface<IGameEventSystem*>("GameEventSystemServerV001");
    g_pNetworkMessages           = factory::engine->GetInterface<INetworkMessages*>("NetworkMessagesVersion001");
    g_pNetworkSystem             = factory::engine->GetInterface<INetworkSystem*>("NetworkSystemVersion001");

    g_pGameTypes            = factory::engine->GetInterface<IGameTypes*>("GameTypes001");
    g_pStringTableContainer = factory::engine->GetInterface<INetworkStringTableContainer*>("Source2EngineToServerStringTable001");
    g_pResourceSystem       = modules::resource->FindInterface("ResourceSystem013").As<IResourceSystem*>();

    AssertPtr(engine);
    AssertPtr(server);
    AssertPtr(gameClients);
    AssertPtr(gameEntities);
    AssertPtr(icvar);
    AssertPtr(schemaSystem);
    AssertPtr(g_pFullFileSystem);
    AssertPtr(g_pGameResourceServiceServer);
    AssertPtr(g_pNetworkServerService);
    AssertPtr(g_pGameEventSystem);
    AssertPtr(g_pNetworkMessages);
    AssertPtr(g_pNetworkSystem);
    AssertPtr(g_pGameTypes);
    AssertPtr(g_pStringTableContainer);
    AssertPtr(g_pResourceSystem);

    g_pCVar = icvar;
}
