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
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sharp.Modules.MenuManager.Core;

internal static class MenuColor
{
    public static string KeyColor      = "#DDAA11";
    public static string TextColor     = "#ffffff";
    public static string DisabledColor = "#888888";
    public static string CursorColor   = "#3399FF";

    public static void Load(IConfiguration configuration, ILogger logger)
    {
        var section = configuration.GetSection("MenuManager:Colors");

        if (!section.Exists())
        {
            return;
        }

        KeyColor      = ParseColor(section, logger, "Key",      "#DDAA11");
        TextColor     = ParseColor(section, logger, "Text",     "#ffffff");
        DisabledColor = ParseColor(section, logger, "Disabled", "#888888");
        CursorColor   = ParseColor(section, logger, "Cursor",   "#3399FF");

        logger.LogInformation(
            "MenuManager Colors: KeyColor={KeyColor}, TextColor={TextColor}, DisabledColor={DisabledColor}, CursorColor={CursorColor}",
            KeyColor,
            TextColor,
            DisabledColor,
            CursorColor);
    }

    private static string ParseColor(IConfiguration parent, ILogger logger, string key, string fallback)
    {
        var section = parent.GetSection(key);

        if (!section.Exists())
        {
            return fallback;
        }

        var value = section.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (!value.StartsWith('#'))
        {
            value = $"#{value}";
        }

        if (!IsValidHexColor(value))
        {
            logger.LogWarning("Invalid color value detected for {key}: {value}", key, value);

            return fallback;
        }

        return value;
    }

    private static bool IsValidHexColor(string value)
    {
        // 4 (#RGB), 5 (#RGBA), 7 (#RRGGBB), 9 (#RRGGBBAA)
        if (value.Length is not (4 or 5 or 7 or 9))
        {
            return false;
        }

        return uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }
}
