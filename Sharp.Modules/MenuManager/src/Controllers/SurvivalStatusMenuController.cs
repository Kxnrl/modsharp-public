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
using System.Runtime.InteropServices;
using System.Text;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Core.Controllers;

internal class SurvivalStatusMenuController : BaseMenuController
{
    private readonly ILocalizerManager? _localizerManager;

    public SurvivalStatusMenuController(MenuManager menuManager,
        IModSharp                                   modSharp,
        IEventManager                               eventManager,
        IEntityManager                              entityManager,
        ulong                                       sessionId,
        Func<IGameClient, Menu>                     menuFactory,
        IGameClient                                 player,
        ILocalizerManager?                          localization) : base(menuManager,
                                                                         modSharp,
                                                                         eventManager,
                                                                         entityManager,
                                                                         sessionId,
                                                                         menuFactory,
                                                                         player)
    {
        _localizerManager = localization;
        _timer            = modSharp.PushTimer(Think, 0.01, GameTimerFlags.Repeatable);
    }

    private void Think()
    {
        if (_cacheContent is null)
        {
            return;
        }

        Print(Client, _cacheContent);
    }

    private void Print(IGameClient client, string content)
    {
        if (_showSurvivalRespawnStatusEvent is null)
        {
            _showSurvivalRespawnStatusEvent = EventManager.CreateEvent("show_survival_respawn_status", false)
                                              ?? throw new Exception("Failed to create event");

            _showSurvivalRespawnStatusEvent.SetInt("duration", 1);
            _showSurvivalRespawnStatusEvent.SetInt("userid",   -1);
        }

        _showSurvivalRespawnStatusEvent.SetString("loc_token", content);
        _showSurvivalRespawnStatusEvent.FireToClient(client);
    }

    private readonly Guid    _timer;
    private          string? _cacheContent;

    private static IGameEvent? _showSurvivalRespawnStatusEvent;

    public override void Render()
    {
        var sb = new StringBuilder();

        const int paddingItemCount = 2; // Buffer zones above and below the cursor

        var offset = Cursor - ItemSkipCount;

        // Scroll down if cursor hits the bottom buffer
        if (offset >= MaxPageItems - paddingItemCount)
        {
            ItemSkipCount = Cursor - (MaxPageItems - paddingItemCount - 1);

            // Prevent scrolling past the last item
            var maxItemSkipCount = Math.Max(0, BuiltMenuItems.Count - MaxPageItems);

            if (ItemSkipCount >= maxItemSkipCount)
            {
                ItemSkipCount = maxItemSkipCount;
            }
        }

        // Scroll up if cursor hits the top buffer
        else if (offset < paddingItemCount)
        {
            // Clamp to 0 to prevent negative skip when cursor is near the start
            ItemSkipCount = Math.Max(0, Cursor - paddingItemCount);
        }

        /*string? header = null;

        if (PreviousMenus.Count > 0)
        {
            var builder = new StringBuilder();

            foreach (var previousMenu in PreviousMenus.Reverse())
            {
                builder.Append(previousMenu.Menu.BuildTitle(Client));

                builder.Append(" > ");
            }

            var content = builder.ToString();

            header = content;
        }*/

        // title
        var title = Menu.BuildTitle(Client);

        sb.Append($"<font class='fontSize-m'>{title}<br><font class='fontSize-xs'>\u00A0<br></font><font class='fontSize-sm'>");

        // colors
        var keyColor      = MenuColor.KeyColor;
        var textColor     = MenuColor.TextColor;
        var disabledColor = MenuColor.DisabledColor;
        var cursorColor   = MenuColor.CursorColor;

        var itemIndex = 1;

        var start = ItemSkipCount;
        var end   = Math.Min(start + MaxPageItems, BuiltMenuItems.Count);

        ReadOnlySpan<BuiltMenuItem> span = CollectionsMarshal.AsSpan(BuiltMenuItems);

        for (var i = start; i < end; i++)
        {
            ref readonly var item = ref span[i];

            if (item.State == MenuItemState.Spacer)
            {
                sb.Append("<br>");

                continue;
            }

            var indexStr = Menu.ShowIndex ? $"{Colored(keyColor, $"{itemIndex}.")} " : "";

            if (item.State == MenuItemState.Disabled)
            {
                sb.Append($"{Colored(disabledColor, $"{indexStr}{item.Title}")}<br>");
            }
            else if (i == Cursor)
            {
                sb.Append(
                    $"{Colored(cursorColor, Menu.CursorLeft)} {indexStr}{Colored(item.Color ?? textColor, item.Title)} {Colored(cursorColor, Menu.CursorRight)}<br>");
            }
            else
            {
                sb.Append($"{indexStr}{Colored(item.Color ?? textColor, item.Title)}<br>");
            }

            itemIndex++;
        }

        // pad empty line
        for (var i = itemIndex; i < MaxPageItems; i++)
        {
            sb.Append("<br/>");
        }

        var hasBackItem = false;
        var hasExitItem = false;

        for (var i = 0; i < span.Length; i++)
        {
            ref readonly var builtMenuItem = ref span[i];

            if (builtMenuItem.ActionKind == MenuItemActionKind.Back)
            {
                hasBackItem = true;
            }
            else if (builtMenuItem.ActionKind == MenuItemActionKind.Exit)
            {
                hasExitItem = true;
            }

            if (hasBackItem && hasExitItem)
            {
                break;
            }
        }

        const string confirmKey  = "MenuSelection.Confirm";
        const string prevItemKey = "MenuSelection.PrevItem";
        const string nextItemKey = "MenuSelection.NextItem";
        const string exitKey     = "MenuSelection.Exit";
        const string backKey     = "MenuSelection.Back";

        string confirm;
        string prevItem;
        string nextItem;
        string exit;
        string back;

        if (_localizerManager is null)
        {
            confirm  = confirmKey;
            prevItem = prevItemKey;
            nextItem = nextItemKey;
            exit     = exitKey;
            back     = backKey;
        }
        else
        {
            var locale = _localizerManager.For(Client);
            confirm  = locale.Text(confirmKey);
            prevItem = locale.Text(prevItemKey);
            nextItem = locale.Text(nextItemKey);
            exit     = locale.Text(exitKey);
            back     = locale.Text(backKey);
        }

        // sb.Append("<font class='fontSize-s'>");

        // Use per-item custom hint if the current item defines one, otherwise default hints
        var customHint = Cursor >= 0 && Cursor < BuiltMenuItems.Count
            ? BuiltMenuItems[Cursor].HintText
            : null;

        if (customHint is not null)
        {
            sb.Append(customHint);
        }
        else
        {
            sb.Append(
                $"{Key(MenuManager.KeyBindings.Confirm.GetBindHint())} {Text(confirm)} / {Key(MenuManager.KeyBindings.MoveUpCursor.GetBindHint())} {Text(prevItem)} / {Key(MenuManager.KeyBindings.MoveDownCursor.GetBindHint())} {Text(nextItem)}");

            var showBottomHint = !(hasBackItem && hasExitItem);
            var showExitHint   = !hasExitItem;
            var showBackHint   = !hasBackItem && PreviousMenus.Count > 0;

            if (showBottomHint && (showExitHint || showBackHint))
            {
                sb.Append("<br>");

                if (showExitHint)
                {
                    sb.Append($"{Key(MenuManager.KeyBindings.Exit.GetBindHint())} {Text(exit)}");
                }

                if (showBackHint)
                {
                    if (showExitHint)
                    {
                        sb.Append(" / ");
                    }

                    sb.Append($"{Key(MenuManager.KeyBindings.GoBack.GetBindHint())} {Text(back)}");
                }
            }
        }

        // sb.Append("</font>");

        _cacheContent = sb.ToString();

        return;

        static string Colored(string color, string content)
            => $"<font color='{color}'>{content}</font>";

        string Key(string key)
            => Colored(keyColor, key);

        string Text(string text)
            => Colored(textColor, text);
    }

    public override void Dispose()
    {
        base.Dispose();

        ModSharp.StopTimer(_timer);
    }

    internal static void ReleaseSharedEvent()
    {
        _showSurvivalRespawnStatusEvent?.Dispose();
        _showSurvivalRespawnStatusEvent = null;
    }
}
