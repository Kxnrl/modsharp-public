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

    public IGameClient Client { get; }

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
        Func<IGameClient, Menu>           menuFactory,
        IGameClient                       player)
    {
        EntityManager = entityManager;
        MenuManager   = menuManager;
        ModSharp      = modSharp;
        EventManager  = eventManager;

        MenuFactory = menuFactory;

        Menu = menuFactory(player);

        Client = player;

        // call menu enter hook
        Menu.OnEnter?.Invoke(Client);

        // build current menu items
        BuildItems();
    }

    private bool SetCursor(int cursor)
    {
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

        Render();

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
                BuiltMenuItems.Add(new (string.Empty,
                                        MenuItemState.Spacer,
                                        0));

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

            BuiltMenuItems.Add(new (content,
                                    context.State,
                                    0,
                                    context.Action,
                                    context.Color));
        }
    }

    public void Refresh()
    {
        BuildItems();
        Render();
    }

    public void GoToPreviousPage()
    {
    }

    public void GoToNextPage()
    {
    }

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
        PreviousMenus.Push(new PreviousMenu(MenuFactory, Menu, 0, Cursor));

        MenuFactory = menuFactory;

        Menu = MenuFactory(Client);
        Menu.OnEnter?.Invoke(Client);

        BuildItems();
        SetCursor(0);
        Render();
    }

    public void Exit()
    {
        MenuManager.CloseClientMenu(Client);
    }

    public void GoBack()
    {
        if (!PreviousMenus.TryPop(out var previousMenu))
        {
            Exit();

            return;
        }

        Menu.OnExit?.Invoke(Client);

        MenuFactory = previousMenu.MenuFactory;
        Menu        = previousMenu.Menu;

        BuildItems();
        SetCursor(previousMenu.Cursor);
        Render();
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
