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

namespace Sharp.Shared.Enums;

public enum LoadoutSlot : uint
{
    Promoted             = unchecked((uint) -2),
    Invalid              = unchecked((uint) -1),
    Melee                = 0x0,
    C4                   = 0x1,
    Secondary0           = 0x2,
    Secondary1           = 0x3,
    Secondary2           = 0x4,
    Secondary3           = 0x5,
    Secondary4           = 0x6,
    Secondary5           = 0x7,
    Smg0                 = 0x8,
    Smg1                 = 0x9,
    Smg2                 = 0xA,
    Smg3                 = 0xB,
    Smg4                 = 0xC,
    Smg5                 = 0xD,
    Rifle0               = 0xE,
    Rifle1               = 0xF,
    Rifle2               = 0x10,
    Rifle3               = 0x11,
    Rifle4               = 0x12,
    Rifle5               = 0x13,
    Heavy0               = 0x14,
    Heavy1               = 0x15,
    Heavy2               = 0x16,
    Heavy3               = 0x17,
    Heavy4               = 0x18,
    Heavy5               = 0x19,
    Grenade0             = 0x1A,
    Grenade1             = 0x1B,
    Grenade2             = 0x1C,
    Grenade3             = 0x1D,
    Grenade4             = 0x1E,
    Grenade5             = 0x1F,
    Equipment0           = 0x20,
    Equipment1           = 0x21,
    Equipment2           = 0x22,
    Equipment3           = 0x23,
    Equipment4           = 0x24,
    Equipment5           = 0x25,
    ClothingCustomPlayer = 0x26,
    ClothingCustomHead   = 0x27,
    ClothingFacemask     = 0x28,
    ClothingHands        = 0x29,
    ClothingEyewear      = 0x2A,
    ClothingHat          = 0x2B,
    ClothingLowerBody    = 0x2C,
    ClothingTorso        = 0x2D,
    ClothingAppearance   = 0x2E,
    Misc0                = 0x2F,
    Misc1                = 0x30,
    Misc2                = 0x31,
    Misc3                = 0x32,
    Misc4                = 0x33,
    Misc5                = 0x34,
    Misc6                = 0x35,
    MusicKit             = 0x36,
    Flair0               = 0x37,
    Spray0               = 0x38,
    Pet                  = 0x39,
}
