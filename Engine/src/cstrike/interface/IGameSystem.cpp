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
#include "vhook/call.h"

#include "cstrike/interface/IGameSystem.h"

#include <cstring>

CBaseGameSystemFactory* GetGameSystemFactory()
{
    return CBaseGameSystemFactory::GetFirst();
}

static void* GetFactoryInstance(CBaseGameSystemFactory* factory)
{
    DeclareVFuncIndex(IGameSystemFactory, IsReallocating, offset);

    if (!VCall_Manual(offset, bool, factory))
        return factory->m_pInstance;

    const auto ppInstance = reinterpret_cast<void**>(factory->m_pInstance);
    return ppInstance ? *ppInstance : nullptr;
}

void* FindGameSystemByName(const char* name)
{
    auto list = CBaseGameSystemFactory::GetFirst();

    while (list)
    {
        if (strcmp(list->m_pszName, name) == 0)
        {
            return GetFactoryInstance(list);
        }

        list = list->m_pNext;
    }

    return nullptr;
}
