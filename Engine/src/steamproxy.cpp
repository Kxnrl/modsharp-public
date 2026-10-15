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

#include "steamproxy.h"
#include "definitions.h"

#include "steamworks/isteamgameserver.h"
#include "steamworks/isteamugc.h"

bool ISteamApiProxy::IsAvailable()
{
    return m_pSteamServer && m_pSteamUGC;
}

void ISteamApiProxy::SetGameDescription(const char* description) const
{
    if (!m_pSteamServer)
        return;

    m_pSteamServer->SetGameDescription(description);
}

bool ISteamApiProxy::BLoggedOn() const
{
    if (!m_pSteamServer)
        return false;

    return m_pSteamServer->BLoggedOn();
}

bool ISteamApiProxy::BSecure() const
{
    return m_pSteamServer->BSecure();
}

uint64_t ISteamApiProxy::GetSteamId() const
{
    return static_cast<uint64_t>(m_pSteamServer->GetSteamID().ConvertToUint64());
}

bool ISteamApiProxy::WasRestartRequested() const
{
    return m_pSteamServer->WasRestartRequested();
}

void ISteamApiProxy::SetMaxPlayerCount(int count) const
{
    if (!m_pSteamServer)
        return;

    m_pSteamServer->SetMaxPlayerCount(count);
}

void ISteamApiProxy::SetBotPlayerCount(int count) const
{
    if (!m_pSteamServer)
        return;

    m_pSteamServer->SetBotPlayerCount(count);
}

void ISteamApiProxy::SetServerName(const char* name) const
{
    if (!m_pSteamServer)
        return;
    m_pSteamServer->SetServerName(name);
}

void ISteamApiProxy::SetMapName(const char* name) const
{
    if (!m_pSteamServer)
        return;

    m_pSteamServer->SetMapName(name);
}

void ISteamApiProxy::SetPasswordProtected(bool protect) const
{
    if (!m_pSteamServer)
        return;

    m_pSteamServer->SetPasswordProtected(protect);
}

void ISteamApiProxy::SetGameTags(const char* tags) const
{
    if (!m_pSteamServer)
        return;

    m_pSteamServer->SetGameTags(tags);
}

bool ISteamApiProxy::RequestUserGroupStatus(SteamId_t steamId, uint64_t groupId) const
{
    if (!m_pSteamServer)
        return false;

#if PLATFORM_WINDOWS
    return m_pSteamServer->RequestUserGroupStatus(steamId, groupId);
#else
    CSteamID pid;
    CSteamID gid;
    pid.SetFromUint64(static_cast<uint64>(steamId));
    gid.SetFromUint64(static_cast<uint64>(groupId));
    return m_pSteamServer->RequestUserGroupStatus(pid, gid);
#endif
}

void ISteamApiProxy::EndAuthSession(SteamId_t steamId) const
{
    if (!m_pSteamServer || !BLoggedOn())
        return;

#if PLATFORM_WINDOWS
    m_pSteamServer->EndAuthSession(steamId);
#else
    const auto u64 = static_cast<uint64>(steamId);
    CSteamID   id;
    id.SetFromUint64(u64);
    m_pSteamServer->EndAuthSession(id);
#endif
}

void ISteamApiProxy::BUpdateUserData(SteamId_t steamId, const char* playerName, uint32_t score) const
{
    if (!m_pSteamServer)
        return;

#if PLATFORM_WINDOWS
    m_pSteamServer->BUpdateUserData(steamId, playerName, score);
#else
    CSteamID pid;
    pid.SetFromUint64(static_cast<uint64>(steamId));
    m_pSteamServer->BUpdateUserData(pid, playerName, static_cast<uint32>(score));
#endif
}

uint32_t ISteamApiProxy::UserHasLicenseForApp(SteamId_t steamId, uint32_t appId) const
{
    if (!m_pSteamServer)
        return k_EUserHasLicenseResultNoAuth;

#if PLATFORM_WINDOWS
    return static_cast<uint32_t>(m_pSteamServer->UserHasLicenseForApp(steamId, appId));
#else
    CSteamID pid;
    pid.SetFromUint64(static_cast<uint64>(steamId));
    return static_cast<uint32_t>(m_pSteamServer->UserHasLicenseForApp(pid, static_cast<AppId_t>(appId)));
#endif
}

uint32_t ISteamApiProxy::GetItemState(uint64_t nPublishedFileId) const
{
    if (!m_pSteamUGC)
        return 0;

    return m_pSteamUGC->GetItemState(static_cast<PublishedFileId_t>(nPublishedFileId));
}

bool ISteamApiProxy::GetItemDownloadInfo(uint64_t nPublishedFileId, uint64_t* punBytesDownloaded, uint64_t* punBytesTotal)
{
    if (!m_pSteamUGC)
        return false;

    return m_pSteamUGC->GetItemDownloadInfo(static_cast<PublishedFileId_t>(nPublishedFileId), reinterpret_cast<uint64*>(punBytesDownloaded), reinterpret_cast<uint64*>(punBytesTotal));
}

bool ISteamApiProxy::DownloadItem(uint64_t nPublishedFileId, bool bHighPriority)
{
    if (!m_pSteamUGC)
        return false;

    return m_pSteamUGC->DownloadItem(static_cast<PublishedFileId_t>(nPublishedFileId), bHighPriority);
}

void ISteamApiProxy::SuspendDownloads(bool bSuspend)
{
    if (!m_pSteamUGC)
        return;

    return m_pSteamUGC->SuspendDownloads(bSuspend);
}

uint32_t ISteamApiProxy::GetPublicIP()
{
    if (!m_pSteamServer)
        return 0;

    return m_pSteamServer->GetPublicIP().m_unIPv4;
}