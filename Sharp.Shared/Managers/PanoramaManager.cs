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
    ICustomHudLayout? CreateLayout(string layoutResource, string? targetName = null);

    /// <summary>
    ///     Installs a custom-HUD button-click listener as global on <b>current map</b>.
    /// </summary>
    /// <remarks>Remove the listener during module shutdown.</remarks>
    void InstallClickListener(CustomHudClickedHandler listener);

    /// <summary>
    ///     Removes a previously installed button-click listener as global on <b>current map</b>.
    /// </summary>
    void RemoveClickListener(CustomHudClickedHandler listener);

    /// <summary>
    ///     Installs a custom-HUD button-click listener for single layout.
    /// </summary>
    void InstallClickCallback(ICustomHudLayout layout, CustomHudClickedHandler callback);

    /// <summary>
    ///     Removes a previously installed button-click listener for single layout.
    /// </summary>
    void RemoveClickCallback(ICustomHudLayout layout, CustomHudClickedHandler callback);
}
