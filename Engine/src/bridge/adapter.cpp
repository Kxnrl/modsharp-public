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

#include "bridge/adapter.h"
#include "bridge/natives/CVarNatives.h"
#include "bridge/natives/ClientNatives.h"
#include "bridge/natives/CoreNatives.h"
#include "bridge/natives/EconItemSchemaNative.h"
#include "bridge/natives/EntityNatives.h"
#include "bridge/natives/EventNatives.h"
#include "bridge/natives/GameNatives.h"
#include "bridge/natives/NetNatives.h"
#include "bridge/natives/PlayerNatives.h"
#include "bridge/natives/SchemaNatives.h"
#include "bridge/natives/SoundNatives.h"
#include "bridge/natives/TransmitManager.h"

#include "strtool.h"

#include "cstrike/type/CUtlVector.h"

static CUtlVector<NativeItem*>& GetBridgeNatives()
{
    static CUtlVector<NativeItem*> instance;
    return instance;
}

static CUtlVector<ForwardItem<void>*>& GetBridgeForwards()
{
    static CUtlVector<ForwardItem<void>*> instance;
    return instance;
}

namespace bridge
{
void CreateNative(const char* name, void* func)
{
    const auto item = new NativeItem();

    StrCopy(item->m_Name, sizeof(item->m_Name), name);
    item->m_Func = func;

    GetBridgeNatives().AddToTail(item);
}

ForwardItem<void>* CreateForward(const char* name)
{
    const auto item = new ForwardItem();

    StrCopy(item->m_Name, sizeof(item->m_Name), name);
    item->m_Func         = nullptr;
    item->m_InstallCount = 0;

    GetBridgeForwards().AddToTail(item);

    return item;
}

void* GetNatives()
{
    return &GetBridgeNatives();
}
void* GetForwards()
{
    return &GetBridgeForwards();
}

void InitNatives()
{
    natives::core::Init();
    natives::game::Init();
    natives::cvar::Init();
    natives::schemasystem::Init();
    natives::net::Init();
    natives::event::Init();
    natives::entity::Init();
    natives::client::Init();
    natives::player::Init();
    natives::transmit::Init();
    natives::econitemschema::Init();
    natives::sound::Init();
}

} // namespace bridge