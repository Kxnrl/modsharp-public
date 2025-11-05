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

#ifndef CSTRIKE_INTERFACE_RECIPIENTFILTER_H
#define CSTRIKE_INTERFACE_RECIPIENTFILTER_H

#include "definitions.h"

#include "cstrike/type/CPlayerSlot.h"

#include <bit>

class IRecipientFilter
{
public:
    virtual ~IRecipientFilter() {}

    [[nodiscard]] virtual bool IsReliable() const    = 0;
    [[nodiscard]] virtual bool IsInitMessage() const = 0;

    [[nodiscard]] virtual NetworkReceiver_t* GetRecipientMask() const       = 0;
    [[nodiscard]] virtual CPlayerSlot        GetPredictedPlayerSlot() const = 0;

    [[nodiscard]] inline NetworkReceiver_t GetRecipients() const { return *GetRecipientMask(); }
    [[nodiscard]] inline int               GetRecipientCount() const { return std::popcount(*GetRecipientMask()); }
    [[nodiscard]] inline bool              IsPlayerSlotInFilter(PlayerSlot_t slot) const
    {
        if (slot >= CS_MAX_PLAYERS)
            return false;

        return (GetRecipients() & (BASE_RECEIVER_MAGIC << slot)) != 0;
    }

    inline void SetRecipients(NetworkReceiver_t mask) const { *GetRecipientMask() = mask; }
};

bool ParseNetworkReceivers(const struct RuntimeRecipientFilter* pFilter, int32_t* bits, NetworkReceiver_t* receivers);

#endif
