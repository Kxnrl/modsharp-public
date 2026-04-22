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

#ifndef MS_VHOOK_CALL_H
#define MS_VHOOK_CALL_H

#include "logging.h"

#define CALL_VIRTUAL(retType, idx, ...) \
    vhook::call::CallVirtual<retType>(idx, __FUNCTION__, __VA_ARGS__)

#define DeclareVFuncIndex(c, m, o) \
    static auto o = g_pGameData->GetVFunctionIndex(#c "::" #m)

#define DeclareOffset(c, m, o) \
    static auto o = g_pGameData->GetOffset(#c "::" #m)

#define VCall_AutoVoid(c, m, ...)                                                \
    static auto offset__##c##__##m = g_pGameData->GetVFunctionIndex(#c "::" #m); \
    vhook::call::CallVirtual<void>(offset__##c##__##m, __FUNCTION__, __VA_ARGS__)

#define VCall_Manual(o, ret, ...) \
    vhook::call::CallVirtual<ret>(o, __FUNCTION__, __VA_ARGS__)

namespace vhook::call
{
template <typename T = void*>
inline T GetVMethod(unsigned int uIndex, void* pClass, const char* func)
{
    if (!pClass)
    {
        FatalError("Tried getting virtual function from a null class -> %s", func);
        return T();
    }

    void** pVTable = *static_cast<void***>(pClass);
    if (!pVTable)
    {
        FatalError("Tried getting virtual function from a null vtable -> %s", func);
        return T();
    }

    return reinterpret_cast<T>(pVTable[uIndex]);
}

template <typename T, typename... Args>
inline T CallVirtual(unsigned int uIndex, const char* func, void* pClass, Args... args)
{
#ifdef PLATFORM_WINDOWS
    auto pFunc = GetVMethod<T(__thiscall*)(void*, Args...)>(uIndex, pClass, func);
#else
    auto pFunc = GetVMethod<T(__attribute__((cdecl))*)(void*, Args...)>(uIndex, pClass, func);
#endif
    if (!pFunc)
    {
        FatalError("Tried calling a null virtual function.\n  Index: %u\n  Called from: %s", uIndex, func);
        return T();
    }

    return pFunc(pClass, args...);
}

template <typename T, typename... Args>
inline T CallVirtual(unsigned int uIndex, void* pClass, Args... args)
{
    return CallVirtual<T>(uIndex, "(unknown file)", 0, pClass, args...);
}

} // namespace vhook::call

#endif