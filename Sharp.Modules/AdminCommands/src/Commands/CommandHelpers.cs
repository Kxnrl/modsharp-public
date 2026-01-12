using System.Globalization;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal static class CommandHelpers
{
    private static readonly char[] VectorSeparators = [',', ' '];

    public static string GetRemainingArgs(in StringCommand command, int startIndex)
    {
        if (command.ArgCount < startIndex)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();

        for (var i = startIndex; i <= command.ArgCount; i++)
        {
            if (i > startIndex)
            {
                sb.Append(' ');
            }

            sb.Append(command.GetArg(i));
        }

        return sb.ToString();
    }

    public static bool TryParseVector(StringCommand command, int startIndex, out Vector vector)
    {
        vector = default;

        // three separate args (startIndex, startIndex+1, startIndex+2)
        if (command.ArgCount >= startIndex + 2)
        {
            if (float.TryParse(command.GetArg(startIndex),        NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                && float.TryParse(command.GetArg(startIndex + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                && float.TryParse(command.GetArg(startIndex + 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
            {
                vector = new Vector(x, y, z);

                return true;
            }
        }
        else if (command.ArgCount >= startIndex)
        {
            // single token with delimiters: "x,y,z" or "x y z"
            var token = command.GetArg(startIndex).AsSpan();
            
            // Fast path for simple parsing without Split
            var sep1 = token.IndexOfAny(VectorSeparators);
            if (sep1 > 0)
            {
                var part1 = token[..sep1];
                var remainder = token[(sep1 + 1)..];
                
                // Skip consecutive separators if any (though Split(RemoveEmptyEntries) handled this, manual parsing needs care)
                // For simplicity and performance on well-formed inputs:
                while (remainder.Length > 0 && (remainder[0] == ',' || remainder[0] == ' '))
                {
                    remainder = remainder[1..];
                }

                var sep2 = remainder.IndexOfAny(VectorSeparators);
                if (sep2 > 0)
                {
                    var part2 = remainder[..sep2];
                    var part3 = remainder[(sep2 + 1)..];
                    
                    // Trim part3 potentially
                    while (part3.Length > 0 && (part3[0] == ',' || part3[0] == ' '))
                    {
                        part3 = part3[1..];
                    }

                    if (float.TryParse(part1, NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                        && float.TryParse(part2, NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                        && float.TryParse(part3, NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                    {
                        vector = new Vector(x, y, z);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static string FormatVector(Vector v)
        => $"{v.X:0.##}, {v.Y:0.##}, {v.Z:0.##}";

    public static bool TryParseTeam(string raw, out CStrikeTeam team)
    {
        if (string.Equals(raw, "t", StringComparison.OrdinalIgnoreCase) 
            || string.Equals(raw, "te", StringComparison.OrdinalIgnoreCase) 
            || string.Equals(raw, "terrorist", StringComparison.OrdinalIgnoreCase))
        {
            team = CStrikeTeam.TE;
            return true;
        }

        if (string.Equals(raw, "ct", StringComparison.OrdinalIgnoreCase) 
            || string.Equals(raw, "counter", StringComparison.OrdinalIgnoreCase) 
            || string.Equals(raw, "c", StringComparison.OrdinalIgnoreCase))
        {
            team = CStrikeTeam.CT;
            return true;
        }

        if (string.Equals(raw, "spec", StringComparison.OrdinalIgnoreCase) 
            || string.Equals(raw, "spectator", StringComparison.OrdinalIgnoreCase) 
            || string.Equals(raw, "s", StringComparison.OrdinalIgnoreCase))
        {
            team = CStrikeTeam.Spectator;
            return true;
        }

        if (byte.TryParse(raw, out var parsed) && Enum.IsDefined(typeof(CStrikeTeam), parsed))
        {
            team = (CStrikeTeam)parsed;
            return true;
        }

        team = CStrikeTeam.UnAssigned;
        return false;
    }

    public static bool TryGetPawn(IGameClient     target,
                                  out IPlayerPawn pawn,
                                  bool            requireAlive = false)
    {
        pawn = null!;

        if (target.GetPlayerController()?.GetPlayerPawn() is not { } playerPawn)
        {
            return false;
        }

        if (requireAlive && !playerPawn.IsAlive)
        {
            return false;
        }

        pawn = playerPawn;

        return true;
    }

    public static bool ShouldEnable(StringCommand command, int index, bool current)
    {
        if (command.ArgCount < index)
        {
            return !current;
        }

        var token = command.GetArg(index).Trim().ToLowerInvariant();

        return token switch
        {
            "on" or "1" or "true" or "enable" or "enabled"     => true,
            "off" or "0" or "false" or "disable" or "disabled" => false,
            _                                                  => !current,
        };
    }
}
