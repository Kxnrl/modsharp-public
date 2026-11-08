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
#include "netmessage.h"
#include "vhook/call.h"

#include "cstrike/component/PlayerControllerComponent.h"
#include "cstrike/entity/PlayerController.h"
#include "cstrike/entity/PlayerPawn.h"
#include "cstrike/interface/CGameEntitySystem.h"
#include "cstrike/interface/IRecipientFilter.h"
#include "cstrike/interface/ISoundOpSystem.h"
#include "cstrike/type/CRecipientFilter.h"
#include "cstrike/type/EmitSound.h"

#include <proto/cstrike15_gcmessages.pb.h>

#include <cstdarg>

CBasePlayerController* CBasePlayerController::FindBySlot(PlayerSlot_t slot)
{
    if (slot >= CS_MAX_PLAYERS)
        return nullptr;

    return g_pGameEntitySystem->FindEntityByIndex<CBasePlayerController*>(slot + 1);
}

CBasePlayerPawn* CBasePlayerController::GetPawnInternal()
{
    return g_pGameEntitySystem->FindEntityByEHandle<CBasePlayerPawn*>(m_hPawn());
}

SteamId_t CBasePlayerController::GetSteamID()
{
    static auto offset  = GetFieldOffset("CBasePlayerController", "m_steamID");
    const auto  steamId = GetFieldValue<SteamId_t>(offset);
    if (IsFakeClient())
        return 0;

    return steamId;
}

bool CBasePlayerController::CanHearAndReadChatFrom(CBasePlayerController* pSenderController)
{
    DeclareVFuncIndex(CBasePlayerController, CanHearAndReadChatFrom, offset);
    return VCall_Manual(offset, bool, this, pSenderController);
}

void CBasePlayerController::SwitchTeam(CStrikeTeam_t team)
{
    address::server::CBasePlayerController_SwitchSteam(this, team);
}

// think hook 只认「调用前不是初值 (0, "")」的字段为插件所设, 空 tag 必须配一个非 0 clan id 才分得开
static constexpr uint32_t kClanTagSentinelClanId = 0xFFFFFFFF;

void CBasePlayerController::SetClanTag(const char* tag)
{
    const auto& clan = g_pGameEntitySystem->AllocPooledString(tag);
    m_szClan(clan);

    if (m_unClanId32bit() == 0 && *m_szClan().Get() == '\0')
        m_unClanId32bit(kClanTagSentinelClanId);
}

void CBasePlayerController::ClientPrint(HudPrint_t dest, const char* name, const char* param1, const char* param2, const char* param3, const char* param4)
{
    if (IsBot() || !IsConnected())
        return;

    UTIL_TextMsgSingle(GetPlayerSlot(), dest, name, param1, param2, param3, param4);
}

void CBasePlayerController::ClientPrintF(HudPrint_t dest, const char* message, ...)
{
    va_list args;
    va_start(args, message);

    char buffer[1024];
    vsnprintf(buffer, sizeof(buffer), message, args);
    va_end(args);

    ClientPrint(dest, buffer);
}

SndOpEventGuid_t CBasePlayerController::EmitSoundClient(const char* pszSound, const float* pVolume) const
{
    CSingleRecipientFilter filter(GetPlayerSlot());
    const auto             guid = g_pSoundOpGameSystem->StartSoundEvent(&filter, pszSound);

    if (pVolume != nullptr && guid.IsValid())
    {
        const auto         volume = *pVolume;
        CSosFieldDataFloat param(volume);
        g_pSoundOpGameSystem->SetSoundEventParam(&filter, guid, SE_PARAM_NAME_VOLUME, &param);
    }

    return guid;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

CCSPlayerPawn* CCSPlayerController::GetPlayerPawn()
{
    return g_pGameEntitySystem->FindEntityByEHandle<CCSPlayerPawn*>(m_hPlayerPawn());
}

CCSObserverPawn* CCSPlayerController::GetObserverPawn()
{
    return g_pGameEntitySystem->FindEntityByEHandle<CCSObserverPawn*>(m_hObserverPawn());
}

void CCSPlayerController::ForceChangeTeam(CStrikeTeam_t team)

{
    address::server::CBasePlayerController_CheckPawn(this);
    ChangeTeam(team);
}

void CCSPlayerController::CheckPawn()
{
    address::server::CBasePlayerController_CheckPawn(this);
}

void CCSPlayerController::SetPawn(CCSPlayerPawn* pawn, bool unk1, bool unk2, bool unk3, bool unk4)
{
    address::server::CBasePlayerController_SetPawn(this, pawn, unk1, unk2, unk3, unk4);
}

void CCSPlayerController::RoundRespawn()
{
    // 在CCSGameRules::RestartRound的复活中
    // 有一个Func是跟VPhysics有关的
    // 暂时不知道是干什么的

    if (this->m_hPlayerPawn().ToInt() != this->m_hPawn().ToInt())
    {
        if (const auto pawn = g_pGameEntitySystem->FindEntityByEHandle<CCSPlayerPawn*>(this->m_hPlayerPawn()))
            SetPawn(pawn, true, false, false, false);
    }

    static auto offset = g_pGameData->GetVFunctionIndex("CCSPlayerController::RoundRespawn");
    CALL_VIRTUAL(void, offset, this);
}

CEconItemView* CCSPlayerController::GetItemInLoadoutFromInventory(CStrikeTeam_t team, int32_t slot)
{
    const auto pService = m_pInventoryServices();
    AssertPtr(pService);
    return pService->GetItemInLoadout(team, slot);
}

bool CCSPlayerController::ResetClanTag()
{
    if (CCSPlayerController_InventoryServices::sm_nPersonaDataOffset == 0)
        return false;

    const CSOPersonaDataPublic* persona = nullptr;
    if (const auto pService = m_pInventoryServices(); pService != nullptr && !pService->TryGetPersonaDataPublic(persona))
        return false;

    const auto  clanId = persona != nullptr ? persona->clan_id() : 0;
    const auto& clan   = g_pGameEntitySystem->AllocPooledString(persona != nullptr ? persona->clan_tag().c_str() : "");

    if (m_unClanId32bit() != clanId)
        m_unClanId32bit(clanId);
    if (m_szClan() != clan)
        m_szClan(clan);

    return true;
}

void CCSPlayerController::ResetClanTagForSlot(PlayerSlot_t slot)
{
    if (g_pGameEntitySystem == nullptr)
        return;

    if (const auto pController = FindBySlot(slot))
        static_cast<CCSPlayerController*>(pController)->ResetClanTag();
}