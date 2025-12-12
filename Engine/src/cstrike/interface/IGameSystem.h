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

#ifndef CSTRIKE_INTERFACE_GAMESYSTEM_H
#define CSTRIKE_INTERFACE_GAMESYSTEM_H

class IGameSystem
{
public:
    virtual bool Init()     = 0;
    virtual void PostInit() = 0;
    virtual void Shutdown() = 0;

    virtual void GameInit(const void* msg)               = 0; // 3
    virtual void GameShutdown(const void* msg)           = 0; // 4
    virtual void GamePostInit(const void* msg)           = 0; // 5
    virtual void GamePreShutdown(const void* msg)        = 0; // 6
    virtual void OnPrecacheResource(const void* context) = 0; // 7
    virtual void GameActivate(const void* msg)           = 0; // 8

private:
    virtual void Method_09() = 0;
    virtual void Method_10() = 0;

public:
    virtual void GameDeactivate(const void* msg) = 0; // 11
};

#define GAMESYSTEMNAME_GAMERULES "GameRulesGameSystem"

class CGameRulesGameSystem : public IGameSystem
{
};

class CBaseGameSystemFactory
{
    virtual ~CBaseGameSystemFactory() = 0;

public:
    static CBaseGameSystemFactory** sm_ppFirst;

    static CBaseGameSystemFactory* GetFirst()
    {
        return *sm_ppFirst;
    }

public:
    CBaseGameSystemFactory* m_pNext;
    const char*             m_pszName;
    IGameSystem*            m_pInstance;
};

#endif