#pragma once

#ifndef CSTRIKE_TYPE_CENTITYCLASS_H
#    define CSTRIKE_TYPE_CENTITYCLASS_H

#include <cstdint>

class CEntityIdentity;

class CEntityClass
{
public:
    CEntityClass() = delete;

    [[nodiscard]] CEntityIdentity* GetEntityListHead() const
    {
        return *reinterpret_cast<CEntityIdentity* const*>(reinterpret_cast<const uintptr_t>(this) + 0x128);
    }
};

#endif