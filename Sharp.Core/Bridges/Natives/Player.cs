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

using System;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Core.Bridges.Natives;

public static unsafe partial class Player
{
    public static partial IntPtr ControllerGetPawn(IntPtr ptr);

    public static partial IntPtr ControllerFromEntity(IntPtr ptr);

    public static partial void ControllerPrint(IntPtr ptr,
        HudPrintChannel                               channel,
        string                                        message,
        string?                                       param1,
        string?                                       param2,
        string?                                       param3,
        string?                                       param4);

    public static partial void ControllerRoundRespawn(IntPtr ptr);

    public static partial void ControllerSwitchTeam(IntPtr ptr, CStrikeTeam team);

    public static partial IntPtr ControllerFindBySlot(long slot);

    public static partial void ControllerSetClanTag(IntPtr ptr, string tag);

    public static partial IntPtr ControllerGetItemInLoadoutFromInventory(IntPtr ptr, CStrikeTeam team, int slot);

    public static partial SoundOpEventGuid ControllerEmitSoundClient(IntPtr ptr, string sound, float?* volume);

    public static partial void ControllerCheckPawn(IntPtr ptr);

    public static partial void ControllerSetPawn(IntPtr ptr,
        IntPtr                                          pawnPtr,
        bool                                            unknown1,
        bool                                            unknown2,
        bool                                            unknown3,
        bool                                            unknown4);

    public static partial float ControllerGetLaggedMovement(IntPtr ptr);

    public static partial void ControllerSetLaggedMovement(IntPtr ptr, float val);

    public static partial IntPtr PawnGetController(IntPtr ptr);

    public static partial IntPtr PawnFromEntity(IntPtr ptr);

    public static partial void PawnPrint(IntPtr ptr,
        HudPrintChannel                         channel,
        string                                  message,
        string?                                 param1,
        string?                                 param2,
        string?                                 param3,
        string?                                 param4);

    public static partial void PawnSlay(IntPtr ptr, bool explode);

    public static partial IntPtr PawnGiveNamedItem(IntPtr ptr, string name);

    public static partial IntPtr ItemServicesGiveNamedItem(IntPtr ptr, string name, IntPtr viewPtr, bool force);

    public static partial void WeaponServicesEquipWeapon(IntPtr ptr, IntPtr weaponPtr);

    public static partial IntPtr PawnFindBySlot(long slot);

    public static partial IntPtr PawnGetActiveWeapon(IntPtr ptr);

    public static partial IntPtr PawnGetWeaponBySlot(IntPtr ptr, GearSlot slot, long unknown);

    public static partial void PawnRemovePlayerItem(IntPtr ptr, IntPtr entityPtr);

    public static partial void PawnRemoveAllItems(IntPtr ptr, bool removeSuit);

    public static partial void PawnDropWeapon(IntPtr ptr, IntPtr entityPtr);

    public static partial bool PawnSelectItem(IntPtr ptr, IntPtr entityPtr);

    public static partial bool PawnDetachWeapon(IntPtr ptr, IntPtr entityPtr);

    public static partial void PawnSwitchWeapon(IntPtr ptr, IntPtr entityPtr);

    public static partial void PawnSetDefaultGloves(IntPtr ptr, bool hide);

    public static partial void PawnGiveGloves(IntPtr ptr, int itemDefIndex, int prefab, float wear, int seed);

    public static partial SoundOpEventGuid PawnEmitSoundClient(IntPtr ptr, string sound, float?* volume);

    public static partial bool PawnIsPlayer(IntPtr ptr);
}
