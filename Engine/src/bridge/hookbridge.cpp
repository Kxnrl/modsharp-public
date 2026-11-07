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

#include "bridge/hookbridge.h"

#include "global.h"
#include "logging.h"

InlineHook::~InlineHook()
{
    m_hkInline.reset();
    m_pTargetFn = nullptr;
    m_pHookFn   = nullptr;
}

void InlineHook::Prepare(void* targetFn, void* hookFn)
{
    m_pTargetFn = targetFn;
    m_pHookFn   = hookFn;
}

bool InlineHook::Install()
{
    if (m_hkInline || m_pTargetFn == nullptr || m_pHookFn == nullptr)
        return false;

    auto hook_result = safetyhook::InlineHook::create(m_pTargetFn, m_pHookFn);

    if (!hook_result)
    {
        const auto& err = hook_result.error();

        FERROR("Failed to create hook at %p. Reason: %s", err.ip, g_szInlineHookErrors[err.type]);

        return false;
    }

    m_hkInline = std::move(*hook_result);

    return true;
}

void InlineHook::Uninstall()
{
    m_hkInline.reset();
}

void* InlineHook::GetOriginalFunction()
{
    return m_hkInline.original<void*>();
}

MidFuncHook::~MidFuncHook()
{
    m_hkMid.reset();
    m_pTargetFn = nullptr;
    m_pHookFn   = nullptr;
}

void MidFuncHook::Prepare(void* targetFn, void* hookFn)
{
    m_pTargetFn = targetFn;
    m_pHookFn   = hookFn;
}

bool MidFuncHook::Install()
{
    if (m_hkMid || m_pTargetFn == nullptr || m_pHookFn == nullptr)
        return false;

    auto hook_result = safetyhook::MidHook::create(m_pTargetFn, reinterpret_cast<safetyhook::MidHookFn>(m_pHookFn));

    if (!hook_result)
    {
        const auto& err = hook_result.error();

        FERROR("Failed to create midfunc hook at %p. Reason: %s", m_pTargetFn, g_szMidFuncHookErrors[err.type]);

        return false;
    }

    m_hkMid = std::move(*hook_result);

    return true;
}

void MidFuncHook::Uninstall()
{
    m_hkMid.reset();
}

void* MidFuncHook::GetOriginalFunction()
{
    FatalError("YOU SHOULD NOT CALL THIS -> %s", __FUNCTION__);

    // std::unreachable();
#if defined PLATFORM_WINDOWS
    __assume(false);
#else
    __builtin_unreachable();
#endif
}