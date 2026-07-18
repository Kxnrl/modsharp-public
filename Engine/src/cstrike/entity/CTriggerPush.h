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

#ifndef CSTRIKE_ENTITY_TRIGGERPUSH_H
#define CSTRIKE_ENTITY_TRIGGERPUSH_H

#include "cstrike/entity/CBaseTrigger.h"

class CTriggerPush : public CBaseTrigger
{
public:
    DECLARE_SCHEMA_CLASS(CTriggerPush)

    SCHEMA_FIELD(Vector, m_vecPushDirEntitySpace)
    SCHEMA_FIELD(bool, m_bTriggerOnStartTouch)
    SCHEMA_FIELD(float, m_flSpeed)

    bool PassesTriggerFilters(CBaseEntity* pOther);
};

#endif