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

namespace Sharp.Shared.Enums;

public enum WeaponGameplayAnimState : ushort
{
    Uninitialized                = 0,    // 0x0
    Dropped                      = 1,    // 0x1
    Holstered                    = 10,   // 0xA
    Deploy                       = 11,   // 0xB
    Idle                         = 50,   // 0x32
    ShootPrimary                 = 100,  // 0x64
    ShootSecondary               = 101,  // 0x65
    ShootDryFire                 = 102,  // 0x66
    Charge                       = 103,  // 0x67
    GrenadePullPin               = 200,  // 0xC8
    GrenadeReady                 = 201,  // 0xC9
    GrenadeThrow                 = 202,  // 0xCA
    C4Plant                      = 300,  // 0x12C
    HealthshotInject             = 400,  // 0x190
    KnifePrimaryHit              = 500,  // 0x1F4
    KnifePrimaryMiss             = 501,  // 0x1F5
    KnifeSecondaryHit            = 502,  // 0x1F6
    KnifeSecondaryMiss           = 503,  // 0x1F7
    KnifePrimaryStab             = 504,  // 0x1F8
    KnifeSecondaryStab           = 505,  // 0x1F9
    SilencerApply                = 600,  // 0x258
    SilencerRemove               = 601,  // 0x259
    Reload                       = 800,  // 0x320
    ReloadOutro                  = 801,  // 0x321
    Inspect                      = 1000, // 0x3E8
    InspectOutro                 = 1001, // 0x3E9
    InventoryUITumble            = 1500, // 0x5DC
    InventoryUIKeyChainApply     = 1501, // 0x5DD
    EndValid                     = 2000, // 0x7D0
    WeaponLegacyStateClearFiring = 2001, // 0x7D1
}
