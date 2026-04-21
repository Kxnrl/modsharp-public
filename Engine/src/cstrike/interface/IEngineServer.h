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

#ifndef CSTRIKE_INTERFACE_ENGINESERVER_H
#define CSTRIKE_INTERFACE_ENGINESERVER_H

#include "definitions.h"

#include "cstrike/interface/IAppSystem.h"

#include <cstdint>

class CGlobalVars;

class IEngineServer : public IAppSystem
{
public:
    virtual bool  IsPaused()     = 0; // 11
    virtual float GetTimeScale() = 0; // 12

private:
    virtual void* Unknown13() = 0; // 13
    virtual void* Unknown14() = 0; // 14

public:
    virtual uint32_t GetStatsAppID() = 0; // 15

private:
    virtual void* Unknown16() = 0; // 16
    virtual void* Unknown17() = 0; // 17

public:
    virtual uint64_t GetSteamUniverse() = 0; // 18

private:
    virtual void Unknown19() = 0; // 19
    virtual void Unknown20() = 0; // 20
    virtual void Unknown21() = 0; // 21
    virtual void Unknown22() = 0; // 22
    virtual void Unknown23() = 0; // 23
    virtual void Unknown24() = 0; // 24
    virtual void Unknown25() = 0; // 25
    virtual void Unknown26() = 0; // 26
    virtual void Unknown27() = 0; // 27
    virtual void Unknown28() = 0; // 28
    virtual void Unknown29() = 0; // 29

    virtual void Unknown30() = 0; // 30
    virtual void Unknown31() = 0; // 31

public:
    virtual void ChangeLevel(const char* map, const char* options = nullptr) = 0; // 32
    virtual int  IsMapValid(const char* filename)                            = 0; // 33
    virtual bool IsDedicatedServer()                                         = 0; // 34
    virtual bool IsHltvRelay()                                               = 0; // 35
    virtual bool IsServerLocalOnly()                                         = 0; // 36

public:
    virtual int  PrecacheGeneric(const char* name, bool preload = false) = 0; // 37
    virtual bool IsGenericPrecached(const char* name) const              = 0; // 38

    virtual int         GetPlayerUserId(PlayerSlot_t slot)          = 0; // 39
    virtual const char* GetPlayerNetworkIDString(PlayerSlot_t slot) = 0; // 40
    virtual void*       GetPlayerNetInfo(PlayerSlot_t slot)         = 0; // 41

private:
    virtual void Unknown42() = 0; // 42
    virtual void Unknown43() = 0; // 43
    virtual void Unknown44() = 0; // 44

public:
    /* 这里直接调用 g_InputServices + 200 (4, ...) */
    virtual void ServerCommand(const char* command)                    = 0; // 45
    virtual void ClientCommand(PlayerSlot_t slot, const char* command) = 0; // 46
    virtual void LightStyle(int style, const char* val)                = 0; // 47
    virtual void ClientPrint(PlayerSlot_t slot, const char* message)   = 0; // 48

private:
    virtual void Unknown49() = 0; // 49
    virtual void Unknown50() = 0; // 50
    virtual void Unknown51() = 0; // 51

public:
    virtual const char* GetGameDir(void* buffer) = 0; // 52

    virtual PlayerSlot_t CreateFakeClient(const char* name) = 0; // 53

    virtual const char* GetClientConVarValue(PlayerSlot_t slot, const char* name) = 0; // 54

    virtual void LogPrint(const char* msg) = 0; // 55
    virtual bool IsLogEnabled()            = 0; // 56

private:
    virtual void IsSplitScreenPlayer()               = 0; // 57
    virtual void GetSplitScreenPlayerAttachToEdict() = 0; // 58
    virtual void GetSplitScreenPlayerForEdict()      = 0; // 59
    virtual void UnloadSpawnGroup()                  = 0; // 60

public:
    virtual void LoadSpawnGroup(void* sgd) = 0; // 61

private:
    virtual void Unknown62() = 0; // 62
    virtual void Unknown63() = 0; // 63
    virtual void Unknown64() = 0; // 64
    virtual void Unknown65() = 0; // 65
    virtual void Unknown66() = 0; // 66
    virtual void Unknown67() = 0; // 67

public:
    virtual void SetTimescale(float flTimescale) = 0; // 68
private:
    virtual uint32_t GetAppID() = 0; // 69
public:
    // 看起来内部调用了 offset 155 读SteamId, 但是还有个Sub判断Bool不知道是干什么的, 返回值不明
    virtual const SteamId_t* GetClientSteamId(PlayerSlot_t slot) = 0; // 70

private:
    virtual void SetGamestatsData() = 0; // 71
    virtual void GetGamestatsData() = 0; // 72

public:
    // 这里是传的Kv
    virtual void ClientCommandKeyValues(PlayerSlot_t slot, void* pCommand) = 0; // 73

private:
    virtual void Unknown74() = 0; // 74

public:
    // 内部调用 offset 2490 所以我觉得应该是判断是否被Steam认证
    virtual bool IsClientFullyAuthenticated(PlayerSlot_t slot) = 0; // 75

    virtual CGlobalVars* GetGlobalVars() = 0; // 76

    virtual void SetFakeClientConVarValue(PlayerSlot_t slot, const char* cvar, const char* value) = 0; // 77

private:
    virtual void GetSharedEdictChangeInfo() = 0; // 78
    virtual void SetAchievementMgr()        = 0; // 79
    virtual void GetAchievementMgr()        = 0; // 80
    virtual void GetPlayerInfo()            = 0; // 81

private:
    // 里面判断了virtual 22 -> 判断 offset 96 我也不知道是啥, 失败直接返回0
    virtual SteamId_t GetClientSteamId_Unknown(PlayerSlot_t slot) = 0; // 82

private:
    virtual void GetPVSForSpawnGroup()  = 0; // 83
    virtual void FindSpawnGroupByName() = 0; // 84
    virtual void GetGameServerSteamID() = 0; // 85

public:
    virtual int GetBuildVersion() const = 0; // 86

    // 判断完美国服
    virtual bool IsClientLowViolence(PlayerSlot_t slot) = 0; // 87

private:
    virtual void DisconnectClient()    = 0; // 88
    virtual void DisconnectAllClient() = 0; // 89
    virtual void Unknown90()           = 0; // 90
    virtual void GetClientListening()  = 0; // 91
    virtual void SetClientListening()  = 0; // 92
    virtual void SetClientProximity()  = 0; // 93
    virtual void Unknown94()           = 0; // 94
    virtual void Unknown95()           = 0; // 95
    virtual void Unknown96() = 0;           // 96

public:
    // Server 跟 disconnect时的提示有关系, 我也不知道取值
    virtual void  KickClient(PlayerSlot_t slot, const char* reason, int disconnectReason) = 0; // 97
    virtual void* BanClientUnknown1(SteamId_t steamId, float flDuration, bool bKick) = 0;      // 98
    virtual void* BanClient(PlayerSlot_t slot, float flDuration, bool bKick) = 0;              // 99

    virtual bool  StartHltvReplay(PlayerSlot_t slot, int64_t unknown2); // 100
    virtual void* StopHltvReplay(PlayerSlot_t slot);                    // 101
    virtual void  StopHltvReplayAll();                                  // 102

private:
    // client  virtual 70 -> offset 3120
    virtual void* GetClientUnknown(PlayerSlot_t slot) = 0; // 103

    virtual void Unknown104() = 0; // 104
    virtual void Unknown105() = 0; // 105
    virtual void Unknown106() = 0; // 106
    virtual void Unknown107() = 0; // 107

    // Find client exists by offset 3120
    virtual bool CheckClientUnknown() = 0;                               // 108
    virtual void SetClientUpdateRate(PlayerSlot_t slot, float rate) = 0; // 109
    virtual void UpdateClientRate(PlayerSlot_t nSlot) = 0;               // 110

    virtual void Unknown111() = 0; // 111
    virtual void Unknown112() = 0; // 112
    virtual void Unknown113() = 0; // 113
    // more rest
};

#endif
