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

#include "cstrike/type/CCommand.h"

#include "gamedata.h"
#include "global.h"

CCommand::CCommand() :
    pad{}
{
    m_nArgv0Size = {};
    m_ArgSBuffer = {};
    m_ArgvBuffer = {};
    m_Args       = {};
}

bool CCommand::Tokenize(const char* pCommand, void* pBreakSet)
{
    using fn         = bool (*)(CCommand*, const char*, void*);
    static auto func = g_pGameData->GetAddress<fn>("CCommand::Tokenize");

    return func(this, pCommand, pBreakSet);
}
