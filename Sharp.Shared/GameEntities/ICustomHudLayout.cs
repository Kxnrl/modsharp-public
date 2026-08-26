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
using Sharp.Shared.Attributes;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Shared.GameEntities;

/// <summary>
///     A server-controlled Panorama layout created by the <c>custom_hud_layout</c> entity.
/// </summary>
/// <remarks>
///     <para>
///         The layout supports Panel, Label, Image and Button panels plus CSS. Panorama events
///         and client-side scripts are not supported.
///     </para>
///     <para>
///         Images must resolve to client-mounted local resources. Custom HUD layouts reject
///         HTTP and HTTPS image URLs.
///     </para>
///     <para>
///         Panel ids, class names and dialog-variable names are interned by the game, with a
///         limit of 1024 unique values in each category per layout.
///     </para>
///     <para>
///         Prefer the per-player overloads that accept <see cref="IGameClient" /> or
///         <see cref="IPlayerController" />. <see cref="PlayerSlot" /> overloads remain available
///         for low-level and compatibility use.
///     </para>
/// </remarks>
[NetClass("CCSCustomHudLayout")]
public interface ICustomHudLayout : IBaseEntity
{
    /// <summary>
    ///     Logical Panorama layout resource used by this entity, for example
    ///     <c>panorama/layout/custom_game/my_plugin/main.vxml</c>.
    /// </summary>
    string Layout { get; }

    /// <summary>
    ///     Overrides whether a panel has a pre-defined CSS class for every player.
    /// </summary>
    /// <remarks>
    ///     This changes only class membership. For the change to have a visual effect, the
    ///     compiled VCSS must contain a matching rule for the class.
    /// </remarks>
    /// <param name="panelId">The panel's <c>id</c> attribute.</param>
    /// <param name="className">CSS class name without a leading dot.</param>
    /// <param name="classOverride">
    ///     Class-presence override. <see cref="CustomHudClassOverride.Inherit" /> restores VXML.
    /// </param>
    void SetClassOverride(string panelId, string className, CustomHudClassOverride classOverride);

    /// <summary>
    ///     Overrides whether a panel has a pre-defined CSS class for one player slot.
    /// </summary>
    /// <remarks>
    ///     This changes only class membership. <see cref="CustomHudClassOverride.Inherit" />
    ///     removes the player override and inherits the global class state.
    /// </remarks>
    /// <param name="playerSlot">Player receiving the override.</param>
    /// <param name="panelId">The panel's <c>id</c> attribute.</param>
    /// <param name="className">CSS class name without a leading dot.</param>
    /// <param name="classOverride">Class-presence override for this player.</param>
    void SetClassOverrideForPlayer(PlayerSlot playerSlot,
        string                                      panelId,
        string                                      className,
        CustomHudClassOverride                      classOverride);

    /// <summary>
    ///     Overrides whether a panel has a pre-defined CSS class for one game client.
    /// </summary>
    /// <param name="client">Game client receiving the override.</param>
    /// <param name="panelId">The panel's <c>id</c> attribute.</param>
    /// <param name="className">CSS class name without a leading dot.</param>
    /// <param name="classOverride">Class-presence override for this client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="client" /> is <see langword="null" />.</exception>
    void SetClassOverrideForPlayer(IGameClient client,
        string                                  panelId,
        string                                  className,
        CustomHudClassOverride                  classOverride)
    {
        ArgumentNullException.ThrowIfNull(client);
        SetClassOverrideForPlayer(client.Slot, panelId, className, classOverride);
    }

    /// <summary>
    ///     Overrides whether a panel has a pre-defined CSS class for one player controller.
    /// </summary>
    /// <param name="player">Player controller receiving the override.</param>
    /// <param name="panelId">The panel's <c>id</c> attribute.</param>
    /// <param name="className">CSS class name without a leading dot.</param>
    /// <param name="classOverride">Class-presence override for this player.</param>
    /// <exception cref="ArgumentNullException"><paramref name="player" /> is <see langword="null" />.</exception>
    void SetClassOverrideForPlayer(IPlayerController player,
        string                                        panelId,
        string                                        className,
        CustomHudClassOverride                        classOverride)
    {
        ArgumentNullException.ThrowIfNull(player);
        SetClassOverrideForPlayer(player.PlayerSlot, panelId, className, classOverride);
    }

    /// <summary>
    ///     Sets a dialog variable for every player. Labels can reference it with Panorama's
    ///     <c>{s:variable_name}</c> syntax.
    /// </summary>
    /// <param name="panelId">The <c>id</c> of the panel that owns the dialog variable.</param>
    /// <param name="variableName">Variable name without the <c>{s:...}</c> wrapper.</param>
    /// <param name="value">Final text displayed to every player.</param>
    void SetDialogVariableString(string panelId, string variableName, string value);

    /// <summary>
    ///     Sets a dialog-variable override for one player.
    /// </summary>
    /// <param name="playerSlot">Player receiving the override.</param>
    /// <param name="panelId">The <c>id</c> of the panel that owns the dialog variable.</param>
    /// <param name="variableName">Variable name without the <c>{s:...}</c> wrapper.</param>
    /// <param name="value">Override value. An empty string is an explicit empty value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    void SetDialogVariableStringForPlayer(PlayerSlot playerSlot,
        string                                      panelId,
        string                                      variableName,
        string                                      value);

    /// <summary>
    ///     Sets a dialog-variable override for one game client.
    /// </summary>
    /// <param name="client">Game client receiving the override.</param>
    /// <param name="panelId">The <c>id</c> of the panel that owns the dialog variable.</param>
    /// <param name="variableName">Variable name without the <c>{s:...}</c> wrapper.</param>
    /// <param name="value">Override value. An empty string is an explicit empty value.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="client" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    void SetDialogVariableStringForPlayer(IGameClient client,
        string                                     panelId,
        string                                     variableName,
        string                                     value)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(value);
        SetDialogVariableStringForPlayer(client.Slot, panelId, variableName, value);
    }

    /// <summary>
    ///     Sets a dialog-variable override for one player controller.
    /// </summary>
    /// <param name="player">Player controller receiving the override.</param>
    /// <param name="panelId">The <c>id</c> of the panel that owns the dialog variable.</param>
    /// <param name="variableName">Variable name without the <c>{s:...}</c> wrapper.</param>
    /// <param name="value">Override value. An empty string is an explicit empty value.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="player" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    void SetDialogVariableStringForPlayer(IPlayerController player,
        string                                           panelId,
        string                                           variableName,
        string                                           value)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(value);
        SetDialogVariableStringForPlayer(player.PlayerSlot, panelId, variableName, value);
    }

    /// <summary>
    ///     Removes a dialog-variable override for one player slot so it inherits the global value.
    /// </summary>
    /// <param name="playerSlot">Player whose override is removed.</param>
    /// <param name="panelId">The <c>id</c> of the panel that owns the dialog variable.</param>
    /// <param name="variableName">Variable name without the <c>{s:...}</c> wrapper.</param>
    void ClearDialogVariableStringForPlayer(PlayerSlot playerSlot, string panelId, string variableName);

    /// <summary>
    ///     Removes a dialog-variable override for one game client so it inherits the global value.
    /// </summary>
    /// <param name="client">Game client whose override is removed.</param>
    /// <param name="panelId">The <c>id</c> of the panel that owns the dialog variable.</param>
    /// <param name="variableName">Variable name without the <c>{s:...}</c> wrapper.</param>
    /// <exception cref="ArgumentNullException"><paramref name="client" /> is <see langword="null" />.</exception>
    void ClearDialogVariableStringForPlayer(IGameClient client, string panelId, string variableName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ClearDialogVariableStringForPlayer(client.Slot, panelId, variableName);
    }

    /// <summary>
    ///     Removes a dialog-variable override for one player controller so it inherits the global value.
    /// </summary>
    /// <param name="player">Player controller whose override is removed.</param>
    /// <param name="panelId">The <c>id</c> of the panel that owns the dialog variable.</param>
    /// <param name="variableName">Variable name without the <c>{s:...}</c> wrapper.</param>
    /// <exception cref="ArgumentNullException"><paramref name="player" /> is <see langword="null" />.</exception>
    void ClearDialogVariableStringForPlayer(IPlayerController player, string panelId, string variableName)
    {
        ArgumentNullException.ThrowIfNull(player);
        ClearDialogVariableStringForPlayer(player.PlayerSlot, panelId, variableName);
    }

    /// <summary>
    ///     Enables or disables mouse input capture for this layout and player.
    /// </summary>
    /// <remarks>
    ///     Input capture is tracked independently by every layout. Player movement is restored
    ///     only after all layouts have released capture for the player.
    /// </remarks>
    /// <param name="playerSlot">Player whose cursor mode and click detection are changed.</param>
    /// <param name="enabled"><see langword="true" /> to capture input; otherwise, <see langword="false" />.</param>
    void SetInputCaptureEnabled(PlayerSlot playerSlot, bool enabled);

    /// <summary>
    ///     Enables or disables mouse input capture for this layout and game client.
    /// </summary>
    /// <param name="client">Game client whose cursor mode and click detection are changed.</param>
    /// <param name="enabled"><see langword="true" /> to capture input; otherwise, <see langword="false" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="client" /> is <see langword="null" />.</exception>
    void SetInputCaptureEnabled(IGameClient client, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(client);
        SetInputCaptureEnabled(client.Slot, enabled);
    }

    /// <summary>
    ///     Enables or disables mouse input capture for this layout and player controller.
    /// </summary>
    /// <param name="player">Player controller whose cursor mode and click detection are changed.</param>
    /// <param name="enabled"><see langword="true" /> to capture input; otherwise, <see langword="false" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="player" /> is <see langword="null" />.</exception>
    void SetInputCaptureEnabled(IPlayerController player, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(player);
        SetInputCaptureEnabled(player.PlayerSlot, enabled);
    }

    /// <summary>
    ///     Returns whether this layout currently captures input for a player.
    /// </summary>
    /// <param name="playerSlot">Player whose input-capture state is queried.</param>
    /// <returns><see langword="true" /> when this layout currently captures the player's input.</returns>
    bool IsInputCaptureEnabled(PlayerSlot playerSlot);

    /// <summary>
    ///     Returns whether this layout currently captures input for a game client.
    /// </summary>
    /// <param name="client">Game client whose input-capture state is queried.</param>
    /// <returns><see langword="true" /> when this layout currently captures the client's input.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client" /> is <see langword="null" />.</exception>
    bool IsInputCaptureEnabled(IGameClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return IsInputCaptureEnabled(client.Slot);
    }

    /// <summary>
    ///     Returns whether this layout currently captures input for a player controller.
    /// </summary>
    /// <param name="player">Player controller whose input-capture state is queried.</param>
    /// <returns><see langword="true" /> when this layout currently captures the player's input.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="player" /> is <see langword="null" />.</exception>
    bool IsInputCaptureEnabled(IPlayerController player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return IsInputCaptureEnabled(player.PlayerSlot);
    }
}
