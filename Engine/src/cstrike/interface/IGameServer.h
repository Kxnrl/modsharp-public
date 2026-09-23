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

#ifndef CSTRIKE_INTERFACE_GAMESERVER_H
#define CSTRIKE_INTERFACE_GAMESERVER_H

#include "cstrike/interface/IAppSystem.h"

/* IServerGameDLL */
class CSource2Server : IAppSystem
{
private:
    virtual void Unknown11() = 0;

public:
    virtual void SetGlobals(class CGlobalVars* pGlobals) = 0; // 12

private:
    virtual void Unknown13() = 0;
    virtual void Unknown14() = 0;

public:
    virtual void WorldUpdate(bool simulating) = 0; // 15

private:
    virtual void Unknown16() = 0;
    virtual void Unknown17() = 0;

public:
    virtual void ApplyGameSettings(class KeyValues* pKV)                   = 0; // 18
    virtual void GameFrame(bool simulating, bool firstTick, bool lastTick) = 0; // 19

private:
    virtual void Unknown20() = 0;
    virtual void Unknown21() = 0;
    virtual void Unknown22() = 0;
    virtual void Unknown23() = 0;
    virtual void Unknown24() = 0;
    virtual void Unknown25() = 0;
    virtual void Unknown26() = 0;
    virtual void Unknown27() = 0;
    virtual void Unknown28() = 0;
    virtual void Unknown29() = 0;
    virtual void Unknown30() = 0;

public:
    virtual bool IsPaused() = 0; // 31

private:
    virtual void Unknown32() = 0;
    virtual void Unknown33() = 0;
    virtual void Unknown34() = 0;
    virtual void Unknown35() = 0;
    virtual void Unknown36() = 0;
    virtual void Unknown37() = 0;
    virtual void Unknown38() = 0;
    virtual void Unknown39() = 0;
    virtual void Unknown40() = 0;

public:
    virtual void GameServerSteamAPIActivated()         = 0; // 41
    virtual void GameServerSteamAPIDeactivated()       = 0; // 42
    virtual void HostNameChanged(const char* hostname) = 0; // 43
    virtual void FatalShutdown() const                 = 0; // 44
    virtual void UpdateWhenNotInGame(float frameTime)  = 0; // 45

private:
    virtual void Unknown46() = 0;

    virtual void ConVarChanged(const char* name, const char* value) = 0; // 47
};

using IServerGameDLL = CSource2Server;

#endif
