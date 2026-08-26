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

using Sharp.Shared.GameEntities;

namespace Sharp.Shared.Managers;

/// <summary>
///     Creates custom HUD layouts and dispatches their button-click events.
/// </summary>
/// <remarks>
///     <para>
///         This manager only creates the server entity. It does not distribute or mount assets
///         on clients. The compiled VXML, VCSS and local images must already be present in
///         client-mounted Workshop/addon content. Custom HUD layouts cannot load images from
///         HTTP or HTTPS URLs.
///     </para>
/// </remarks>
public interface ICustomHudManager
{
    /// <summary>
    ///     Callback invoked when a player clicks a Button in a custom HUD layout.
    /// </summary>
    /// <param name="player">Player who clicked the button.</param>
    /// <param name="layout">Layout containing the clicked button.</param>
    /// <param name="buttonId">The clicked Button panel's <c>id</c> attribute.</param>
    public delegate void CustomHudClickedHandler(IPlayerController player, ICustomHudLayout layout, string buttonId);

    /// <summary>
    ///     Creates and spawns a <c>custom_hud_layout</c> entity.
    /// </summary>
    /// <param name="layoutResource">
    ///     Mounted logical layout path, for example
    ///     <c>panorama/layout/custom_game/my_plugin/main.vxml</c>.
    /// </param>
    /// <param name="targetName">Optional entity targetname.</param>
    /// <returns>The spawned layout, or <see langword="null" /> if entity creation failed.</returns>
    /// <remarks>
    ///     Spawning the entity does not verify client asset availability. Its compiled VXML,
    ///     referenced VCSS and local images must already be mounted by every client.
    /// </remarks>
    /// <exception cref="System.ArgumentException">
    ///     <paramref name="layoutResource" /> is empty or whitespace, or a supplied
    ///     <paramref name="targetName" /> is empty or whitespace.
    /// </exception>
    /// <exception cref="System.PlatformNotSupportedException">
    ///     The current platform or game build does not expose the custom-HUD native API.
    /// </exception>
    ICustomHudLayout? CreateLayout(string layoutResource, string? targetName = null);

    /// <summary>
    ///     Installs a custom-HUD button-click listener.
    /// </summary>
    /// <remarks>Remove the listener during module shutdown.</remarks>
    /// <param name="listener">Callback to register.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="listener" /> is <see langword="null" />.</exception>
    void InstallClickListener(CustomHudClickedHandler listener);

    /// <summary>
    ///     Removes a previously installed button-click listener.
    /// </summary>
    /// <param name="listener">Previously registered callback to remove.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="listener" /> is <see langword="null" />.</exception>
    void RemoveClickListener(CustomHudClickedHandler listener);
}
