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

using Sharp.Shared.Enums;

// ReSharper disable ConvertToConstant.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Sharp.Shared.Definition;

public static class ChatColor
{
    public const string White      = "\x01";
    public const string DarkRed    = "\x02";
    public const string Pink       = "\x03";
    public const string Green      = "\x04";
    public const string LightGreen = "\x05";
    public const string Lime       = "\x06";
    public const string Red        = "\x07";
    public const string Grey       = "\x08";
    public const string Yellow     = "\x09";
    public const string Gold       = "\x10";
    public const string Silver     = "\x0A";
    public const string Blue       = "\x0B";
    public const string DarkBlue   = "\x0C";
    public const string Purple     = "\x0E";
    public const string LightRed   = "\x0F";
    public const string Muted      = "\x0A";
    public const string Head       = "\x03";

    public const char NewLine = '\u2029';

    public static string GetColor(this CStrikeTeam team)
        => team switch
        {
            CStrikeTeam.CT => Blue,
            CStrikeTeam.TE => LightGreen,
            _              => White,
        };
}
