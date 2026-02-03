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

        // Three separate arguments (e.g., "100" "50" "25")
        if (command.ArgCount >= startIndex + 2)
        {
            return TryParseRaw(command.GetArg(startIndex),
                               command.GetArg(startIndex + 1),
                               command.GetArg(startIndex + 2),
                               out vector);
        }

        //  One argument with delimiters (e.g., "100,50,25")
        if (command.ArgCount >= startIndex)
        {
            var arg   = command.GetArg(startIndex);
            var parts = arg.Split(VectorSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 3)
            {
                return TryParseRaw(parts[0], parts[1], parts[2], out vector);
            }
        }

        return false;
    }

    private static bool TryParseRaw(string sX, string sY, string sZ, out Vector vec)
    {
        vec = default;
        const NumberStyles style   = NumberStyles.Float;
        var                culture = CultureInfo.InvariantCulture;

        if (float.TryParse(sX,    style, culture, out var x)
            && float.TryParse(sY, style, culture, out var y)
            && float.TryParse(sZ, style, culture, out var z))
        {
            vec = new Vector(x, y, z);

            return true;
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
}
