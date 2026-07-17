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

#include "global.h"

#include "cstrike/interface/ICvar.h"

bool CVarCreationBase_t::IsFlagSet(int64_t flag) const
{
    return (this->m_nFlags & flag) != 0;
}

void CVarCreationBase_t::AddFlags(int64_t flags)
{
    this->m_nFlags |= flags;
}

void CVarCreationBase_t::RemoveFlags(int64_t flags)
{
    this->m_nFlags &= ~flags;
}

int64_t CVarCreationBase_t::GetFlags() const
{
    return m_nFlags;
}

CConVarBaseData* ICvar::FindConVarIterator(const char* name)
{
    for (uint16_t i = 0; i < 0xFFFF; i++)
    {
        const auto pConVar = GetConVar(i);
        if (pConVar == nullptr)
        {
            return nullptr;
        }

        if (strcasecmp(name, pConVar->GetName()) == 0)
            return pConVar;
    }

    return nullptr;
}

ConCommand* ICvar::FindConCommandIterator(const char* name)
{
    for (uint16_t i = 0; i < 0xFFFF; i++)
    {
        const auto pCommand = GetCommand(i);
        if (pCommand == nullptr)
        {
            return nullptr;
        }

        if (strcasecmp(name, pCommand->GetName()) == 0)
            return pCommand;
    }

    return nullptr;
}

int32_t DefaultCompletionFunc(const char*, void*)
{
    return 0;
}

void UnlockHiddenCommand(const char* command, bool release)
{
    const auto pCommand = icvar->FindConCommandIterator(command);
    if (pCommand && pCommand->GetFlags() & (FCVAR_HIDDEN | FCVAR_DEVELOPMENTONLY))
    {
        pCommand->RemoveFlags(FCVAR_HIDDEN | FCVAR_DEVELOPMENTONLY);
        if (release && !pCommand->IsFlagSet(FCVAR_RELEASE))
            pCommand->AddFlags(FCVAR_RELEASE);
    }
}
