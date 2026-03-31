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

using System.Text;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Services.Internal;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Common;

internal sealed class CommandContext
{
    private readonly InterfaceBridge _bridge;
    private readonly ModuleContext   _moduleContext;
    private readonly IGameClient?    _issuer;
    private readonly StringCommand   _command;
    private readonly ILogger         _logger;

    private const string DefaultReasonKey      = "Admin.Reason.Default";
    private const string DefaultReasonFallback = "No reason provided";

    public CommandContext(InterfaceBridge bridge,
        ModuleContext                     moduleContext,
        IGameClient?                      issuer,
        StringCommand                     command,
        ILogger                           logger)
    {
        _bridge        = bridge;
        _moduleContext = moduleContext;
        _issuer        = issuer;
        _command       = command;
        _logger        = logger;
    }

    /// <summary>
    ///     Checks if the command has at least the specified number of arguments.
    ///     If not, replies with the specified error message and returns false.
    /// </summary>
    public bool RequireArgs(int minimum, string key, string fallback)
    {
        if (_command.ArgCount >= minimum)
        {
            return true;
        }

        ReplyKey(key, fallback);

        return false;
    }

    /// <summary>
    ///     Tries to resolve one or more targets from the argument at the specified index.
    ///     Handles targeting logic (e.g., @ct, @all, steamid) and replies with errors if no targets or invalid targets are
    ///     found.
    /// </summary>
    public bool TryGetTargets(int argIndex, out IReadOnlyList<IGameClient> targets, out string targetLabel)
    {
        targets     = [];
        targetLabel = string.Empty;

        if (_command.ArgCount < argIndex)
        {
            ReplyKey("Admin.TargetRequired", "Target is required.");

            return false;
        }

        var rawTarget = _command.GetArg(argIndex);
        targetLabel = rawTarget;

        var matches = ResolveTargets(rawTarget).ToList();

        if (matches.Count == 0)
        {
            ReplyKey("Admin.TargetNone", "No player found matching '{0}'.", rawTarget);

            return false;
        }

        if (matches.Count == 1)
        {
            targetLabel = matches[0].Name;
        }

        targets = matches;

        return true;
    }

    /// <summary>
    ///     Tries to resolve a single target from the argument at the specified index.
    ///     Replies with an error if no target is found or if multiple targets match.
    /// </summary>
    public bool TryGetSingleTarget(int argIndex, out IGameClient target)
    {
        target = null!;

        if (_command.ArgCount < argIndex)
        {
            ReplyKey("Admin.TargetRequired", "Target is required.");

            return false;
        }

        var rawTarget = _command.GetArg(argIndex);
        var matches   = ResolveTargets(rawTarget).Take(2).ToList();

        if (matches.Count == 0)
        {
            ReplyKey("Admin.TargetNone", "No player found matching '{0}'.", rawTarget);

            return false;
        }

        if (matches.Count > 1)
        {
            ReplyKey("Admin.TargetMultiple", "Multiple players matched '{0}'. Please be more specific.", rawTarget);

            return false;
        }

        target = matches[0];

        return true;
    }

    /// <summary>
    ///     Tries to parse a duration string (e.g., "5m", "1h", "1y") from the argument at the specified index.
    ///     If no suffix is provided (e.g. "10"), it defaults to minutes.
    ///     Returns null for permanent/zero duration. Replies with an error if the format is invalid.
    /// </summary>
    public bool TryParseDuration(int argIndex, out TimeSpan? duration)
    {
        duration = null;

        if (_command.ArgCount < argIndex)
        {
            return true;
        }

        var token = _command.GetArg(argIndex);

        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        var parsed = ParseDurationToken(token);

        if (parsed is null && !token.Equals("0", StringComparison.OrdinalIgnoreCase))
        {
            ReplyKey("Admin.InvalidDuration", "Invalid duration. Use formats like 5m, 2h, 1d, 1w, 1y or 0 for permanent.");

            return false;
        }

        duration = parsed;

        return true;
    }

    /// <summary>
    ///     Tries to parse an on/off state from the argument at the specified index.
    ///     Returns null if the argument is missing (implying a toggle).
    ///     Returns true/false if explicitly set.
    ///     Replies with error and returns false if the argument is present but invalid.
    /// </summary>
    public bool TryGetState(int argIndex, out bool? state)
    {
        state = null;

        // If argument is missing, we consider this valid (it means "Toggle")
        if (_command.ArgCount < argIndex)
        {
            return true;
        }

        var arg = _command.GetArg(argIndex).ToLower();

        switch (arg)
        {
            case "on":
            case "1":
            case "true":
            case "enable":
                state = true;

                return true;
            case "off":
            case "0":
            case "false":
            case "disable":
                state = false;

                return true;
            default:
                ReplyKey("Admin.InvalidState", "Invalid state '{0}'. Use 'on' or 'off'.", arg);

                return false;
        }
    }

    /// <summary>
    ///     Gets the reason string from the specified argument index, or returns the default reason if not provided.
    /// </summary>
    public string GetReason(int argIndex)
    {
        var argCount = _command.ArgCount;

        if (argIndex > argCount)
        {
            return LocalizeOrFallback(DefaultReasonKey, DefaultReasonFallback);
        }

        if (argIndex == argCount)
        {
            return _command.GetArg(argIndex);
        }

        var sb = new StringBuilder();

        for (var i = argIndex; i <= _command.ArgCount; i++)
        {
            sb.Append(_command.GetArg(i));

            if (i < argCount)
            {
                sb.Append(' ');
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Replies to the command issuer with a localized message.
    ///     Use this for errors, warnings, or information intended only for the caller.
    /// </summary>
    public void ReplyKey(string key, string fallback, params ReadOnlySpan<object?> args)
        => ReplyKeyInternal(key, fallback, args, false);

    /// <summary>
    ///     Replies to the command issuer with a localized message, and optionally broadcasts it to the server
    ///     if the module configuration allows it (e.g., based on immunity levels).
    ///     Use this for successful administrative actions (e.g., banning, kicking, changing maps).
    /// </summary>
    public void ReplySuccessKey(string key, string fallback, params ReadOnlySpan<object?> args)
        => ReplyKeyInternal(key, fallback, args, true);

    /// <summary>
    ///     Replies to the command issuer with a raw message (no localization).
    ///     Prefer using <see cref="ReplyKey" /> when possible.
    /// </summary>
    public void Reply(string message)
        => ReplyInternal(message, false);

    /// <summary>
    ///     Replies to the command issuer with a raw message, and optionally broadcasts it to the server.
    ///     Prefer using <see cref="ReplySuccessKey" /> when possible.
    /// </summary>
    public void ReplySuccess(string message)
        => ReplyInternal(message, true);

    public string IssuerName => _issuer is null ? "Console" : _issuer.Name;

    private bool ShouldBroadcast
    {
        get
        {
            var mode = _moduleContext.BroadcastType;

            if (mode <= 0)
            {
                return false;
            }

            if (_issuer is null)
            {
                return true;
            }

            if (mode == 1)
            {
                return true;
            }

            // mode == 2: obey max immunity
            var maxAllowed = _moduleContext.BroadcastMaxImmunity;
            var immunity   = GetIssuerImmunity();

            return immunity <= maxAllowed;
        }
    }

    private void ReplyKeyInternal(string key, string fallback, ReadOnlySpan<object?> args, bool broadcast)
    {
        if (broadcast && ShouldBroadcast)
        {
            BroadcastKey(key, fallback, args);

            return;
        }

        ReplyInternal(LocalizeOrFallback(key, fallback, args), false);
    }

    private void ReplyInternal(string message, bool broadcast)
    {
        if (broadcast && ShouldBroadcast)
        {
            BroadcastLiteral(message);

            return;
        }

        if (_issuer is null)
        {
            _logger.LogInformation(message);

            return;
        }

        const string prefix = "[MS] ";

        _issuer.GetPlayerController()?.Print(HudPrintChannel.Chat, prefix + message);
    }

    private int GetIssuerImmunity()
    {
        if (_moduleContext.AdminManager is not { } adminManager)
        {
            return 0; // Without admin manager, do not block broadcasts.
        }

        // should neve reach here
        if (_issuer is null)
        {
            return byte.MaxValue;
        }

        return adminManager.GetAdmin(_issuer.SteamId)?.Immunity ?? 0;
    }

    private void BroadcastKey(string key, string fallback, ReadOnlySpan<object?> args)
    {
        if (_moduleContext.LocalizerManager is { } localizer)
        {
            try
            {
                var clients = _bridge.ClientManager.GetGameClients(true);

                localizer.ForMany(clients)
                         .Localized(key, args)
                         .Print();

                if (_issuer is null)
                {
                    _logger.LogInformation(LocalizeOrFallback(key, fallback, args));
                }

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast localized message for key {Key}", key);
            }
        }

        BroadcastLiteral(LocalizeOrFallback(key, fallback, args));
    }

    private void BroadcastLiteral(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        _bridge.ModSharp.PrintToChatAll(message);

        if (_issuer is null)
        {
            _logger.LogInformation(message);
        }
    }

    private IEnumerable<IGameClient> ResolveTargets(string rawTarget)
    {
        if (_moduleContext.TargetingManager is { } targetingManager)
        {
            return targetingManager.GetByTarget(_issuer, rawTarget);
        }

        ReplyKey("Admin.TargetingMissing", "Targeting module is not installed.");

        return [];
    }

    private static TimeSpan? ParseDurationToken(string token)
    {
        var span = token.AsSpan().Trim();

        if (span.Length == 0 || span is "0")
        {
            return null;
        }

        var suffix     = 'm';
        var numberSpan = span;
        var lastChar   = span[^1];

        if (!char.IsAsciiDigit(lastChar))
        {
            suffix     = char.ToLowerInvariant(lastChar);
            numberSpan = span[..^1];
        }

        if (!int.TryParse(numberSpan, out var amount) || amount <= 0)
        {
            return null;
        }

        return suffix switch
        {
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            'd' => TimeSpan.FromDays(amount),
            'w' => TimeSpan.FromDays(amount * 7),
            'y' => TimeSpan.FromDays(amount * 365),
            _   => TimeSpan.FromMinutes(amount),
        };
    }

    private string LocalizeOrFallback(string key, string fallback, params ReadOnlySpan<object?> args)
    {
        if (_moduleContext.LocalizerManager is { } localizer && _issuer is not null)
        {
            try
            {
                var locale = localizer.For(_issuer);

                if (locale.TryText(key, out var text, args))
                {
                    return text;
                }
            }
            catch
            {
                // ignored
            }
        }

        try
        {
            return args.Length > 0 ? string.Format(fallback, args) : fallback;
        }
        catch (FormatException)
        {
            return fallback;
        }
    }
}
