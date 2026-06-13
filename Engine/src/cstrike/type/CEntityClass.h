#pragma once

#ifndef CSTRIKE_TYPE_CENTITYCLASS_H
#    define CSTRIKE_TYPE_CENTITYCLASS_H

#include <cstdint>

class CEntityIdentity;

class CEntityClass
{
public:
    CEntityClass() = delete;

    inline static uint32_t sm_nEntityListHeadOffset = 0;

    [[nodiscard]] CEntityIdentity* GetEntityListHead() const
    {
        return *reinterpret_cast<CEntityIdentity* const*>(reinterpret_cast<const uintptr_t>(this) + sm_nEntityListHeadOffset);
    }
};

#endif