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

public enum RenderFx : byte
{
    None           = 0x0,
    PulseSlow      = 0x1,
    PulseFast      = 0x2,
    PulseSlowWide  = 0x3,
    PulseFastWide  = 0x4,
    FadeSlow       = 0x5,
    FadeFast       = 0x6,
    SolidSlow      = 0x7,
    SolidFast      = 0x8,
    StrobeSlow     = 0x9,
    StrobeFast     = 0xA,
    StrobeFaster   = 0xB,
    FlickerSlow    = 0xC,
    FlickerFast    = 0xD,
    NoDissipation  = 0xE,
    FadeOut        = 0xF,
    FadeIn         = 0x10,
    PulseFastWider = 0x11,
    GlowShell      = 0x12,
}
