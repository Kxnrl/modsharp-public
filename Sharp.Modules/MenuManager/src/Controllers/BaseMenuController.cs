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
using System.Collections.Generic;
using System.Linq;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Core.Controllers;

internal abstract class BaseMenuController : IInternalMenuController
{
    protected const int MaxPageItems = 5;

    public IGameClient Client    { get; }
    public ulong       SessionId { get; }

    protected readonly Stack<PreviousMenu> PreviousMenus  = new ();
    protected readonly List<BuiltMenuItem> BuiltMenuItems = [];
    protected          int                 Cursor;
    protected          int                 ItemSkipCount;

    public             Menu                    Menu        { get; protected set; }
    public             Func<IGameClient, Menu> MenuFactory { get; private set; }
    protected readonly MenuManager             MenuManager;
    protected readonly IEntityManager          EntityManager;
    protected readonly IModSharp               ModSharp;
    protected readonly IEventManager           EventManager;

    public BaseMenuController(MenuManager menuManager,
        IModSharp                         modSharp,
        IEventManager                     eventManager,
        IEntityManager                    entityManager,
        ulong                             sessionId,
        Func<IGameClient, Menu>           menuFactory,
        IGameClient                       player)
    {
        EntityManager = entityManager;
        MenuManager   = menuManager;
        ModSharp      = modSharp;
        EventManager  = eventManager;
        SessionId     = sessionId;

        MenuFactory = menuFactory;

        Menu = menuFactory(player);

        Client = player;

        // call menu enter hook
        Menu.OnEnter?.Invoke(Client);

        // build current menu items
        BuildItems();
        SetCursor(0, false);
    }

    private bool SetCursor(int cursor, bool render = true)
    {
        if (BuiltMenuItems.Count == 0)
        {
            Cursor = -1;

            return false;
        }

        if (cursor >= BuiltMenuItems.Count || cursor < 0)
        {
            cursor = BuiltMenuItems.Count - 1;
        }

        var tries = 0;

        while (BuiltMenuItems[cursor].State != MenuItemState.Default)
        {
            cursor--;

            if (cursor < 0)
            {
                cursor = BuiltMenuItems.Count - 1;
            }

            tries++;

            if (tries >= BuiltMenuItems.Count)
            {
                Cursor = -1;

                return false;
            }
        }

        Cursor = cursor;

        if (render)
        {
            Render();
        }

        return true;
    }

    public bool MoveUpCursor()
    {
        if (Cursor == -1)
        {
            return false;
        }

        var cursor = Cursor - 1;

        if (cursor < 0)
        {
            cursor = BuiltMenuItems.Count - 1;
        }

        var tries = 0;

        while (BuiltMenuItems[cursor].State != MenuItemState.Default)
        {
            cursor--;

            if (cursor < 0)
            {
                cursor = BuiltMenuItems.Count - 1;
            }

            tries++;

            if (tries >= BuiltMenuItems.Count)
            {
                return false;
            }
        }

        Cursor = cursor;

        Render();

        return true;
    }

    public bool MoveDownCursor()
    {
        if (Cursor == -1)
        {
            return false;
        }

        var cursor = Cursor + 1;

        if (cursor >= BuiltMenuItems.Count)
        {
            cursor = 0;
        }

        var tries = 0;

        while (BuiltMenuItems[cursor].State != MenuItemState.Default)
        {
            cursor++;

            if (cursor >= BuiltMenuItems.Count)
            {
                cursor = 0;
            }

            tries++;

            if (tries >= BuiltMenuItems.Count)
            {
                return false;
            }
        }

        Cursor = cursor;

        Render();

        return true;
    }

    public abstract void Render();

    private void BuildItems()
    {
        var span = Menu.GetItemSpan();

        BuiltMenuItems.Clear();
        BuiltMenuItems.EnsureCapacity(span.Length);

        foreach (ref readonly var menuItem in span)
        {
            if (menuItem.Generator is null)
            {
                continue;
            }

            var context = new MenuItemContext();
            menuItem.Generator.Invoke(Client, ref context);

            if (context.State == MenuItemState.Ignore)
            {
                continue;
            }

            if (context.State == MenuItemState.Spacer)
            {
                BuiltMenuItems.Add(new BuiltMenuItem(string.Empty,
                                                     MenuItemState.Spacer,
                                                     0,
                                                     ActionKind: context.ActionKind,
                                                     HintText: context.HintText));

                continue;
            }

            if (context.ActionKind == MenuItemActionKind.Back && PreviousMenus.Count == 0)
            {
                continue;
            }

            // ignore if no title
            if (string.IsNullOrWhiteSpace(context.Title))
            {
                continue;
            }

            // we mark it as disabled if no action is provided
            if (context.Action is null && context.State == MenuItemState.Default)
            {
                context.State = MenuItemState.Disabled;
            }

            var content = context.Title ?? string.Empty;

            BuiltMenuItems.Add(new BuiltMenuItem(content,
                                                 context.State,
                                                 0,
                                                 context.Action,
                                                 context.Color,
                                                 context.ActionKind,
                                                 context.HintText));
        }
    }

    public void Refresh()
    {
        BuildItems();

        var maxItemSkipCount = Math.Max(0, BuiltMenuItems.Count - MaxPageItems);
        ItemSkipCount = Math.Clamp(ItemSkipCount, 0, maxItemSkipCount);

        if (!SetCursor(Cursor))
        {
            Render();
        }
    }

    public void GoToPreviousPage()
    {
        MoveCursorByPage(-1);
    }

    public void GoToNextPage()
    {
        MoveCursorByPage(1);
    }

    private bool MoveCursorByPage(int direction)
    {
        if (Cursor == -1 || BuiltMenuItems.Count == 0)
        {
            return false;
        }

        var selectableIndices = new List<int>(BuiltMenuItems.Count);
        var currentPosition   = -1;

        for (var i = 0; i < BuiltMenuItems.Count; i++)
        {
            if (BuiltMenuItems[i].State != MenuItemState.Default)
            {
                continue;
            }

            if (i == Cursor)
            {
                currentPosition = selectableIndices.Count;
            }

            selectableIndices.Add(i);
        }

        if (selectableIndices.Count == 0)
        {
            Cursor = -1;

            return false;
        }

        if (currentPosition == -1)
        {
            currentPosition = 0;
        }

        var offset         = direction * MaxPageItems;
        var targetPosition = Math.Clamp(currentPosition + offset, 0, selectableIndices.Count - 1);
        var targetCursor   = selectableIndices[targetPosition];

        if (targetCursor == Cursor)
        {
            return false;
        }

        Cursor = targetCursor;
        Render();

        return true;
    }

    public bool IsInMenu(Menu menuInstance)
    {
        if (ReferenceEquals(Menu, menuInstance))
        {
            return true;
        }

        foreach (var previousMenu in PreviousMenus)
        {
            if (ReferenceEquals(previousMenu.Menu, menuInstance))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsInCurrentMenu(Menu menuInstance)
        => ReferenceEquals(Menu, menuInstance);

    public void Confirm()
    {
        if (Cursor == -1)
        {
            return;
        }

        BuiltMenuItems[Cursor]
            .Action?.Invoke(this);
    }

    public void Next(Menu menu)
        => Next(_ => menu);

    public void Next(Func<IGameClient, Menu> menuFactory)
    {
        PreviousMenus.Push(new PreviousMenu(MenuFactory, Menu, ItemSkipCount, Cursor));

        MenuFactory = menuFactory;

        Menu = MenuFactory(Client);
        Menu.OnEnter?.Invoke(Client);

        BuildItems();
        ItemSkipCount = 0;

        if (!SetCursor(0))
        {
            Render();
        }
    }

    public void Exit()
    {
        MenuManager.CloseClientMenu(Client);
    }

    public void GoBack()
    {
        if (!PreviousMenus.TryPop(out var previousMenu))
        {
            return;
        }

        Menu.OnExit?.Invoke(Client);

        MenuFactory = previousMenu.MenuFactory;
        Menu        = previousMenu.Menu;

        BuildItems();
        ItemSkipCount = Math.Clamp(previousMenu.Page, 0, Math.Max(0, BuiltMenuItems.Count - MaxPageItems));

        if (!SetCursor(previousMenu.Cursor))
        {
            Render();
        }
    }

    public virtual void Dispose()
    {
        Menu.OnExit?.Invoke(Client);

        foreach (var previousMenu in PreviousMenus.Reverse())
        {
            previousMenu.Menu.OnExit?.Invoke(Client);
        }

        PreviousMenus.Clear();
    }
}
