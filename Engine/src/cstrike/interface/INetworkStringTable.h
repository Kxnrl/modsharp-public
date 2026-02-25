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

#ifndef CSTRIKE_INTERFACE_NETWORKSTRINGTABLE_H
#define CSTRIKE_INTERFACE_NETWORKSTRINGTABLE_H

#include "cstrike/interface/IAppSystem.h"

#include <cstdint>

class INetworkStringTable;
class CNetworkStringTableItem;

class INetworkStringTableContainer : public IAppSystem
{
    virtual ~INetworkStringTableContainer() = 0;

public:
    // table creation/destruction
    virtual INetworkStringTable* CreateStringTable(const char* tableName, int maxentries, int userdatafixedsize = 0, int userdatanetworkbits = 0, int flags = 0) = 0;
    virtual void                 RemoveAllTables()                                                                                                               = 0;

    // table info
    virtual INetworkStringTable* FindTable(const char* tableName) const = 0;
    virtual INetworkStringTable* GetTable(int32_t stringTable) const    = 0;
    virtual int                  GetNumTables() const                   = 0;

    // ??
    virtual void SetAllowClientSideAddString(INetworkStringTable* table, bool bAllowClientSideAddString) = 0;
    virtual void CreateDictionary(const char* pchMapName)                                                = 0;
};

struct StringTableUserData
{
    void*   m_pData;
    int32_t m_nBytes;

    explicit StringTableUserData(void* data = nullptr, int32_t bytes = 0) :
        m_pData(data), m_nBytes(bytes)
    {
    }
};

class INetworkStringTable
{
    virtual ~INetworkStringTable() = 0;

public:
    virtual const char* GetTableName() const   = 0;
    virtual int32_t     GetTableId() const     = 0;
    virtual int64_t     GetStringCount() const = 0;

private:
    virtual void Method_004() = 0;
    virtual void Method_005() = 0;
    virtual void Method_006() = 0;

public:
    virtual int32_t              AddString(bool bIsServer, const char* value, const StringTableUserData* data)         = 0;
    virtual const char*          GetString(int32_t index) const                                                        = 0;
    virtual void                 SetStringUserData(int32_t index, const StringTableUserData* data, bool forceOverride) = 0;
    virtual StringTableUserData* GetStringUserData(int32_t index) const                                                = 0;
    virtual int32_t              FindStringIndex(const char* value) const                                              = 0;

private:
    virtual void Method_012() = 0;

public:
    virtual void SetAllowClientSideAddString(bool state) = 0;
};

using CNetworkStringTableContainer = INetworkStringTableContainer;
using CNetworkStringTable          = INetworkStringTable;

class CSharpNetworkStringTableHelper
{
public:
    virtual ~CSharpNetworkStringTableHelper() = default;
    virtual const char* GetName(INetworkStringTable* pTable) const { return pTable->GetTableName(); }
    virtual int32_t     GetId(INetworkStringTable* pTable) const { return pTable->GetTableId(); }
    virtual int64_t     GetStringCount(INetworkStringTable* pTable) const { return pTable->GetStringCount(); }
    virtual int32_t     AddString(INetworkStringTable* pTable, bool bIsServer, const char* value, void* data, int32_t size)
    {
        const StringTableUserData vd(data, size);
        return pTable->AddString(bIsServer, value, &vd);
    }
    virtual const char*          GetString(INetworkStringTable* pTable, int32_t index) const { return pTable->GetString(index); }
    virtual StringTableUserData* GetStringUserData(INetworkStringTable* pTable, int32_t index) const { return pTable->GetStringUserData(index); }
    virtual void                 SetStringUserData(INetworkStringTable* pTable, int32_t index, void* data, int32_t size)
    {
        const StringTableUserData vd(data, size);
        pTable->SetStringUserData(index, &vd, false);
    }
    virtual int32_t FindStringIndex(INetworkStringTable* pTable, const char* value) const { return pTable->FindStringIndex(value); }
};

#endif