using System;
using System.Collections.Generic;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Shared;

// ReSharper disable MemberCanBePrivate.Global
public class Menu
{
    public string?                    Title              { get; private set; }
    public Func<IGameClient, string>? TitleFactory       { get; private set; }
    public string?                    Description        { get; private set; }
    public Func<IGameClient, string>? DescriptionFactory { get; private set; }
    public IReadOnlyList<MenuItem>    Items              => _items.AsReadOnly();

    private readonly List<MenuItem> _items = [];

    public Action<IGameClient>? OnExit;
    public Action<IGameClient>? OnEnter;

    public void SetTitle(string name)
        => Title = name;

    public void SetTitle(Func<IGameClient, string> factory)
        => TitleFactory = factory;

    public string BuildTitle(IGameClient player)
        => TitleFactory?.Invoke(player) ?? Title ?? string.Empty;

    public void SetDescription(string desc)
        => Description = desc;

    public void SetDescription(Func<IGameClient, string> factory)
        => DescriptionFactory = factory;

    public string BuildDescription(IGameClient player)
        => DescriptionFactory?.Invoke(player) ?? Description ?? string.Empty;

    public void AddItems(IEnumerable<MenuItem> items)
        => _items.AddRange(items);

    public void AddItem(string name, Action<IMenuController>? action = null)
        => _items.Add(new MenuItem(x => new MenuItemMetadata(name), action));

    public void AddItem(Func<IMenuController, MenuItemMetadata> factory)
        => _items.Add(new MenuItem(factory));

    public void AddItem(Func<IMenuController, string> titleFactory, Action<IMenuController>? action)
        => _items.Add(new MenuItem(x => new MenuItemMetadata(titleFactory(x)), action));

    public void AddItem(Func<IMenuController, string> titleFactory,
        Action<IMenuController>?                      action,
        Func<IMenuController, MenuItemState>          stateFactory)
        => _items.Add(new MenuItem(x => new MenuItemMetadata(titleFactory(x), stateFactory(x)), action));

    public void AddItem(string name, Action<IMenuController>? action, Func<IMenuController, MenuItemState> stateFactory)
        => _items.Add(new MenuItem(x => new MenuItemMetadata(name, stateFactory(x)), action));

    public void AddSpacer()
        => _items.Add(new MenuItem(x => new MenuItemMetadata(null, MenuItemState.Spacer)));

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

        public Builder Item(Func<IMenuController, MenuItemMetadata> factory)
        {
            _menu.AddItem(factory);

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

        public Builder Description(string desc)
        {
            _menu.SetDescription(desc);

            return this;
        }

        public Builder Description(Func<IGameClient, string> factory)
        {
            _menu.SetDescription(factory);

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

public readonly record struct MenuItem(
    Func<IMenuController, MenuItemMetadata>? Factory = null,
    Action<IMenuController>?                 Action  = null);

public readonly record struct MenuItemMetadata(
    string?       Title = null,
    MenuItemState State = MenuItemState.Default);
