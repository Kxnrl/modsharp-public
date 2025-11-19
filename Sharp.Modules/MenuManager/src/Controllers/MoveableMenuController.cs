using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sharp.Modules.MenuManager.Core.Renderers;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Core.Controllers;

internal class MoveableMenuController : IInternalMenuController
{
    private const int MaxPageItems = 7;

    public IGameClient Player { get; }

    private readonly IMenuRenderer _menuRenderer;

    private readonly Stack<PreviousMenu> _previousMenus  = new ();
    private readonly List<BuiltMenuItem> _builtMenuItems = [];
    private          int                 _cursor;
    private          int                 _itemSkipCount;

    public           Menu                    Menu        { get; private set; }
    public           Func<IGameClient, Menu> MenuFactory { get; private set; }
    private          RenderData              _renderData;
    private readonly CoreMenuManager         _menuManager;
    private readonly IEntityManager          _entityManager;

    public MoveableMenuController(CoreMenuManager menuManager,
        IModSharp                                 modSharp,
        IEventManager                             eventManager,
        IEntityManager                            entityManager,
        Func<IGameClient, Menu>                   menuFactory,
        IGameClient                               player)
    {
        _entityManager = entityManager;
        _menuManager   = menuManager;

        MenuFactory = menuFactory;

        Menu = menuFactory(player);

        Player = player;

        // _menuRenderer = new ViewmodelMenuRenderer(_entityManager, player);
        _menuRenderer = new SurvivalStatusMenuRenderer(modSharp, eventManager, player);

        // call menu enter hook
        Menu.OnEnter?.Invoke(Player);

        // build current menu items
        BuildItems();

        // render current page
        Render();
    }

    private bool SetCursor(int cursor)
    {
        if (cursor >= _builtMenuItems.Count || cursor < 0)
            cursor = _builtMenuItems.Count - 1;

        var tries = 0;

        while (_builtMenuItems[cursor].State != MenuItemState.Default)
        {
            cursor--;

            if (cursor < 0)
            {
                cursor = _builtMenuItems.Count - 1;
            }

            tries++;

            if (tries >= _builtMenuItems.Count)
            {
                _cursor = -1;

                return false;
            }
        }

        _cursor = cursor;

        Render();

        return true;
    }

    public bool MoveUpCursor()
    {
        if (_cursor == -1)
            return false;

        var cursor = _cursor - 1;

        if (cursor >= _builtMenuItems.Count || cursor < 0)
            return false;

        while (_builtMenuItems[cursor].State != MenuItemState.Default)
        {
            cursor--;

            if (cursor < 0)
                return false;
        }

        _cursor = cursor;

        Render();

        return true;
    }

    public bool MoveDownCursor()
    {
        if (_cursor == -1)
            return false;

        var cursor = _cursor + 1;

        if (cursor >= _builtMenuItems.Count || cursor < 0)
            return false;

        while (_builtMenuItems[cursor].State != MenuItemState.Default)
        {
            cursor++;

            if (cursor >= _builtMenuItems.Count)
                return false;
        }

        _cursor = cursor;

        Render();

        return true;
    }

    private void Render()
    {
        const int paddingItemCount = 2; // 游标视窗上下预留展示选项数量

        var offset = _cursor - _itemSkipCount;

        if (offset >= MaxPageItems - paddingItemCount)
        {
            _itemSkipCount = _cursor - (MaxPageItems - paddingItemCount - 1);

            var maxItemSkipCount = _builtMenuItems.Count - MaxPageItems;

            if (_itemSkipCount >= maxItemSkipCount)
                _itemSkipCount = maxItemSkipCount;
        }
        else if (offset < paddingItemCount)
        {
            _itemSkipCount = _cursor - paddingItemCount;

            if (_itemSkipCount < 0)
                _itemSkipCount = 0;
        }

        string? header = null;

        if (_previousMenus.Any())
        {
            var builder = new StringBuilder();

            foreach (var previousMenu in _previousMenus.Reverse())
            {
                builder.Append(previousMenu.Menu.BuildTitle(Player));

                builder.Append(" > ");
            }

            var content = builder.ToString();

            header = content;
        }

        // title
        var title = Menu.BuildTitle(Player);

        // description
        string? description = null;

        if (Menu.Description is not null || Menu.DescriptionFactory is not null)
        {
            var content = Menu.BuildDescription(Player);

            description = content;
        }

        var itemIndex = 1;

        List<RenderItem> items = [];

        foreach (var item in _builtMenuItems.Skip(_itemSkipCount)
                                            .Take(MaxPageItems))
        {
            if (item.State == MenuItemState.Spacer)
            {
                items.Add(new ("", RenderItemType.Spacer, itemIndex));
            }
            else
            {
                items.Add(new (item.Title,
                               item.State == MenuItemState.Disabled ? RenderItemType.Disabled : RenderItemType.Default,
                               itemIndex));
            }

            itemIndex++;
        }

        // pad empty line
        for (var i = itemIndex; i <= MaxPageItems; i++)
        {
            items.Add(new ("", RenderItemType.Spacer, itemIndex));
        }

        // bottom
        string? footer = null;

        {
            var pagination = $"当前 {_cursor + 1}/{_builtMenuItems.Count} 页";
            var content    = $"{pagination}\n\nR 上一项 / F 下一项\nE 确认选择 / Shift 返回上一级菜单\nTab 退出菜单";

            footer = content;
        }

        _renderData = new RenderData
        {
            Description = description,
            Footer      = footer,
            Header      = header,
            Items       = items,
            Title       = title,
        };

        foreach (var renderItem in _renderData.Items)
        {
            renderItem.IsSelected = false;
        }

        _renderData.Items[_cursor].IsSelected = true;

        _menuRenderer.Render(_renderData);
    }

    private void BuildItems()
    {
        _builtMenuItems.Clear();

        var index = 0;

        foreach (var menuItem in Menu.Items)
        {
            index++;
            var metadata = menuItem.Factory?.Invoke(this);

            if (metadata?.State == MenuItemState.Ignore)
            {
                index--;

                continue;
            }

            if (metadata?.State == MenuItemState.Spacer)
            {
                _builtMenuItems.Add(new BuiltMenuItem(string.Empty,
                                                      MenuItemState.Spacer,
                                                      0,
                                                      null));
            }
            else
            {
                var content = $"{metadata?.Title ?? string.Empty}";

                _builtMenuItems.Add(new BuiltMenuItem(content,
                                                      metadata?.State ?? MenuItemState.Default,
                                                      0,
                                                      metadata?.Action));
            }
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
        if (_cursor == -1)
            return;

        _builtMenuItems[_cursor]
            .Action?.Invoke(this);
    }

    public void Next(Menu menu)
        => Next(_ => menu);

    public void Next(Func<IGameClient, Menu> menuFactory)
    {
        _previousMenus.Push(new PreviousMenu(MenuFactory, Menu, 0, _cursor));

        MenuFactory = menuFactory;

        Menu = MenuFactory(Player);
        Menu.OnEnter?.Invoke(Player);

        BuildItems();
        SetCursor(0);
        Render();
    }

    public void Exit()
    {
        _menuManager.ClosePlayerMenu(Player);
    }

    public void GoBack()
    {
        if (!_previousMenus.TryPop(out var previousMenu))
        {
            Exit();

            return;
        }

        Menu.OnExit?.Invoke(Player);

        MenuFactory = previousMenu.MenuFactory;
        Menu        = previousMenu.Menu;

        BuildItems();
        SetCursor(previousMenu.Cursor);
        Render();
    }

    public void Dispose()
    {
        Menu.OnExit?.Invoke(Player);

        foreach (var previousMenu in _previousMenus.Reverse())
        {
            previousMenu.Menu.OnExit?.Invoke(Player);
        }

        _previousMenus.Clear();

        _menuRenderer.Dispose();
    }
}
