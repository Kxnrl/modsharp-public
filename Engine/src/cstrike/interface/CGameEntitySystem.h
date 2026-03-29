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

#ifndef CSTRIKE_INTERFACE_ENTITYLIST_H
#define CSTRIKE_INTERFACE_ENTITYLIST_H

#include "definitions.h"

struct KeyValuesVariantItem;
class CBaseHandle;
class CBaseEntity;
class Variant_t;
class Vector;

class IEntityListener
{
public:
    virtual void OnEntityCreated(CBaseEntity* pEntity)                       = 0;
    virtual void OnEntitySpawned(CBaseEntity* pEntity)                       = 0;
    virtual void OnEntityDeleted(CBaseEntity* pEntity)                       = 0;
    virtual void OnEntityFollowed(CBaseEntity* pEntity, CBaseEntity* pOwner) = 0;
};

class INotifyManager
{
    enum notify_system_event_t
    {
        NOTIFY_EVENT_TELEPORT = 0,
        NOTIFY_EVENT_DESTROY,
    };

    virtual void AddEntity(CBaseEntity* pNotify, CBaseEntity* pWatched)                                       = 0;
    virtual void RemoveEntity(CBaseEntity* pNotify, CBaseEntity* pWatched)                                    = 0;
    virtual void ReportNamedEvent(CBaseEntity* pEntity, const char* pEventName)                               = 0;
    virtual void ReportSystemEvent(CBaseEntity* pEntity, notify_system_event_t eventType, const void* params) = 0;
    virtual void ClearEntity(CBaseEntity* pNotify)                                                            = 0;
};

class CGameEntitySystem
{
public:
    void AddListenerEntity(IEntityListener* pListener);
    void RemoveListenerEntity(IEntityListener* pListener);

    CBaseEntity* FindEntityByEHandle(const CBaseHandle& handle);
    template <typename T>
    T FindEntityByEHandle(const CBaseHandle& handle)
    {
        return reinterpret_cast<T>(FindEntityByEHandle(handle));
    }

    [[nodiscard]] const matrix3x4_t& GetSpawnOriginOffset() const;

    // CUtlSymbolLarge
    [[nodiscard]] CUtlSymbolLarge AllocPooledString(const char* pStr) const;

    CBaseEntity* FindEntityByIndex(EntityIndex_t index);
    CBaseEntity* FindByClassname(CBaseEntity* startEntity, const char* classname);
    CBaseEntity* FindByName(CBaseEntity* startEntity, const char* name);
    CBaseEntity* FindInSphere(CBaseEntity* startEntity, Vector* center, float radius);

    CBaseEntity* SpawnEntityFromKeyValuesSync(const char* classname, const KeyValuesVariantItem* items = nullptr, int count = 0) const;
    
    CBaseEntity* CreateEntityByName(const char* classname) const;

    void AddEntityIOEvent(CBaseEntity* pEntity, const char* pInputName, CBaseEntity* pActivator, CBaseEntity* pCaller, Variant_t* pValue, float flDelay, int outputID);

    template <typename T>
    T CreateEntityByNameAs(const char* classname) const
    {
        return reinterpret_cast<T>(CreateEntityByName(classname));
    }

    template <typename T>
    T FindEntityByIndex(EntityIndex_t index)
    {
        return reinterpret_cast<T>(FindEntityByIndex(index));
    }
    template <typename T>
    T FindByClassnameCast(T startEntity, const char* classname)
    {
        return reinterpret_cast<T>(FindByClassname(startEntity, classname));
    }

    static void Init();
};

#endif
