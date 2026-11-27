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

using System.Collections.Generic;
using Sharp.Shared.GameEntities;

namespace Sharp.Shared.Managers;

/// <remarks>
///     All members must be called on the game main thread.
/// </remarks>
public interface IPanoramaManager
{
    /// <summary>
    ///     Callback invoked when a player clicks a Button in a custom HUD layout.
    /// </summary>
    /// <param name="player">Player who clicked the button.</param>
    /// <param name="layout">Layout containing the clicked button.</param>
    /// <param name="buttonId">The clicked Button panel's <c>id</c> attribute.</param>
    delegate void CustomHudClickedHandler(IPlayerController player, ICustomHudLayout layout, string buttonId);

    /// <summary>
    ///     Get Managed layouts
    /// </summary>
    IEnumerable<ICustomHudLayout> GetManagedLayouts();

    /// <summary>
    ///     Creates and spawns a <c>custom_hud_layout</c> entity.
    /// </summary>
    /// <param name="layoutResource">
    ///     Mounted logical layout path, for example
    ///     <c>panorama/layout/custom_game/my_plugin/main.vxml</c>.
    /// </param>
    /// <param name="targetName">Optional entity targetname; defaults to <c>"ms_custom_hud_layout"</c> when null or whitespace.</param>
    /// <returns>The spawned layout, or <see langword="null" /> if entity creation failed.</returns>
    /// <remarks>
    ///     Spawning the entity does not verify client asset availability. Its compiled VXML,
    ///     referenced VCSS and local images must already be mounted by every client.
    ///     Tracked layouts and their per-layout callbacks are cleared on map shutdown.
    /// </remarks>
    /// <exception cref="System.ArgumentException"><paramref name="layoutResource" /> is null, empty, or whitespace.</exception>
    ICustomHudLayout? CreateLayout(string layoutResource, string? targetName = null);

    /// <summary>
    ///     Installs a global custom-HUD button-click listener.
    /// </summary>
    /// <remarks>
    ///     Global listeners run before per-layout callbacks and persist across map changes until removed.
    ///     Duplicate registrations and delegates from unloaded modules are logged and rejected.
    ///     Remove the listener during module shutdown.
    /// </remarks>
    void InstallClickListener(CustomHudClickedHandler listener);

    /// <summary>
    ///     Removes a previously installed global button-click listener.
    /// </summary>
    void RemoveClickListener(CustomHudClickedHandler listener);

    /// <summary>
    ///     Installs a custom-HUD button-click callback for a single managed layout.
    /// </summary>
    /// <remarks>
    ///     Per-layout callbacks run after global listeners and are cleared automatically when the layout is deleted or on map shutdown.
    ///     Duplicate registrations and delegates from unloaded modules are logged and rejected.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException"><paramref name="layout" /> is not a layout tracked by this manager.</exception>
    void InstallClickCallback(ICustomHudLayout layout, CustomHudClickedHandler callback);

    /// <summary>
    ///     Removes a previously installed button-click callback for a single layout.
    /// </summary>
    void RemoveClickCallback(ICustomHudLayout layout, CustomHudClickedHandler callback);
}
