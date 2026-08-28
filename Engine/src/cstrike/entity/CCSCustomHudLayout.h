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

#ifndef CSTRIKE_ENTITY_CUSTOMHUDLAYOUT_H
#define CSTRIKE_ENTITY_CUSTOMHUDLAYOUT_H

#include "definitions.h"

#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/type/CNetworkUtlVectorBase.h"
#include "cstrike/type/CUtlString.h"
#include "cstrike/type/CUtlVectorEmbeddedNetworkVar.h"

enum class EHudPanelClassStatus_t : uint32_t
{
    k_eHudPanelClassStatus_Undefined        = 0xFFFFFFFF,
    k_eHudPanelClassStatus_DoesNotHaveClass = 0x0,
    k_eHudPanelClassStatus_HasClass         = 0x1
};

struct HUDPanelHasClass_t
{
    uint16_t               m_nPanelIdIndex;
    uint16_t               m_nClassNameIndex;
    EHudPanelClassStatus_t m_eClassStatus;
};

class HUDPanelDialogVariableString_t
{
    void* m_pVTable;

public:
    uint16_t   m_nPanelIdIndex;
    uint16_t   m_nDialogVariableIndex;
    CUtlString m_sValue;
    bool       m_bIsSet;

    explicit HUDPanelDialogVariableString_t();

    HUDPanelDialogVariableString_t(uint16_t nPanelIdIndex, uint16_t nDialogVariableIndex, const CUtlString& sValue, bool bIsSet) : HUDPanelDialogVariableString_t()
    {
        m_nPanelIdIndex        = nPanelIdIndex;
        m_nDialogVariableIndex = nDialogVariableIndex;
        m_sValue               = sValue;
        m_bIsSet               = bIsSet;
    }
};

class CCSCustomHudLayoutState
{
    DECLARE_SCHEMA_CLASS(CCSCustomHudLayoutState)
public:
    SCHEMA_FIELD(int32_t, m_playerSlot)
    SCHEMA_FIELD(bool, m_bInputCaptureEnabled)
    SCHEMA_NETWORK_VECTOR_BASE_FIELD(HUDPanelHasClass_t, m_vecHasClasses)
    SCHEMA_NETWORK_VECTOR_BASE_FIELD(HUDPanelDialogVariableString_t, m_vecDialogVariableStrings)
};

class CCSCustomHudLayout : public CBaseEntity
{
    DECLARE_SCHEMA_CLASS(CCSCustomHudLayout)
public:
    SCHEMA_FIELD(CUtlString, m_strLayout)
    SCHEMA_EMBEDDED_NETWORK_VAR_FIELD(CCSCustomHudLayoutState, m_vecPlayerLayoutStates)
    SCHEMA_FIELD(CCSCustomHudLayoutState, m_globalLayoutState)
    SCHEMA_NETWORK_VECTOR_BASE_FIELD(CUtlString, m_vecPanelIds)
    SCHEMA_NETWORK_VECTOR_BASE_FIELD(CUtlString, m_vecClassNames)
    SCHEMA_NETWORK_VECTOR_BASE_FIELD(CUtlString, m_vecDialogVariableNames)

    void SetHasClass(const char* panelId, const char* className, int32_t hasClass);
    void SetHasClassForPlayer(PlayerSlot_t playerSlot, const char* panelId, const char* className, int32_t hasClass);
    void SetDialogVariableString(const char* panelId, const char* variableName, const char* value);
    void SetDialogVariableStringForPlayer(PlayerSlot_t playerSlot, const char* panelId, const char* variableName, const char* value);
    void ClearDialogVariableStringForPlayer(PlayerSlot_t playerSlot, const char* panelId, const char* variableName);
    void SetInputCaptureEnabled(PlayerSlot_t playerSlot, bool enabled);

    [[nodiscard]] bool IsInputCaptureEnabled(PlayerSlot_t playerSlot);
};

#endif
