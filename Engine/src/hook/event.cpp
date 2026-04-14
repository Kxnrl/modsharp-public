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

#include "address.h"
#include "bridge/adapter.h"
#include "bridge/forwards/forward.h"
#include "gamedata.h"
#include "global.h"
#include "hook/installer.h"
#include "logging.h"

#include "cstrike/interface/IGameEvent.h"
#include "cstrike/type/CNetworkGameServer.h"
#include "cstrike/type/CServerSideClient.h"
#include "cstrike/type/VProf.h"

#include <safetyhook.hpp>

#include <algorithm>
#include <bitset>
#include <string>
#include <vector>

// #define EVENT_HOOK_ASSERT

static constexpr size_t          MAX_EVENT_ID = 512;
static std::bitset<MAX_EVENT_ID> s_hookedEventIds;

class CGameEventManager;
class IGameEventSystem;
class CGameSystemReallocatingFactory;

BeginMemberHookScope(CGameEventManager)
{
    DeclareVirtualHook(FireEvent, bool, (CGameEventManager * pManager, IGameEvent * pEvent, bool serverOnly))
    {
#ifdef EVENT_HOOK_ASSERT
        WARN("%10s: 0x%p\n" // CGameEventManager*
             "%10s: 0x%p\n" // IGameEvent*
             "%10s: %s",    // bool -> String
             "this", pManager,
             "pEvent", pEvent,
             "serverOnly", BOOLVAL(serverOnly));
#endif

        if (!pEvent)
            return false;

        VPROF_MS_HOOK();

        if (const auto id = pEvent->GetID(); id >= 0 && std::cmp_less(id, MAX_EVENT_ID) && s_hookedEventIds.test(id))
        {
            auto noClients = serverOnly;

            if (!forwards::HookFireEvent->Invoke(pEvent, &noClients))
            {
                eventManager->FreeEvent(pEvent);
                return false;
            }

            return FireEvent(pManager, pEvent, noClients);
        }
        return FireEvent(pManager, pEvent, serverOnly);
    }
}

class CGameEventListener : public IGameEventListener2
{
public:
    void FireGameEvent(IGameEvent* event) override
    {
        if (const auto id = event->GetID(); id >= 0 && static_cast<size_t>(id) < MAX_EVENT_ID && s_hookedEventIds.test(id))
        {
            forwards::FireGameEvent->Invoke(event);
        }
    }

} g_EventListener;

void InstallEventHooks()
{
    VHOOK(CGameEventManager, FireEvent, server);
}

// Natives Below

static void AddEventHook(const char* pEventName)
{
    if (eventManager->FindListener(&g_EventListener, pEventName))
        return;

    auto retEventId = eventManager->AddListener(&g_EventListener, pEventName, true);
    if (retEventId == -1)
    {
        FERROR("Failed to add event listener: Invalid event name %s", pEventName);
        return;
    }

    if (static_cast<size_t>(retEventId) < MAX_EVENT_ID) { s_hookedEventIds.set(retEventId); }
}

static IGameEvent* CreateEventNative(const char* pEventName, bool force)
{
    return eventManager->CreateEvent(pEventName, force);
}

static void FireEvent(IGameEvent* pEvent, bool serverOnly)
{
    eventManager->FireEvent(pEvent, serverOnly);
}

static void FireToClient(IGameEvent* pEvent, PlayerSlot_t slot)
{
    const auto pProxy = address::server::GetLegacyGameEventListener(slot);

    if (!pProxy)
        return;

    const auto pClient = sv->GetClientSafety(slot);
    if (pClient == nullptr || !pClient->IsConnected())
        return;

    pProxy->FireGameEvent(pEvent);
}

static void FireToClients(IGameEvent* pEvent)
{
    const auto clients = sv->GetClients();

    for (int i = clients->Count() - 1; i >= 0; i--)
    {
        const auto pClient = clients->Element(i);
        if (pClient == nullptr || !pClient->IsConnected())
            continue;

        const auto pProxy = address::server::GetLegacyGameEventListener(i);

        if (!pProxy)
            continue;

        pProxy->FireGameEvent(pEvent);
    }
}

static void FreeEvent(IGameEvent* pEvent)
{
    eventManager->FreeEvent(pEvent);
}

static IGameEvent* CloneEvent(IGameEvent* pEvent)
{
    return eventManager->DuplicateEvent(pEvent);
}

static bool FindListener(PlayerSlot_t slot, const char* pEventName)
{
    const auto pProxy = address::server::GetLegacyGameEventListener(slot);

    if (!pProxy)
        return false;

    return eventManager->FindListener(pProxy, pEventName);
}

static const char* GetEventName(IGameEvent* pEvent)
{
    return pEvent->GetName();
}

static const char* GetEventString(IGameEvent* pEvent, const char* pKey, const char* defaultValue)
{
    return pEvent->GetString(pKey, defaultValue);
}

static int GetEventInt(IGameEvent* pEvent, const char* pKey, int defaultValue)
{
    return pEvent->GetInt(pKey, defaultValue);
}

static uint64_t GetEventUInt64(IGameEvent* pEvent, const char* pKey, uint64_t defaultValue)
{
    return pEvent->GetUint64(pKey, defaultValue);
}

static CEntityInstance* GetEventPlayerController(IGameEvent* pEvent, const char* pKey)
{
    return pEvent->GetPlayerController(pKey);
}

static CEntityInstance* GetEventPlayerPawn(IGameEvent* pEvent, const char* pKey)
{
    return pEvent->GetPlayerPawn(pKey);
}

static float GetEventFloat(IGameEvent* pEvent, const char* pKey, float defaultValue)
{
    return pEvent->GetFloat(pKey, defaultValue);
}

static void SetEventString(IGameEvent* pEvent, const char* pKey, const char* pValue)
{
    pEvent->SetString(pKey, pValue);
}

static void SetEventInt(IGameEvent* pEvent, const char* pKey, int value)
{
    pEvent->SetInt(pKey, value);
}

static void SetEventFloat(IGameEvent* pEvent, const char* pKey, float value)
{
    pEvent->SetFloat(pKey, value);
}

static void SetEventUInt64(IGameEvent* pEvent, const char* pKey, uint64_t value)
{
    pEvent->SetUint64(pKey, value);
}

static void SetEventPlayerPawn(IGameEvent* pEvent, const char* pKey, CBasePlayerPawn* pPawn)
{
    return pEvent->SetPlayer(pKey, reinterpret_cast<CEntityInstance*>(pPawn));
}

static void SetEventPlayerSlot(IGameEvent* pEvent, const char* pKey, int slot)
{
    return pEvent->SetPlayer(pKey, slot);
}

namespace natives::event
{
void Init()
{
    bridge::CreateNative("Event.HookEvent", reinterpret_cast<void*>(AddEventHook));
    bridge::CreateNative("Event.CreateEvent", reinterpret_cast<void*>(CreateEventNative));
    bridge::CreateNative("Event.FireEvent", reinterpret_cast<void*>(FireEvent));
    bridge::CreateNative("Event.FireToClient", reinterpret_cast<void*>(FireToClient));
    bridge::CreateNative("Event.FireToClients", reinterpret_cast<void*>(FireToClients));
    bridge::CreateNative("Event.FreeEvent", reinterpret_cast<void*>(FreeEvent));
    bridge::CreateNative("Event.CloneEvent", reinterpret_cast<void*>(CloneEvent));
    bridge::CreateNative("Event.FindListener", reinterpret_cast<void*>(FindListener));

    bridge::CreateNative("Event.GetName", reinterpret_cast<void*>(GetEventName));

    // Getter
    bridge::CreateNative("Event.GetString", reinterpret_cast<void*>(GetEventString));
    bridge::CreateNative("Event.GetInt", reinterpret_cast<void*>(GetEventInt));
    bridge::CreateNative("Event.GetFloat", reinterpret_cast<void*>(GetEventFloat));
    bridge::CreateNative("Event.GetUInt64", reinterpret_cast<void*>(GetEventUInt64));
    bridge::CreateNative("Event.GetPlayerController", reinterpret_cast<void*>(GetEventPlayerController));
    bridge::CreateNative("Event.GetPlayerPawn", reinterpret_cast<void*>(GetEventPlayerPawn));

    // Setter
    bridge::CreateNative("Event.SetString", reinterpret_cast<void*>(SetEventString));
    bridge::CreateNative("Event.SetInt", reinterpret_cast<void*>(SetEventInt));
    bridge::CreateNative("Event.SetFloat", reinterpret_cast<void*>(SetEventFloat));
    bridge::CreateNative("Event.SetUInt64", reinterpret_cast<void*>(SetEventUInt64));
    bridge::CreateNative("Event.SetPlayerPawn", reinterpret_cast<void*>(SetEventPlayerPawn));
    bridge::CreateNative("Event.SetPlayerSlot", reinterpret_cast<void*>(SetEventPlayerSlot));
}
} // namespace natives::event