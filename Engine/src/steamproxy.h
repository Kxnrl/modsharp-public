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

#ifndef MS_ROOT_STEAMPROXY_H
#define MS_ROOT_STEAMPROXY_H

#include "definitions.h"

#include <cstdint>

class ISteamGameServer;
class ISteamUGC;

class ISteamApiProxy
{
public:
    virtual ~ISteamApiProxy() = default;

    ISteamApiProxy() :
        m_pSteamServer(nullptr), m_pSteamUGC(nullptr)
    {
    }

    void SetSteamServer(ISteamGameServer* pServer)
    {
        m_pSteamServer = pServer;
    }

    void SetSteamUGC(ISteamUGC* pUGC)
    {
        m_pSteamUGC = pUGC;
    }

    [[nodiscard]] virtual bool     IsAvailable();
    virtual void                   SetGameDescription(const char* description) const;
    [[nodiscard]] virtual bool     BLoggedOn() const;
    [[nodiscard]] virtual bool     BSecure() const;
    [[nodiscard]] virtual uint64_t GetSteamId() const;
    [[nodiscard]] virtual bool     WasRestartRequested() const;
    virtual void                   SetMaxPlayerCount(int count) const;
    virtual void                   SetBotPlayerCount(int count) const;
    virtual void                   SetServerName(const char* name) const;
    virtual void                   SetMapName(const char* name) const;
    virtual void                   SetPasswordProtected(bool protect) const;
    virtual void                   SetGameTags(const char* tags) const;
    [[nodiscard]] virtual bool     RequestUserGroupStatus(SteamId_t steamId, uint64_t groupId) const;
    virtual void                   EndAuthSession(SteamId_t steamId) const;
    virtual void                   BUpdateUserData(SteamId_t steamId, const char* playerName, uint32_t score) const;
    [[nodiscard]] virtual uint32_t UserHasLicenseForApp(SteamId_t steamId, uint32_t appId) const;
    [[nodiscard]] virtual uint32_t GetItemState(uint64_t nPublishedFileId) const;
    virtual bool                   GetItemDownloadInfo(uint64_t nPublishedFileId, uint64_t* punBytesDownloaded, uint64_t* punBytesTotal);
    virtual bool                   DownloadItem(uint64_t nPublishedFileId, bool bHighPriority);
    virtual bool                   DeleteItem(uint64_t nPublishedFileId);
    virtual void                   SuspendDownloads(bool bSuspend);
    virtual uint32_t               GetPublicIP();
    virtual void*                  GetSteamGameServer();
    virtual void*                  GetSteamUGC();

private:
    ISteamGameServer* m_pSteamServer;
    ISteamUGC*        m_pSteamUGC;
};

#endif