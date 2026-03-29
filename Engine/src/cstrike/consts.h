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

#ifndef CSTRIKE_ROOT_CONST_H
#define CSTRIKE_ROOT_CONST_H

#include <cstdint>

enum HudPrint_t : uint32_t
{
    Hud_Console = 2,
    Hud_Chat,
    Hud_Hint,
    Hud_SayText,
    Hud_Center,
};

enum SolidType_t : uint8_t
{
    SOLID_NONE     = 0, // no solid model
    SOLID_BSP      = 1, // a BSP tree
    SOLID_BBOX     = 2, // an AABB
    SOLID_OBB      = 3, // an OBB (not implemented yet)
    SOLID_SPHERE   = 4, // a SPHERE
    SOLID_POINT    = 5, // a Point
    SOLID_VPHYSICS = 6, // solid vphysics object, get vcollide from the model and collide with that
    SOLID_CAPSULE  = 7, // a capsule
    SOLID_LAST,
};

enum SolidFlags_t
{
    FSOLID_CUSTOMRAYTEST = 0x0001,        // Ignore solid type + always call into the entity for ray tests
    FSOLID_CUSTOMBOXTEST = 0x0002,        // Ignore solid type + always call into the entity for swept box tests
    FSOLID_NOT_SOLID     = 0x0004,        // Are we currently not solid?
    FSOLID_TRIGGER       = 0x0008,        // This is something may be collideable but fires touch functions
                                          // even when it's not collideable (when the FSOLID_NOT_SOLID flag is set)
    FSOLID_NOT_STANDABLE        = 0x0010, // You can't stand on this
    FSOLID_VOLUME_CONTENTS      = 0x0020, // Contains volumetric contents (like water)
    FSOLID_FORCE_WORLD_ALIGNED  = 0x0040, // Forces the collision rep to be world-aligned even if it's SOLID_BSP or SOLID_VPHYSICS
    FSOLID_USE_TRIGGER_BOUNDS   = 0x0080, // Uses a special trigger bounds separate from the normal OBB
    FSOLID_ROOT_PARENT_ALIGNED  = 0x0100, // Collisions are defined in root parent's local coordinate space
    FSOLID_TRIGGER_TOUCH_DEBRIS = 0x0200, // This trigger will touch debris objects
    FSOLID_TRIGGER_TOUCH_PLAYER = 0x0400, // This trigger will touch only players
    FSOLID_NOT_MOVEABLE         = 0x0800, // Assume this object will not move

    FSOLID_MAX_BITS = 12
};

enum RenderMode_t
{
    kRenderNormal = 0, // src
    kRenderTransAlpha, // src*srca+dest*(1-srca)
    kRenderNone,       // Don't render.
    kRenderModeCount, // must be last
};

enum lifeState_t
{
    LIFE_ALIVE,
    LIFE_DYING,
    LIFE_DEAD,
    LIFE_RESPAWNABLE,
    LIFE_RESPAWNING
};

enum MoveCollide_t : uint8_t
{
    MOVECOLLIDE_DEFAULT    = 0x0,
    MOVECOLLIDE_FLY_BOUNCE = 0x1,
    MOVECOLLIDE_FLY_CUSTOM = 0x2,
    MOVECOLLIDE_FLY_SLIDE  = 0x3,
    MOVECOLLIDE_COUNT      = 0x4,
    MOVECOLLIDE_MAX_BITS   = 0x3,
};

enum CStrikeTeam_t : uint8_t
{
    TEAM_UNASSIGNED = 0,
    TEAM_SPECTATOR,
    TEAM_TE,
    TEAM_CT
};

enum MoveType_t : uint8_t
{
    MOVETYPE_NONE = 0,   // never moves
    MOVETYPE_OBSOLETE,   // For players -- in TF2 commander view, etc.
    MOVETYPE_WALK,       // Player only - moving on the ground
    MOVETYPE_FLY,        // No gravity, but still collides with stuff
    MOVETYPE_FLYGRAVITY, // flies through the air + is affected by gravity
    MOVETYPE_VPHYSICS,   // uses VPHYSICS for simulation
    MOVETYPE_PUSH,       // no clip to world, push and crush
    MOVETYPE_NOCLIP,     // No gravity, no collisions, still do velocity/avelocity
    MOVETYPE_OBSERVER,   // Observer movement, depends on player's observer mode
    MOVETYPE_LADDER,     // Used by players only when going onto a ladder
    MOVETYPE_CUSTOM,     // Allows the entity to describe its own physics

    // should always be defined as the last item in the list
    MOVETYPE_LAST     = 0xB,
    MOVETYPE_INVALID  = 0xB,
    MOVETYPE_MAX_BITS = 0x5
};

enum ObserverMode_t
{
    OBS_MODE_NONE      = 0x0,
    OBS_MODE_FIXED     = 0x1,
    OBS_MODE_IN_EYE    = 0x2,
    OBS_MODE_CHASE     = 0x3,
    OBS_MODE_ROAMING   = 0x4,
    OBS_MODE_DIRECTED  = 0x5,
    NUM_OBSERVER_MODES = 0x6,
};

enum class WeaponGameplayAnimState : uint16_t
{
    WPN_ANIMSTATE_UNINITIALIZED        = 0,    // 0x0
    WPN_ANIMSTATE_DROPPED              = 1,    // 0x1
    WPN_ANIMSTATE_HOLSTERED            = 10,   // 0xA
    WPN_ANIMSTATE_DEPLOY               = 11,   // 0xB
    WPN_ANIMSTATE_IDLE                 = 50,   // 0x32
    WPN_ANIMSTATE_SHOOT_PRIMARY        = 100,  // 0x64
    WPN_ANIMSTATE_SHOOT_SECONDARY      = 101,  // 0x65
    WPN_ANIMSTATE_SHOOT_DRYFIRE        = 102,  // 0x66
    WPN_ANIMSTATE_CHARGE               = 103,  // 0x67
    WPN_ANIMSTATE_GRENADE_PULL_PIN     = 200,  // 0xC8
    WPN_ANIMSTATE_GRENADE_READY        = 201,  // 0xC9
    WPN_ANIMSTATE_GRENADE_THROW        = 202,  // 0xCA
    WPN_ANIMSTATE_C4_PLANT             = 300,  // 0x12C
    WPN_ANIMSTATE_HEALTHSHOT_INJECT    = 400,  // 0x190
    WPN_ANIMSTATE_KNIFE_PRIMARY_HIT    = 500,  // 0x1F4
    WPN_ANIMSTATE_KNIFE_PRIMARY_MISS   = 501,  // 0x1F5
    WPN_ANIMSTATE_KNIFE_SECONDARY_HIT  = 502,  // 0x1F6
    WPN_ANIMSTATE_KNIFE_SECONDARY_MISS = 503,  // 0x1F7
    WPN_ANIMSTATE_KNIFE_PRIMARY_STAB   = 504,  // 0x1F8
    WPN_ANIMSTATE_KNIFE_SECONDARY_STAB = 505,  // 0x1F9
    WPN_ANIMSTATE_SILENCER_APPLY       = 600,  // 0x258
    WPN_ANIMSTATE_SILENCER_REMOVE      = 601,  // 0x259
    WPN_ANIMSTATE_RELOAD               = 800,  // 0x320
    WPN_ANIMSTATE_RELOAD_OUTRO         = 801,  // 0x321
    WPN_ANIMSTATE_INSPECT              = 1000, // 0x3E8
    WPN_ANIMSTATE_INSPECT_OUTRO        = 1001, // 0x3E9
    WPN_ANIMSTATE_END_VALID            = 1100, // 0x44C
    WEAPON_LEGACY_STATE_CLEAR_FIRING   = 1101  // 0x44D
};

enum class ItemFlagTypes_t : uint8_t
{
    ITEM_FLAG_NONE                    = 0x0,
    ITEM_FLAG_CAN_SELECT_WITHOUT_AMMO = 0x1,
    ITEM_FLAG_NOAUTORELOAD            = 0x2,
    ITEM_FLAG_NOAUTOSWITCHEMPTY       = 0x4,
    ITEM_FLAG_LIMITINWORLD            = 0x8,
    ITEM_FLAG_EXHAUSTIBLE             = 0x10,
    ITEM_FLAG_DOHITLOCATIONDMG        = 0x20,
    ITEM_FLAG_NOAMMOPICKUPS           = 0x40,
    ITEM_FLAG_NOITEMPICKUP            = 0x80,
};

enum class GearSlot_t : int32_t
{
    GEAR_SLOT_INVALID         = -1,
    GEAR_SLOT_RIFLE           = 0x0,
    GEAR_SLOT_PISTOL          = 0x1,
    GEAR_SLOT_KNIFE           = 0x2,
    GEAR_SLOT_GRENADES        = 0x3,
    GEAR_SLOT_C4              = 0x4,
    GEAR_SLOT_RESERVED_SLOT6  = 0x5,
    GEAR_SLOT_RESERVED_SLOT7  = 0x6,
    GEAR_SLOT_RESERVED_SLOT8  = 0x7,
    GEAR_SLOT_RESERVED_SLOT9  = 0x8,
    GEAR_SLOT_RESERVED_SLOT10 = 0x9,
    GEAR_SLOT_RESERVED_SLOT11 = 0xa,
    GEAR_SLOT_BOOSTS          = 0xb,
    GEAR_SLOT_UTILITY         = 0xc,
    GEAR_SLOT_COUNT           = 0xd,
    GEAR_SLOT_FIRST           = 0x0,
    GEAR_SLOT_LAST            = 0xc,
};

enum EntityFlags_t : uint32_t
{
    FL_ONGROUND              = (1 << 0),
    FL_DUCKING               = (1 << 1),
    FL_WATERJUMP             = (1 << 2),
    FL_BOT                   = (1 << 4),
    FL_FROZEN                = (1 << 5),
    FL_ATCONTROLS            = (1 << 6),
    FL_CLIENT                = (1 << 7),
    FL_FAKECLIENT            = (1 << 8),
    FL_FLY                   = (1 << 10),
    FL_SUPPRESS_SAVE         = (1 << 11),
    FL_IN_VEHICLE            = (1 << 12),
    FL_IN_VEHICLE_TRANSPORT  = (1 << 13),
    FL_GODMODE               = (1 << 14),
    FL_NOTARGET              = (1 << 15),
    FL_AIMTARGET             = (1 << 16),
    FL_STATICPROP            = (1 << 18),
    FL_GRENADE               = (1 << 20),
    FL_DONTTOUCH             = (1 << 22),
    FL_BASEVELOCITY          = (1 << 23),
    FL_CONVEYOR              = (1 << 24),
    FL_OBJECT                = (1 << 25),
    FL_ONFIRE                = (1 << 27),
    FL_DISSOLVING            = (1 << 28),
    FL_TRANSRAGDOLL          = (1 << 29),
    FL_UNBLOCKABLE_BY_PLAYER = (1 << 30)
};

enum ParticleAttachment_t : uint32_t
{
    PATTACH_INVALID   = 0xFFFFFFFF, // Invalid attachment type
    PATTACH_ABSORIGIN = 0,          // Create at absorigin, but don't follow
    PATTACH_ABSORIGIN_FOLLOW,       // Create at absorigin, and update to follow the entity
    PATTACH_CUSTOMORIGIN,           // Create at a custom origin, but don't follow
    PATTACH_CUSTOMORIGIN_FOLLOW,    // Create at a custom origin, follow relative position to specified entity
    PATTACH_POINT,                  // Create on attachment point, but don't follow
    PATTACH_POINT_FOLLOW,           // Create on attachment point, and update to follow the entity
    PATTACH_EYES_FOLLOW,            // Create on eyes of the attached entity, and update to follow the entity
    PATTACH_OVERHEAD_FOLLOW,        // Create at the top of the entity's bbox
    PATTACH_WORLDORIGIN,            // Used for control points that don't attach to an entity
    PATTACH_ROOTBONE_FOLLOW,        // Create at the root bone of the entity, and update to follow
    PATTACH_RENDERORIGIN_FOLLOW,
    PATTACH_MAIN_VIEW,
    PATTACH_WATERWAKE,
    PATTACH_CENTER_FOLLOW,
    PATTACH_CUSTOM_GAME_STATE_1,
    PATTACH_HEALTHBAR,
    MAX_PATTACH_TYPES,
};

#endif