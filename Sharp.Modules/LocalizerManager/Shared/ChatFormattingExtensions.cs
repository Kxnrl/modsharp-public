using System;
using System.Collections.Generic;
using System.Linq;
using Sharp.Shared.Definition;

namespace Sharp.Modules.LocalizerManager.Shared;

public static class ChatFormattingExtensions
{
    private static readonly Dictionary<string, string> ColorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "{normal}", ChatColor.White },
        { "{default}", ChatColor.White },
        { "{white}", ChatColor.White },
        { "{darkred}", ChatColor.DarkRed },
        { "{pink}", ChatColor.Pink },
        { "{green}", ChatColor.Green },
        { "{lightgreen}", ChatColor.LightGreen },
        { "{lime}", ChatColor.Lime },
        { "{red}", ChatColor.Red },
        { "{grey}", ChatColor.Grey },
        { "{gray}", ChatColor.Grey },
        { "{yellow}", ChatColor.Yellow },
        { "{gold}", ChatColor.Gold },
        { "{silver}", ChatColor.Silver },
        { "{blue}", ChatColor.Blue },
        { "{darkblue}", ChatColor.DarkBlue },
        { "{purple}", ChatColor.Purple },
        { "{lightred}", ChatColor.LightRed },
        { "{muted}", ChatColor.Muted },
        { "{head}", ChatColor.Head },
        { "{whitespace}", "\u00A0" }
    };

    public static string ProcessChatColors(this string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        var result = message;

        foreach (var (placeholder, code) in ColorMap)
        {
            result = result.Replace(placeholder, code, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    public static string StripChatColors(this string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        var result = ColorMap.Keys.Aggregate(message, (current, placeholder) => current.Replace(placeholder, string.Empty, StringComparison.OrdinalIgnoreCase));

        // Remove control codes
        var controlCodes = new[]
        {
            "\x01", "\x02", "\x03", "\x04", "\x05", "\x06", "\x07", "\x08",
            "\x09", "\x0A", "\x0B", "\x0C", "\x0D", "\x0E", "\x0F", "\x10"
        };

        foreach (var code in controlCodes)
        {
            result = result.Replace(code, string.Empty, StringComparison.Ordinal);
        }

        return result;
    }
}
