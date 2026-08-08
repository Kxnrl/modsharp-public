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
#include "gamedata.h"
#include "global.h"
#include "vhook/call.h"

#include "cstrike/type/CEntityInstance.h"

#include <algorithm>
#include <array>

HSCRIPT* CEntityInstance::GetOrCreatePublicScriptScope()
{
    return address::server::CEntityInstance_GetOrCreatePublicScriptScope(this);
}

HSCRIPT* CEntityInstance::GetOrCreatePrivateScriptScope()
{
#ifdef PLATFORM_WINDOWS
    return address::server::CEntityInstance_GetOrCreatePrivateScriptScope(this);
#else
    address::server::CEntityInstance_GetOrCreatePrivateScriptScope(this);
    return *reinterpret_cast<HSCRIPT**>(reinterpret_cast<uintptr_t>(this) + 0x18);
#endif
}

EntityIndex_t CEntityInstance::GetEntityIndexNative()
{
    return address::server::CEntityInstance_GetEntityIndex(this);
}

CBaseHandle CEntityInstance::GetRefEHandleNative()
{
#ifdef PLATFORM_WINDOWS
    EHandle_t handle = INVALID_EHANDLE;
    address::server::CEntityInstance_GetRefEHandle(this, &handle);
    return CBaseHandle(handle);
#else
    return CBaseHandle(address::server::CEntityInstance_GetRefEHandle(this));
#endif
}

SchemaClassInfoData_t* CEntityInstance::GetSchemaClassInfo()
{
    DeclareVFuncIndex(CEntityInstance, GetDynamicBinding, offset);
#ifdef PLATFORM_WINDOWS
    SchemaClassInfoData_t* rv = nullptr;
    CALL_VIRTUAL(void, offset, this, &rv);
    return rv;
#else
    return CALL_VIRTUAL(SchemaClassInfoData_t*, offset, this);
#endif
}

void CEntityInstance::Kill()
{
    address::server::CEntityInstance_Kill(this);
}

bool CEntityInstance::IsPreservedEntity(const char* sClassname)
{
    // ReSharper disable StringLiteralTypo
    static constexpr std::array s_PreservedEntities =
    {
        "ai_network",
        "ai_hint",

        "cs_gamerules",
        "cs_team_manager",
        "cs_player_manager",
        "env_cubemap_fog",
        "env_soundscape",
        "env_soundscape_proxy",
        "env_soundscape_triggerable",
        "env_sky",
        "env_wind",
        "env_fog_controller",
        "env_tonemap_controller",
        "env_cascade_light",
        "env_combined_light_probe_volume",
        "func_brush",
        "func_wall",
        "func_buyzone",
        "func_illusionary",
        "func_hostage_rescue",
        "func_bomb_target",
        "info_projecteddecal",
        "info_node",
        "info_target",
        "info_node_hint",
        "info_player_counterterrorist",
        "info_player_terrorist",
        "info_enemy_terrorist_spawn",
        "info_deathmatch_spawn",
        "info_armsrace_counterterrorist",
        "info_armsrace_terrorist",
        "info_map_parameters",
        "keyframe_rope",
        "light_environment",
        "light_spot",
        "light_omni",
        "light_ortho",
        "move_rope",
        "info_ladder",
        "player",
        "cs_player_controller",
        "cs_bot",
        "point_camera",
        "scene_manager",
        "shadow_control",
        "sky_camera",
        "soundent",
        "trigger_soundscape",
        "viewmodel",
        "predicted_viewmodel",
        "csgo_viewmodel",
        "worldent",
        "point_devshot_camera",
        "logic_choreographed_scene",
        "info_bomb_target_hint_A",
        "info_bomb_target_hint_B",
        "info_hostage_rescue_zone_hint",
        "generic_actor",
        "vote_controller",
        "wearable_item",
        "point_hiding_spot",
        "game_coopmission_manager",
        "chicken",
        "global_chatter",
        "sky_camera",
        "player_spray_decal",
        "team_select_camera",
        "team_select_terrorist",
        "team_select_counterterrorist",
        "terrorist_team_intro_camera",
        "terrorist_wingman_intro_camera",
        "counterterrorist_team_intro_camera",
        "counterterrorist_wingman_intro_camera",
        "team_intro_terrorist",
        "team_intro_counterterrorist",
        "wingman_intro_terrorist",
        "wingman_intro_counterterrorist",
        "end_of_match_camera",
        "end_of_match_character",
        "observer",
        "point_script",
        "point_pulse",
};
    // ReSharper restore StringLiteralTypo

    return std::ranges::find(s_PreservedEntities, sClassname) != s_PreservedEntities.end();
}
