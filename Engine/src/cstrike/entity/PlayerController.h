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

#ifndef CSTRIKE_ENTITY_PLAYERCONTROLLER_H
#define CSTRIKE_ENTITY_PLAYERCONTROLLER_H

#include "definitions.h"
#include "logging.h"

#include "cstrike/entity/CBaseEntity.h"

class CBasePlayerPawn;
class CBasePlayerController;
class CCSPlayerPawn;
class CCSObserverPawn;

class CBasePlayerController : public CBaseEntity
{
    enum class PlayerConnectedState : uint32_t
    {
        PlayerNeverConnected = 0xffffffff,
        PlayerConnected      = 0x0,
        PlayerConnecting     = 0x1,
        PlayerReconnecting   = 0x2,
        PlayerDisconnecting  = 0x3,
        PlayerDisconnected   = 0x4,
        PlayerReserved       = 0x5,
    };

public:
    DECLARE_SCHEMA_CLASS(CBasePlayerController)

    SCHEMA_FIELD(CBaseHandle, m_hPawn)
    SCHEMA_FIELD(PlayerConnectedState, m_iConnected)
    SCHEMA_FIELD(bool, m_bLagCompensation)

    SCHEMA_FIELD_MANUAL(CCSPlayerController, m_szClan, CUtlSymbolLarge)
    SCHEMA_FIELD_MANUAL(CCSPlayerController, m_unClanId32bit, uint32_t)

private:
    [[nodiscard]] CBasePlayerPawn* GetPawnInternal();

public:
    template <typename T>
    [[nodiscard]] T GetPawn()
    {
        return reinterpret_cast<T>(GetPawnInternal());
    }

    [[nodiscard]] SteamId_t   GetSteamID();
    [[nodiscard]] const char* GetPlayerName() const
    {
        static auto offset = GetFieldOffset("CBasePlayerController", "m_iszPlayerName");
        return GetFieldPointer<const char*>(offset);
    }
    [[nodiscard]] const char* GetNetworkIDString() const
    {
        static auto offset = GetFieldOffset("CBasePlayerController", "m_szNetworkIDString");
        return GetFieldValue<const char*>(offset);
    }

    [[nodiscard]] PlayerSlot_t GetPlayerSlot() const
    {
        const auto index = GetEntityIndex();
        AssertBool(index > 0);
        return static_cast<PlayerSlot_t>(index - 1);
    }

    [[nodiscard]] bool IsBot() { return (GetFlags() & 0x100) != 0; }
    [[nodiscard]] bool IsFakeClient() { return (GetFlags() & 0x100) != 0; }
    [[nodiscard]] bool CanHearAndReadChatFrom(CBasePlayerController* pSenderController);
    [[nodiscard]] bool IsConnected() { return m_iConnected() == PlayerConnectedState::PlayerConnected; }

    void SwitchTeam(CStrikeTeam_t team);
    void SetClanTag(const char* tag);
    void ResetClanTag();
    void ClientPrint(HudPrint_t dest, const char* name, const char* param1 = nullptr, const char* param2 = nullptr, const char* param3 = nullptr, const char* param4 = nullptr);
    void ClientPrintF(HudPrint_t dest, const char* message, ...);

    SndOpEventGuid_t EmitSoundClient(const char* pszSound, const float* pVolume = nullptr) const;

    void TransientChangeLagCompensationState(bool state) const
    {
        static auto offset = GetFieldOffset("CBasePlayerController", "m_bLagCompensation");
        SetFieldValue(offset, state);
    }

    [[nodiscard]] static CBasePlayerController* FindBySlot(PlayerSlot_t slot);

    static void EnforceClanTags();
    static void ClearClanTagState(PlayerSlot_t slot);
};

class CCSPlayerController_InventoryServices;
class CCSPlayerController : public CBasePlayerController
{
public:
    DECLARE_SCHEMA_CLASS(CCSPlayerController)

    SCHEMA_FIELD(bool, m_bPawnIsAlive)

    SCHEMA_FIELD(CCSPlayerController_InventoryServices*, m_pInventoryServices)

    SCHEMA_FIELD(CBaseHandle, m_hPlayerPawn)
    SCHEMA_FIELD(CBaseHandle, m_hObserverPawn)

    SCHEMA_FIELD(ObserverMode_t, m_DesiredObserverMode)

    SCHEMA_FIELD(uint16_t, m_nPawnCharacterDefIndex)

    [[nodiscard]] CCSPlayerPawn*   GetPlayerPawn();
    [[nodiscard]] CCSObserverPawn* GetObserverPawn();
    [[nodiscard]] CEconItemView*   GetItemInLoadoutFromInventory(CStrikeTeam_t team, int32_t slot);

    void ForceChangeTeam(CStrikeTeam_t team);
    void CheckPawn();
    void SetPawn(CCSPlayerPawn* pawn, bool unk1, bool unk2, bool unk3, bool unk4);
    void RoundRespawn();
};

#endif