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

#ifndef CSTRIKE_ENTITY_GAMEPLAYEREQUIP_H
#define CSTRIKE_ENTITY_GAMEPLAYEREQUIP_H

#include "cstrike/entity/CBaseEntity.h"

class CGamePlayerEquip : public CBaseEntity
{
public:
    DECLARE_SCHEMA_CLASS(CGamePlayerEquip)

    static constexpr int32_t SF_PLAYEREQUIP_USEONLY       = 0x0001;
    static constexpr int32_t SF_PLAYEREQUIP_STRIPFIRST    = 0x0002;
    static constexpr int32_t SF_PLAYEREQUIP_ONLYSTRIPSAME = 0x0004; // NOTE 这个Flags是爆改FGD拿的, 实现逻辑参考CSGO
};

#endif
