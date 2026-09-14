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
#include "module.h"

#include "cstrike/entity/CCSCustomHudLayout.h"

HUDPanelDialogVariableString_t::HUDPanelDialogVariableString_t()
{
    static void* vtable = modules::server->GetVirtualTableByName("HUDPanelDialogVariableString_t");
    AssertPtr(vtable);

    m_pVTable = vtable;

    m_nPanelIdIndex        = 0;
    m_nDialogVariableIndex = 0;
    m_sValue               = "";
    m_bIsSet               = false;
}

void CCSCustomHudLayout::SetHasClass(const char* panelId, const char* className, EHudPanelClassStatus_t classStatus)
{
    if (!panelId || !className)
        return;

    CUtlString panel(panelId);
    CUtlString cssClass(className);
    address::server::CCSCustomHudLayout_SetHasClass(this, &panel, &cssClass, classStatus);
}

void CCSCustomHudLayout::SetHasClassForPlayer(PlayerSlot_t playerSlot, const char* panelId, const char* className, EHudPanelClassStatus_t classStatus)
{
    if (playerSlot >= CS_MAX_PLAYERS || !panelId || !className)
        return;

    CUtlString panel(panelId);
    CUtlString cssClass(className);
    address::server::CCSCustomHudLayout_SetHasClassForPlayer(this, playerSlot, &panel, &cssClass, classStatus);
}

void CCSCustomHudLayout::SetDialogVariableString(const char* panelId, const char* variableName, const char* value)
{
    if (!panelId || !variableName || !value)
        return;

    CUtlString panel(panelId);
    CUtlString variable(variableName);
    CUtlString stringValue(value);
    address::server::CCSCustomHudLayout_SetDialogVariableString(this, &panel, &variable, &stringValue);
}

void CCSCustomHudLayout::SetDialogVariableStringForPlayer(PlayerSlot_t playerSlot, const char* panelId, const char* variableName, const char* value)
{
    if (playerSlot >= CS_MAX_PLAYERS || !panelId || !variableName || !value)
        return;

    CUtlString panel(panelId);
    CUtlString variable(variableName);
    CUtlString stringValue(value);
    address::server::CCSCustomHudLayout_SetDialogVariableStringForPlayer(this, playerSlot, &panel, &variable, &stringValue);
}

void CCSCustomHudLayout::ClearDialogVariableStringForPlayer(PlayerSlot_t playerSlot, const char* panelId, const char* variableName)
{
    if (playerSlot >= CS_MAX_PLAYERS || !panelId || !variableName)
        return;

    CUtlString panel(panelId);
    CUtlString variable(variableName);
    address::server::CCSCustomHudLayout_ClearDialogVariableStringForPlayer(this, playerSlot, &panel, &variable);
}

void CCSCustomHudLayout::SetInputCaptureEnabled(PlayerSlot_t playerSlot, bool enabled)
{
    if (playerSlot >= CS_MAX_PLAYERS)
        return;

    address::server::CCSCustomHudLayout_SetInputCaptureEnabled(this, playerSlot, enabled);
}

bool CCSCustomHudLayout::IsInputCaptureEnabled(PlayerSlot_t playerSlot)
{
    if (playerSlot >= CS_MAX_PLAYERS)
        return false;

    return address::server::CCSCustomHudLayout_IsInputCaptureEnabled(this, playerSlot);
}
