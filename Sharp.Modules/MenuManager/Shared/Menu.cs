using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Shared;

// ReSharper disable MemberCanBePrivate.Global
public class Menu
{
    private readonly List<MenuItem>          _items = [];
    public           IReadOnlyList<MenuItem> Items => _items;

    public ReadOnlySpan<MenuItem> GetItemSpan()
        => CollectionsMarshal.AsSpan(_items);

    public string CursorLeft  { get; private set; } = "►";
    public string CursorRight { get; private set; } = "◄";

    public bool ShowIndex { get; private set; } = true;

    private Func<IGameClient, string> _titleFactory = _ => string.Empty;

    public Action<IGameClient>? OnExit;
    public Action<IGameClient>? OnEnter;

    public void SetTitle(string name)
        => _titleFactory = _ => name;

    public void SetCursor(string left, string right)
    {
        CursorLeft  = WebUtility.HtmlDecode(left);
        CursorRight = WebUtility.HtmlDecode(right);
    }

    public void SetShowIndex(bool show)
        => ShowIndex = show;

    public void SetTitle(Func<IGameClient, string> factory)
        => _titleFactory = factory;

    public string BuildTitle(IGameClient player)
        => _titleFactory(player);

    public void AddItems(IEnumerable<MenuItem> items)
        => _items.AddRange(items);

    public void AddItem(string name, Action<IMenuController>? action = null)
        => _items.Add(new ((IGameClient _, ref MenuItemContext context) =>
        {
            context.Title  = name;
            context.Action = action;
        }));

    public void AddItem(Func<IGameClient, string> titleFactory, Action<IMenuController>? action = null)
        => _items.Add(new ((IGameClient client, ref MenuItemContext context) =>
        {
            context.Title  = titleFactory(client);
            context.Action = action;
        }));

    public void AddItem(MenuItemGenerator generator)
        => _items.Add(new (generator));

    public void AddItem(MenuItemGenerator generator, Action<IMenuController>? action)
        => _items.Add(new ((client, ref context) =>
        {
            generator(client, ref context);
            context.Action ??= action;
        }));

    public void AddSpacer()
        => _items.Add(new ((IGameClient _, ref MenuItemContext context) => context.State = MenuItemState.Spacer));

    public static Builder Create()
        => new ();

    public class Builder
    {
        private readonly Menu _menu = new ();

        public Menu Build()
            => _menu;

        public Builder Items(IEnumerable<MenuItem> items)
        {
            _menu.AddItems(items);

            return this;
        }

        public Builder Item(string name, Action<IMenuController>? action = null)
        {
            _menu.AddItem(name, action);

            return this;
        }

        public Builder Item(Func<IGameClient, string> titleFactory, Action<IMenuController>? action = null)
        {
            _menu.AddItem(titleFactory, action);

            return this;
        }

        public Builder Item(MenuItemGenerator generator)
        {
            _menu.AddItem(generator);

            return this;
        }

        public Builder Item(MenuItemGenerator generator, Action<IMenuController>? action)
        {
            _menu.AddItem(generator, action);

            return this;
        }

        public Builder Spacer()
        {
            _menu.AddSpacer();

            return this;
        }

        public Builder Title(string name)
        {
            _menu.SetTitle(name);

            return this;
        }

        public Builder Title(Func<IGameClient, string> factory)
        {
            _menu.SetTitle(factory);

            return this;
        }

        public Builder Cursor(string left, string right)
        {
            _menu.SetCursor(left, right);

            return this;
        }

        public Builder HideIndex()
        {
            _menu.SetShowIndex(false);

            return this;
        }

        public Builder OnExit(Action<IGameClient> fn)
        {
            _menu.OnExit = fn;

            return this;
        }

        public Builder OnEnter(Action<IGameClient> fn)
        {
            _menu.OnEnter = fn;

            return this;
        }
    }
}

public readonly record struct MenuItem(MenuItemGenerator? Generator = null);

public delegate void MenuItemGenerator(IGameClient client, ref MenuItemContext context);

public record struct MenuItemContext
{
    public string?                  Title;
    public MenuItemState            State;
    public string?                  Color;
    public Action<IMenuController>? Action;
}
