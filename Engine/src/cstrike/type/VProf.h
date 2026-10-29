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

#ifndef CSTRIKE_TYPE_VRPOF_H
#define CSTRIKE_TYPE_VRPOF_H

#include "platform.h"

#include "cstrike/type/CUtlSourceLocation.h"

#include "strtool.h"

#define VPROF_PREFIX_MS "ModSharp::"
#define VPROF_GROUP_MS_HOOKS "ModSharp::Hooks"
#define VPROF_GROUP_CS_SCRIPT "CS Script"

using VProfExitScopeCB = void (*)();

#define VPROF_BUDGET_GROUP_ID_UNACCOUNTED 0

enum VProfBudgetFlags
{
    Client = 1 << 0,
    Server = 1 << 1,
    Other  = 1 << 2,
    Hidden = 1 << 15,
    All    = 0xFFFF
};

struct VProfBudgetGroupCallSite
{
    constexpr VProfBudgetGroupCallSite(const char* group_name, int flags, int group_id = VPROF_BUDGET_GROUP_ID_UNACCOUNTED) :
        m_pGroupName(group_name), m_nFlags(flags), m_nGroupId(group_id) {}

    const char* m_pGroupName;
    int         m_nFlags;
    int         m_nGroupId;
};

template <int DETAIL_LEVEL, bool ASSERT_ACCOUNTED>
class VProfScopeHelper
{
public:
    VProfScopeHelper(const char* pszName, VProfBudgetGroupCallSite& budgetGroup, const CUtlSourceLocation& location)
    {
        m_pExitScope = EnterScopeInternalBudgetFlags(pszName, budgetGroup, location);
    }

    VProfScopeHelper(const VProfScopeHelper&)            = delete;
    VProfScopeHelper& operator=(const VProfScopeHelper&) = delete;

    ~VProfScopeHelper()
    {
        ExitScope();
    }

    void ExitScope()
    {
        if (!m_pExitScope)
            return;

        m_pExitScope();
        m_pExitScope = nullptr;
    }

    static MS_CLASS_IMPORT VProfExitScopeCB EnterScopeInternal(const char* pszName, const CUtlSourceLocation& location);
    static MS_CLASS_IMPORT VProfExitScopeCB EnterScopeInternalBudgetFlags(const char* pszName, VProfBudgetGroupCallSite& budgetGroup, const CUtlSourceLocation& location);

private:
    VProfExitScopeCB m_pExitScope = nullptr;
};

#ifndef VPROF_LEVEL
#    define VPROF_LEVEL 0
#endif

#define VPROF_DETAIL_(detail, name, group, assertAccounted, budgetFlags, source)        \
    static constinit VProfBudgetGroupCallSite __vprof_budget_group(group, budgetFlags); \
    VProfScopeHelper<detail, assertAccounted> vprofHelper_(name, __vprof_budget_group, source)

#define VPROF_0(name, group, assertAccounted, budgetFlags, source) VPROF_DETAIL_(0, name, group, assertAccounted, budgetFlags, source)

#if VPROF_LEVEL > 0
#    define VPROF_1(name, group, assertAccounted, budgetFlags, source) VPROF_DETAIL_(1, name, group, assertAccounted, budgetFlags, source)
#else
#    define VPROF_1(name, group, assertAccounted, budgetFlags, source) ((void)0)
#endif

#if VPROF_LEVEL > 1
#    define VPROF_2(name, group, assertAccounted, budgetFlags, source) VPROF_DETAIL_(2, name, group, assertAccounted, budgetFlags, source)
#else
#    define VPROF_2(name, group, assertAccounted, budgetFlags, source) ((void)0)
#endif

#if VPROF_LEVEL > 2
#    define VPROF_3(name, group, assertAccounted, budgetFlags, source) VPROF_DETAIL_(3, name, group, assertAccounted, budgetFlags, source)
#else
#    define VPROF_3(name, group, assertAccounted, budgetFlags, source) ((void)0)
#endif

#if VPROF_LEVEL > 3
#    define VPROF_4(name, group, assertAccounted, budgetFlags, source) VPROF_DETAIL_(4, name, group, assertAccounted, budgetFlags, source)
#else
#    define VPROF_4(name, group, assertAccounted, budgetFlags, source) ((void)0)
#endif

#define VPROF_(name, detail, group, bAssertAccounted, budgetFlags, source) VPROF_##detail(name, group, bAssertAccounted, budgetFlags, source)

#define VPROF_BUDGET(name, group, source) VPROF_BUDGET_FLAGS(name, group, VProfBudgetFlags::Server, source)
#define VPROF_BUDGET_FLAGS(name, group, flags, source) VPROF_(name, 0, group, false, flags, source)

#define VPROF_MS_AUTO(group)                                                                       \
    static constexpr CUtlSourceLocation __vprof_source_location(__FILE__, __FUNCTION__, __LINE__); \
    static constexpr auto               __vprof_full_name = concat(VPROF_PREFIX_MS, __FUNCTION__); \
    VPROF_BUDGET(__vprof_full_name.c_str(), group, __vprof_source_location)

#define VPROF_MS_HOOK() VPROF_MS_AUTO(VPROF_GROUP_MS_HOOKS)

#define VPROF_MS_HOOK_SCOPE(name)                                                                                         \
    static constexpr CUtlSourceLocation       __vprof_scope_source_location(__FILE__, __FUNCTION__, __LINE__);            \
    static constinit VProfBudgetGroupCallSite __vprof_scope_budget_group(VPROF_GROUP_MS_HOOKS, VProfBudgetFlags::Server); \
    VProfScopeHelper<0, false>                vprofScopeHelper_(VPROF_PREFIX_MS name, __vprof_scope_budget_group, __vprof_scope_source_location)

#define VPROF_CS_SCRIPT_SCOPE(name)                                                                                     \
    static constexpr CUtlSourceLocation       __vprof_cs_source_location(__FILE__, __FUNCTION__, __LINE__);             \
    static constinit VProfBudgetGroupCallSite __vprof_cs_budget_group(VPROF_GROUP_CS_SCRIPT, VProfBudgetFlags::Server); \
    VProfScopeHelper<0, false>                vprofCsScriptHelper_(name, __vprof_cs_budget_group, __vprof_cs_source_location)

#endif