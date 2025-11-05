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

#ifndef CSTRIKE_TYPE_RECIPIENTFILTER_H
#define CSTRIKE_TYPE_RECIPIENTFILTER_H

#include "CoreCLR/NativeSpan.h"
#include "CoreCLR/RuntimeRecipientFilter.h"

#include "cstrike/interface/IRecipientFilter.h"

class CSingleRecipientFilter : public IRecipientFilter
{
public:
    CSingleRecipientFilter() = delete;
    // 这里用IGameClient Index
    CSingleRecipientFilter(PlayerSlot_t iRecipient, bool bReliable = true, bool bInitMessage = false)
    {
        m_nRecipients                       = 0;
        m_slotPlayerExcludedDueToPrediction = -1;
        m_bReliable                         = bReliable;
        m_bInitMessage                      = bInitMessage;

        AddRecipient(iRecipient);
    }

    ~CSingleRecipientFilter() override {}

    [[nodiscard]] bool IsReliable() const override { return m_bReliable; }

    [[nodiscard]] bool IsInitMessage() const override { return m_bInitMessage; }

    [[nodiscard]] NetworkReceiver_t* GetRecipientMask() const override
    {
        return const_cast<NetworkReceiver_t*>(&m_nRecipients);
    }

    [[nodiscard]] CPlayerSlot GetPredictedPlayerSlot() const override
    {
        return m_slotPlayerExcludedDueToPrediction;
    }

    void AddRecipient(PlayerSlot_t slot)
    {
        if (slot >= CS_MAX_PLAYERS)
            return;

        m_nRecipients |= BASE_RECEIVER_MAGIC << slot;
    }

    void RemoveRecipient(PlayerSlot_t slot)
    {
        if (slot >= CS_MAX_PLAYERS)
            return;

        m_nRecipients &= ~(BASE_RECEIVER_MAGIC << slot);
    }

protected:
    NetworkReceiver_t m_nRecipients{};
    int32_t           m_slotPlayerExcludedDueToPrediction{};
    bool              m_bReliable{};
    uint8_t           m_bInitMessage{};
    bool              m_bDoNotSuppressPrediction = false;
};

class CBroadcastRecipientFilter : public IRecipientFilter
{
public:
    CBroadcastRecipientFilter()
    {
        m_nRecipients                       = 0;
        m_slotPlayerExcludedDueToPrediction = -1;
        m_bReliable                         = true;
        m_bInitMessage                      = true;
    }

    CBroadcastRecipientFilter(bool bReliable, bool bInitMessage = false)
    {
        m_nRecipients                       = 0;
        m_slotPlayerExcludedDueToPrediction = -1;
        m_bReliable                         = bReliable;
        m_bInitMessage                      = bInitMessage;
    }

    CBroadcastRecipientFilter(NetworkReceiver_t receivers, bool bReliable = true, bool bInitMessage = false)
    {
        if (receivers == 0)
        {
            AddAllPlayer();
        }
        else
        {
            AddPlayers(receivers);
        }

        m_slotPlayerExcludedDueToPrediction = -1;
        m_bReliable                         = bReliable;
        m_bInitMessage                      = bInitMessage;
    }

    CBroadcastRecipientFilter(const RuntimeRecipientFilter* filter, bool bReliable = true, bool bInitMessage = false)
    {
        switch (filter->Type)
        {
        case RuntimeRecipientFilterType::All:
            AddAllPlayer();
            break;
        case RuntimeRecipientFilterType::Team:
            AddTeamPlayer(filter->Team);
            break;
        case RuntimeRecipientFilterType::Players:
            AddPlayers(filter->Receivers);
            break;
        case RuntimeRecipientFilterType::Single:
            if (filter->Receivers < CS_MAX_PLAYERS)
                AddRecipient(static_cast<PlayerSlot_t>(filter->Receivers));
            break;
        }

        m_slotPlayerExcludedDueToPrediction = -1;
        m_bReliable                         = bReliable;
        m_bInitMessage                      = bInitMessage;
    }

    ~CBroadcastRecipientFilter() override {}

    [[nodiscard]] bool IsReliable() const override { return m_bReliable; }

    [[nodiscard]] bool IsInitMessage() const override { return m_bInitMessage; }

    [[nodiscard]] NetworkReceiver_t* GetRecipientMask() const override
    {
        return const_cast<NetworkReceiver_t*>(&m_nRecipients);
    }

    [[nodiscard]] CPlayerSlot GetPredictedPlayerSlot() const override
    {
        return m_slotPlayerExcludedDueToPrediction;
    }

    void AddRecipient(PlayerSlot_t slot)
    {
        if (slot >= CS_MAX_PLAYERS)
            return;

        m_nRecipients |= BASE_RECEIVER_MAGIC << slot;
    }

    void RemoveRecipient(PlayerSlot_t slot)
    {
        if (slot >= CS_MAX_PLAYERS)
            return;

        m_nRecipients &= ~(BASE_RECEIVER_MAGIC << slot);
    }

    void AddAllPlayer();

    void AddTeamPlayer(const CStrikeTeam_t team);

    void AddPlayers(const NetworkReceiver_t receivers)
    {
        m_nRecipients = receivers;
    }

    void AddPlayers(const NativeFixedSpan<PlayerSlot_t>& span)
    {
        for (auto i = 0; i < span.m_nCount; i++)
            AddRecipient(span.m_pData[i]);
    }

    void AddPlayers(const IRecipientFilter& pFilter)
    {
        const auto masks = pFilter.GetRecipients();

        m_nRecipients = masks;
    }

protected:
    NetworkReceiver_t m_nRecipients{};
    int32_t           m_slotPlayerExcludedDueToPrediction{};
    bool              m_bReliable{};
    uint8_t           m_bInitMessage{};
    bool              m_bDoNotSuppressPrediction = false;
};

#endif