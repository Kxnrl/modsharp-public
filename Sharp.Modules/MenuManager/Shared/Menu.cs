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
using System.Net;
using System.Runtime.InteropServices;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Shared;

// ReSharper disable MemberCanBePrivate.Global
/// <summary>
///     Represents a menu that can be displayed to a game client.
///     Items are built per-client at display time via <see cref="MenuItemGenerator" /> delegates,
///     allowing dynamic content based on player state.
///     <para>
///         All title and cursor values are embedded directly into HTML output.
///         Raw <c>&lt;</c> or <c>&gt;</c> characters may interfere with markup.
///     </para>
/// </summary>
public class Menu
{
    private readonly List<MenuItem> _items = [];

    /// <summary>
    ///     Gets the list of menu items.
    /// </summary>
    public IReadOnlyList<MenuItem> Items => _items;

    /// <summary>
    ///     Gets the item span backed by the internal list, avoiding allocation.
    /// </summary>
    public ReadOnlySpan<MenuItem> GetItemSpan()
        => CollectionsMarshal.AsSpan(_items);

    /// <summary>
    ///     Gets the left cursor indicator shown on the currently selected item.
    /// </summary>
    public string CursorLeft { get; private set; } = "►";

    /// <summary>
    ///     Gets the right cursor indicator shown on the currently selected item.
    /// </summary>
    public string CursorRight { get; private set; } = "◄";

    /// <summary>
    ///     Gets whether item indices (e.g. "1.", "2.") are displayed before item titles.
    /// </summary>
    public bool ShowIndex { get; private set; } = true;

    /// <summary>
    ///     Gets whether the player is allowed to move while the menu is open.
    /// </summary>
    public bool IsPlayerMovementEnabled { get; private set; } = true;

    /// <summary>
    ///     Sets whether the player is allowed to move while the menu is open.
    /// </summary>
    public void SetPlayerMovement(bool enabled)
        => IsPlayerMovementEnabled = enabled;

    private Func<IGameClient, string> _titleFactory = _ => string.Empty;

    /// <summary>
    ///     Invoked when the menu is closed or navigated away from.
    /// </summary>
    public Action<IGameClient>? OnExit;

    /// <summary>
    ///     Invoked when the menu is first displayed or navigated into.
    /// </summary>
    public Action<IGameClient>? OnEnter;

    /// <summary>
    ///     Sets a static menu title.
    /// </summary>
    /// <param name="name">The title text.</param>
    /// <remarks>The value is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void SetTitle(string name)
        => _titleFactory = _ => name;

    /// <summary>
    ///     Sets the cursor indicators displayed around the currently selected item.
    ///     Values are HTML-decoded before being stored.
    /// </summary>
    /// <param name="left">The left cursor indicator.</param>
    /// <param name="right">The right cursor indicator.</param>
    /// <remarks>The resulting values are embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void SetCursor(string left, string right)
    {
        CursorLeft  = WebUtility.HtmlDecode(left);
        CursorRight = WebUtility.HtmlDecode(right);
    }

    /// <summary>
    ///     Sets whether item indices are displayed.
    /// </summary>
    /// <param name="show"><c>true</c> to show indices; <c>false</c> to hide them.</param>
    public void SetShowIndex(bool show)
        => ShowIndex = show;

    /// <summary>
    ///     Sets a per-client title factory that resolves the menu title at display time.
    /// </summary>
    /// <param name="factory">A function that receives the viewing client and returns the title. Useful for localized titles.</param>
    /// <remarks>The resolved value is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void SetTitle(Func<IGameClient, string> factory)
        => _titleFactory = factory;

    /// <summary>
    ///     Builds the menu title for the specified player.
    /// </summary>
    /// <param name="player">The client viewing the menu.</param>
    /// <returns>The resolved title string.</returns>
    public string BuildTitle(IGameClient player)
        => _titleFactory(player);

    /// <summary>
    ///     Adds multiple pre-constructed menu items.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddItems(IEnumerable<MenuItem> items)
        => _items.AddRange(items);

    /// <summary>
    ///     Adds a menu item with a static title.
    ///     If <paramref name="action" /> is <c>null</c>, the item will be rendered as disabled.
    /// </summary>
    /// <param name="name">The item title.</param>
    /// <param name="action">The action invoked when the item is selected, or <c>null</c> for a disabled item.</param>
    /// <remarks>The title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddItem(string name, Action<IMenuController>? action = null)
        => _items.Add(new MenuItem((IGameClient _, ref MenuItemContext context) =>
        {
            context.Title  = name;
            context.Action = action;
        }));

    /// <summary>
    ///     Adds a menu item with a per-client dynamic title.
    ///     If <paramref name="action" /> is <c>null</c>, the item will be rendered as disabled.
    /// </summary>
    /// <param name="titleFactory">
    ///     A function that receives the viewing client and returns the item title. Useful for localized
    ///     titles.
    /// </param>
    /// <param name="action">The action invoked when the item is selected, or <c>null</c> for a disabled item.</param>
    /// <remarks>The resolved title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddItem(Func<IGameClient, string> titleFactory, Action<IMenuController>? action = null)
        => _items.Add(new MenuItem((IGameClient client, ref MenuItemContext context) =>
        {
            context.Title  = titleFactory(client);
            context.Action = action;
        }));

    /// <summary>
    ///     Adds a menu item using a raw <see cref="MenuItemGenerator" /> for full control over the item context.
    ///     The generator can set title, state, color, and action dynamically.
    /// </summary>
    /// <param name="generator">The generator delegate.</param>
    public void AddItem(MenuItemGenerator generator)
        => _items.Add(new MenuItem(generator));

    /// <summary>
    ///     Adds a menu item using a raw <see cref="MenuItemGenerator" /> with a fallback action.
    ///     The <paramref name="action" /> is only applied if the generator does not set one.
    /// </summary>
    /// <param name="generator">The generator delegate.</param>
    /// <param name="action">The fallback action if the generator does not provide one.</param>
    public void AddItem(MenuItemGenerator generator, Action<IMenuController>? action)
        => _items.Add(new MenuItem((client, ref context) =>
        {
            generator(client, ref context);
            context.Action ??= action;
        }));

    /// <summary>
    ///     Adds a spacer item that renders as an empty line. Spacers are not selectable.
    /// </summary>
    public void AddSpacer()
        => _items.Add(new MenuItem((IGameClient _, ref MenuItemContext context) => context.State = MenuItemState.Spacer));

    /// <summary>
    ///     Adds a disabled item with a static title. Disabled items are visible but not selectable.
    /// </summary>
    /// <param name="name">The item title.</param>
    /// <remarks>The title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddDisabledItem(string name)
        => _items.Add(new MenuItem((IGameClient _, ref MenuItemContext context) =>
        {
            context.Title = name;
            context.State = MenuItemState.Disabled;
        }));

    /// <summary>
    ///     Adds a disabled item with a per-client dynamic title. Disabled items are visible but not selectable.
    /// </summary>
    /// <param name="titleFactory">
    ///     A function that receives the viewing client and returns the item title. Useful for localized
    ///     titles.
    /// </param>
    /// <remarks>The resolved title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddDisabledItem(Func<IGameClient, string> titleFactory)
        => _items.Add(new MenuItem((IGameClient client, ref MenuItemContext context) =>
        {
            context.Title = titleFactory(client);
            context.State = MenuItemState.Disabled;
        }));

    /// <summary>
    ///     Adds an item that navigates to a sub-menu when selected.
    /// </summary>
    /// <param name="name">The item title.</param>
    /// <param name="subMenu">The sub-menu to navigate to.</param>
    /// <remarks>The title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddSubMenu(string name, Menu subMenu)
        => AddItem(name, controller => controller.Next(subMenu));

    /// <summary>
    ///     Adds an item that navigates to a dynamically created sub-menu when selected.
    /// </summary>
    /// <param name="name">The item title.</param>
    /// <param name="menuFactory">A factory that receives the client and returns the sub-menu.</param>
    /// <remarks>The title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddSubMenu(string name, Func<IGameClient, Menu> menuFactory)
        => AddItem(name, controller => controller.Next(menuFactory));

    /// <summary>
    ///     Adds an item with a per-client dynamic title that navigates to a sub-menu when selected.
    /// </summary>
    /// <param name="titleFactory">
    ///     A function that receives the viewing client and returns the item title. Useful for localized
    ///     titles.
    /// </param>
    /// <param name="subMenu">The sub-menu to navigate to.</param>
    /// <remarks>The resolved title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddSubMenu(Func<IGameClient, string> titleFactory, Menu subMenu)
        => AddItem(titleFactory, controller => controller.Next(subMenu));

    /// <summary>
    ///     Adds an item with a per-client dynamic title that navigates to a dynamically created sub-menu when selected.
    /// </summary>
    /// <param name="titleFactory">
    ///     A function that receives the viewing client and returns the item title. Useful for localized
    ///     titles.
    /// </param>
    /// <param name="menuFactory">A factory that receives the client and returns the sub-menu.</param>
    /// <remarks>The resolved title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddSubMenu(Func<IGameClient, string> titleFactory, Func<IGameClient, Menu> menuFactory)
        => AddItem(titleFactory, controller => controller.Next(menuFactory));

    /// <summary>
    ///     Adds an item that navigates back to the previous menu when selected.
    ///     Built-in renderers hide this item when there is no previous menu.
    /// </summary>
    /// <param name="name">The item title. Defaults to "Back".</param>
    /// <remarks>The title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddBackItem(string name = "Back")
        => AddItem((IGameClient _, ref MenuItemContext context) =>
        {
            context.Title      = name;
            context.Action     = controller => controller.GoBack();
            context.ActionKind = MenuItemActionKind.Back;
        });

    /// <summary>
    ///     Adds an item with a per-client dynamic title that navigates back to the previous menu when selected.
    ///     Built-in renderers hide this item when there is no previous menu.
    /// </summary>
    /// <param name="titleFactory">
    ///     A function that receives the viewing client and returns the item title. Useful for localized
    ///     titles.
    /// </param>
    /// <remarks>The resolved title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddBackItem(Func<IGameClient, string> titleFactory)
        => AddItem((IGameClient client, ref MenuItemContext context) =>
        {
            context.Title      = titleFactory(client);
            context.Action     = controller => controller.GoBack();
            context.ActionKind = MenuItemActionKind.Back;
        });

    /// <summary>
    ///     Adds an item that closes the menu when selected.
    /// </summary>
    /// <param name="name">The item title. Defaults to "Exit".</param>
    /// <remarks>The title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddExitItem(string name = "Exit")
        => AddItem((IGameClient _, ref MenuItemContext context) =>
        {
            context.Title      = name;
            context.Action     = controller => controller.Exit();
            context.ActionKind = MenuItemActionKind.Exit;
        });

    /// <summary>
    ///     Adds an item with a per-client dynamic title that closes the menu when selected.
    /// </summary>
    /// <param name="titleFactory">
    ///     A function that receives the viewing client and returns the item title. Useful for localized
    ///     titles.
    /// </param>
    /// <remarks>The resolved title is embedded directly into HTML — avoid raw <c>&lt;</c> or <c>&gt;</c> characters.</remarks>
    public void AddExitItem(Func<IGameClient, string> titleFactory)
        => AddItem((IGameClient client, ref MenuItemContext context) =>
        {
            context.Title      = titleFactory(client);
            context.Action     = controller => controller.Exit();
            context.ActionKind = MenuItemActionKind.Exit;
        });

    /// <summary>
    ///     Enables player movement while this menu is open.
    /// </summary>
    public void EnablePlayerMovement()
        => SetPlayerMovement(true);

    /// <summary>
    ///     Disables player movement while this menu is open.
    /// </summary>
    public void DisablePlayerMovement()
        => SetPlayerMovement(false);

    /// <summary>
    ///     Creates a new <see cref="Builder" /> for fluent menu construction.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static Builder Create()
        => new ();

    /// <summary>
    ///     Fluent builder for constructing <see cref="Menu" /> instances.
    /// </summary>
    public class Builder
    {
        private readonly Menu _menu = new ();

        /// <summary>
        ///     Finalizes and returns the constructed menu.
        /// </summary>
        /// <returns>The built <see cref="Menu" /> instance.</returns>
        public Menu Build()
            => _menu;

        /// <inheritdoc cref="Menu.AddItems" />
        public Builder Items(IEnumerable<MenuItem> items)
        {
            _menu.AddItems(items);

            return this;
        }

        /// <inheritdoc cref="Menu.AddItem(string, Action{IMenuController})" />
        public Builder Item(string name, Action<IMenuController>? action = null)
        {
            _menu.AddItem(name, action);

            return this;
        }

        /// <inheritdoc cref="Menu.AddItem(Func{IGameClient, string}, Action{IMenuController})" />
        public Builder Item(Func<IGameClient, string> titleFactory, Action<IMenuController>? action = null)
        {
            _menu.AddItem(titleFactory, action);

            return this;
        }

        /// <inheritdoc cref="Menu.AddItem(MenuItemGenerator)" />
        public Builder Item(MenuItemGenerator generator)
        {
            _menu.AddItem(generator);

            return this;
        }

        /// <inheritdoc cref="Menu.AddItem(MenuItemGenerator, Action{IMenuController})" />
        public Builder Item(MenuItemGenerator generator, Action<IMenuController>? action)
        {
            _menu.AddItem(generator, action);

            return this;
        }

        /// <inheritdoc cref="Menu.AddSpacer" />
        public Builder Spacer()
        {
            _menu.AddSpacer();

            return this;
        }

        /// <inheritdoc cref="Menu.AddDisabledItem(string)" />
        public Builder DisabledItem(string name)
        {
            _menu.AddDisabledItem(name);

            return this;
        }

        /// <inheritdoc cref="Menu.AddDisabledItem(Func{IGameClient, string})" />
        public Builder DisabledItem(Func<IGameClient, string> titleFactory)
        {
            _menu.AddDisabledItem(titleFactory);

            return this;
        }

        /// <inheritdoc cref="Menu.AddSubMenu(string, Menu)" />
        public Builder SubMenu(string name, Menu subMenu)
        {
            _menu.AddSubMenu(name, subMenu);

            return this;
        }

        /// <inheritdoc cref="Menu.AddSubMenu(string, Func{IGameClient, Menu})" />
        public Builder SubMenu(string name, Func<IGameClient, Menu> menuFactory)
        {
            _menu.AddSubMenu(name, menuFactory);

            return this;
        }

        /// <inheritdoc cref="Menu.AddSubMenu(Func{IGameClient, string}, Menu)" />
        public Builder SubMenu(Func<IGameClient, string> titleFactory, Menu subMenu)
        {
            _menu.AddSubMenu(titleFactory, subMenu);

            return this;
        }

        /// <inheritdoc cref="Menu.AddSubMenu(Func{IGameClient, string}, Func{IGameClient, Menu})" />
        public Builder SubMenu(Func<IGameClient, string> titleFactory, Func<IGameClient, Menu> menuFactory)
        {
            _menu.AddSubMenu(titleFactory, menuFactory);

            return this;
        }

        /// <inheritdoc cref="Menu.AddBackItem(string)" />
        public Builder BackItem(string name = "Back")
        {
            _menu.AddBackItem(name);

            return this;
        }

        /// <inheritdoc cref="Menu.AddBackItem(Func{IGameClient, string})" />
        public Builder BackItem(Func<IGameClient, string> titleFactory)
        {
            _menu.AddBackItem(titleFactory);

            return this;
        }

        /// <inheritdoc cref="Menu.AddExitItem(string)" />
        public Builder ExitItem(string name = "Exit")
        {
            _menu.AddExitItem(name);

            return this;
        }

        /// <inheritdoc cref="Menu.AddExitItem(Func{IGameClient, string})" />
        public Builder ExitItem(Func<IGameClient, string> titleFactory)
        {
            _menu.AddExitItem(titleFactory);

            return this;
        }

        /// <inheritdoc cref="Menu.SetTitle(string)" />
        public Builder Title(string name)
        {
            _menu.SetTitle(name);

            return this;
        }

        /// <inheritdoc cref="Menu.SetTitle(Func{IGameClient, string})" />
        public Builder Title(Func<IGameClient, string> factory)
        {
            _menu.SetTitle(factory);

            return this;
        }

        /// <inheritdoc cref="Menu.SetCursor" />
        public Builder Cursor(string left, string right)
        {
            _menu.SetCursor(left, right);

            return this;
        }

        /// <summary>
        ///     Hides item indices from the menu display.
        /// </summary>
        public Builder HideIndex()
        {
            _menu.SetShowIndex(false);

            return this;
        }

        /// <inheritdoc cref="Menu.EnablePlayerMovement()" />
        public Builder EnablePlayerMovement()
        {
            _menu.SetPlayerMovement(true);

            return this;
        }

        /// <inheritdoc cref="Menu.DisablePlayerMovement()" />
        public Builder DisablePlayerMovement()
        {
            _menu.SetPlayerMovement(false);

            return this;
        }

        /// <summary>
        ///     Sets a callback invoked when the menu is closed or navigated away from.
        /// </summary>
        /// <param name="fn">The callback receiving the client.</param>
        public Builder OnExit(Action<IGameClient> fn)
        {
            _menu.OnExit = fn;

            return this;
        }

        /// <summary>
        ///     Sets a callback invoked when the menu is first displayed or navigated into.
        /// </summary>
        /// <param name="fn">The callback receiving the client.</param>
        public Builder OnEnter(Action<IGameClient> fn)
        {
            _menu.OnEnter = fn;

            return this;
        }
    }
}

/// <summary>
///     Represents a single menu item backed by an optional <see cref="MenuItemGenerator" />.
/// </summary>
/// <param name="Generator">
///     The generator delegate invoked per-client to populate the item context.
///     If <c>null</c>, the item is skipped during menu building.
/// </param>
public readonly record struct MenuItem(MenuItemGenerator? Generator = null);

/// <summary>
///     Delegate invoked per-client to populate a <see cref="MenuItemContext" />.
///     Set <see cref="MenuItemContext.Title" />, <see cref="MenuItemContext.State" />,
///     <see cref="MenuItemContext.Color" />, <see cref="MenuItemContext.Action" />,
///     and <see cref="MenuItemContext.ActionKind" /> as needed.
/// </summary>
/// <param name="client">The client viewing the menu.</param>
/// <param name="context">The mutable item context to populate.</param>
public delegate void MenuItemGenerator(IGameClient client, ref MenuItemContext context);

/// <summary>
///     Mutable context populated by a <see cref="MenuItemGenerator" /> during menu building.
/// </summary>
public record struct MenuItemContext
{
    /// <summary>
    ///     The display title. If <c>null</c> or whitespace after generation, the item is skipped.
    ///     The value is embedded directly into HTML output, so raw
    ///     <c>&lt;</c> or <c>&gt;</c> characters may interfere with markup.
    /// </summary>
    public string? Title;

    /// <summary>
    ///     The item state. Defaults to <see cref="MenuItemState.Default" />.
    ///     Items with no <see cref="Action" /> are automatically marked as <see cref="MenuItemState.Disabled" />.
    /// </summary>
    public MenuItemState State;

    /// <summary>
    ///     Optional HTML color override for the item title (e.g. "#FFCCCB"). Does not apply to disabled items.
    /// </summary>
    public string? Color;

    /// <summary>
    ///     The action invoked when the player selects this item.
    ///     If <c>null</c> and <see cref="State" /> is <see cref="MenuItemState.Default" />,
    ///     the item is automatically treated as disabled.
    /// </summary>
    public Action<IMenuController>? Action;

    /// <summary>
    ///     Semantic action kind used by built-in navigation helpers.
    ///     Defaults to <see cref="MenuItemActionKind.None" />.
    /// </summary>
    public MenuItemActionKind ActionKind;

    /// <summary>
    ///     Optional custom hint text displayed at the bottom of the menu when this item is selected.
    ///     If <c>null</c>, the default navigation hints are shown.
    ///     The value is embedded directly into HTML output.
    /// </summary>
    public string? HintText;
}

/// <summary>
///     Semantic item action kind used by the menu renderer.
/// </summary>
public enum MenuItemActionKind
{
    None,
    Back,
    Exit,
}
