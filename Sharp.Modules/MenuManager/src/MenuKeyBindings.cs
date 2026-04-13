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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;

namespace Sharp.Modules.MenuManager.Core;

internal enum MenuBindingType
{
    Command,
    Button,
}

internal readonly struct MenuActionBinding
{
    public MenuBindingType     Type    { get; }
    public string?             Command { get; }
    public UserCommandButtons? Button  { get; }

    private MenuActionBinding(MenuBindingType type, string? command, UserCommandButtons? button)
    {
        Type    = type;
        Command = command;
        Button  = button;
    }

    public static MenuActionBinding FromCommand(string command)
        => new (MenuBindingType.Command, command, null);

    public static MenuActionBinding FromButton(UserCommandButtons button)
        => new (MenuBindingType.Button, null, button);

    /// <summary>
    ///     Returns the bind hint string for display in menus.
    ///     Command type: <c>{s:bind_autobuy}</c>, Button type: <c>{s:bind_sprint}</c>
    /// </summary>
    public string GetBindHint()
    {
        if (Type == MenuBindingType.Command)
        {
            return $"{{s:bind_{Command}}}";
        }

        return Button switch
        {
            UserCommandButtons.Attack       => "{s:bind_attack}",
            UserCommandButtons.Jump         => "{s:bind_jump}",
            UserCommandButtons.Duck         => "{s:bind_duck}",
            UserCommandButtons.Forward      => "{s:bind_forward}",
            UserCommandButtons.Back         => "{s:bind_back}",
            UserCommandButtons.Use          => "{s:bind_use}",
            UserCommandButtons.TurnLeft     => "{s:bind_left}",
            UserCommandButtons.TurnRight    => "{s:bind_right}",
            UserCommandButtons.MoveLeft     => "{s:bind_moveleft}",
            UserCommandButtons.MoveRight    => "{s:bind_moveright}",
            UserCommandButtons.Attack2      => "{s:bind_attack2}",
            UserCommandButtons.Reload       => "{s:bind_reload}",
            UserCommandButtons.Speed        => "{s:bind_sprint}",
            UserCommandButtons.Scoreboard   => "{s:bind_showscores}",
            UserCommandButtons.Zoom         => "{s:bind_zoom}",
            UserCommandButtons.LookAtWeapon => "{s:bind_lookatweapon}",
            _                               => string.Empty,
        };
    }

    public override string ToString()
        => Type == MenuBindingType.Command ? $"Command({Command})" : $"Button({Button})";
}

internal sealed class MenuKeyBindings
{
    public MenuActionBinding MoveUpCursor { get; private set; } = MenuActionBinding.FromCommand("autobuy");
    public MenuActionBinding MoveDownCursor { get; private set; } = MenuActionBinding.FromCommand("rebuy");
    public MenuActionBinding GoBack { get; private set; } = MenuActionBinding.FromButton(UserCommandButtons.Speed);
    public MenuActionBinding Confirm { get; private set; } = MenuActionBinding.FromButton(UserCommandButtons.LookAtWeapon);
    public MenuActionBinding Exit { get; private set; } = MenuActionBinding.FromButton(UserCommandButtons.Scoreboard);

    public static MenuKeyBindings Load(IConfiguration configuration, ILogger logger)
    {
        var bindings = new MenuKeyBindings();
        var section  = configuration.GetSection("MenuManager:KeyBindings");

        if (!section.Exists())
        {
            return bindings;
        }

        bindings.MoveUpCursor   = ParseBinding(section, "MoveUpCursor",   bindings.MoveUpCursor,   logger);
        bindings.MoveDownCursor = ParseBinding(section, "MoveDownCursor", bindings.MoveDownCursor, logger);
        bindings.GoBack         = ParseBinding(section, "GoBack",         bindings.GoBack,         logger);
        bindings.Confirm        = ParseBinding(section, "Confirm",        bindings.Confirm,        logger);
        bindings.Exit           = ParseBinding(section, "Exit",           bindings.Exit,           logger);

        logger.LogInformation(
            "MenuManager KeyBindings: MoveUp={MoveUp}, MoveDown={MoveDown}, GoBack={GoBack}, Confirm={Confirm}, Exit={Exit}",
            bindings.MoveUpCursor,
            bindings.MoveDownCursor,
            bindings.GoBack,
            bindings.Confirm,
            bindings.Exit);

        return bindings;
    }

    private static MenuActionBinding ParseBinding(IConfigurationSection parent,
        string                                                          key,
        MenuActionBinding                                               fallback,
        ILogger                                                         logger)
    {
        var section = parent.GetSection(key);

        if (!section.Exists())
        {
            return fallback;
        }

        var typeValue = section["Type"];

        if (string.IsNullOrWhiteSpace(typeValue))
        {
            logger.LogWarning("MenuManager KeyBindings: '{Key}' missing 'Type', using default {Default}", key, fallback);

            return fallback;
        }

        if (!TryParseBindingType(typeValue, out var type))
        {
            logger.LogWarning("MenuManager KeyBindings: '{Key}' has unknown type '{Type}', using default {Default}",
                              key,
                              typeValue,
                              fallback);

            return fallback;
        }

        switch (type)
        {
            case MenuBindingType.Command:
            {
                var command = section["Command"];

                if (string.IsNullOrWhiteSpace(command))
                {
                    logger.LogWarning(
                        "MenuManager KeyBindings: '{Key}' type is Command but missing 'Command' value, using default {Default}",
                        key,
                        fallback);

                    return fallback;
                }

                return MenuActionBinding.FromCommand(command);
            }
            case MenuBindingType.Button:
            {
                var buttonValue = section["Button"];

                if (string.IsNullOrWhiteSpace(buttonValue)
                    || !TryParseButton(buttonValue, out var button))
                {
                    logger.LogWarning(
                        "MenuManager KeyBindings: '{Key}' type is Button but has invalid or missing 'Button' value, using default {Default}",
                        key,
                        fallback);

                    return fallback;
                }

                return MenuActionBinding.FromButton(button);
            }
            default:
                logger.LogWarning("MenuManager KeyBindings: '{Key}' has unknown type '{Type}', using default {Default}",
                                  key,
                                  type,
                                  fallback);

                return fallback;
        }
    }

    private static bool TryParseBindingType(string value, out MenuBindingType type)
    {
        if (int.TryParse(value, out var numericType)
            && Enum.IsDefined(typeof(MenuBindingType), numericType))
        {
            type = (MenuBindingType) numericType;

            return true;
        }

        if (Enum.TryParse(value, true, out type)
            && Enum.IsDefined(type))
        {
            return true;
        }

        type = default;

        return false;
    }

    private static bool TryParseButton(string value, out UserCommandButtons button)
    {
        if (ulong.TryParse(value, out var numericButton)
            && Enum.IsDefined(typeof(UserCommandButtons), numericButton))
        {
            button = (UserCommandButtons) numericButton;

            return true;
        }

        if (Enum.TryParse(value, true, out button)
            && Enum.IsDefined(button))
        {
            return true;
        }

        button = default;

        return false;
    }

    public UserCommandButtons GetButtonMask()
    {
        UserCommandButtons mask = 0;

        if (MoveUpCursor.Type == MenuBindingType.Button)
        {
            mask |= MoveUpCursor.Button!.Value;
        }

        if (MoveDownCursor.Type == MenuBindingType.Button)
        {
            mask |= MoveDownCursor.Button!.Value;
        }

        if (GoBack.Type == MenuBindingType.Button)
        {
            mask |= GoBack.Button!.Value;
        }

        if (Confirm.Type == MenuBindingType.Button)
        {
            mask |= Confirm.Button!.Value;
        }

        if (Exit.Type == MenuBindingType.Button)
        {
            mask |= Exit.Button!.Value;
        }

        return mask;
    }
}
