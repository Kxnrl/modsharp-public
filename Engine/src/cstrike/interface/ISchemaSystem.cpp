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

#include "cstrike/interface/ISchemaSystem.h"

#include "gamedata.h"
#include "vhook/call.h"

// CSchemaSystemTypeScope
SchemaClassInfoData_t* CSchemaSystemTypeScope::FindDeclaredClass(const char* pClass)
{
#ifdef PLATFORM_WINDOWS
    SchemaClassInfoData_t* rv = nullptr;
    CALL_VIRTUAL(void, gamedata::offset::schemasystem::FindDeclaredClass, this, &rv, pClass);
    return rv;
#else
    return CALL_VIRTUAL(SchemaClassInfoData_t*, gamedata::offset::schemasystem::FindDeclaredClass, this, pClass);
#endif
}
CSchemaSystemTypeScope* ISchemaSystem::GetGlobalTypeScope()
{
    return CALL_VIRTUAL(CSchemaSystemTypeScope*, gamedata::offset::schemasystem::GlobalType, this);
}

// ISchemaSystem

CSchemaSystemTypeScope* ISchemaSystem::FindTypeScopeForModule(const char* module)
{
    return CALL_VIRTUAL(CSchemaSystemTypeScope*, gamedata::offset::schemasystem::FindTypeScopeForModule, this, module, nullptr);
}
