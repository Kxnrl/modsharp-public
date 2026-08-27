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

#include "cstrike/type/CNetworkUtlVectorBase.h"

#include "cstrike/schema.h"

void NetworkVectorBaseStateChanged(void* pInstance, const SchemaFieldData* pData, int32_t nOffset, bool isStruct, uint32_t nArrayIndex)
{
    if (!pData->key.networked)
    {
        return;
    }

    if (pData->offset != 0)
    {
        NetworkStateChanged(reinterpret_cast<uintptr_t>(pInstance) + pData->offset,
                            static_cast<uint32_t>(nOffset),
                            nArrayIndex);
    }
    else if (!isStruct)
    {
        SetStateChanged(static_cast<CBaseEntity*>(pInstance), static_cast<uint32_t>(nOffset), nArrayIndex);
    }
}
