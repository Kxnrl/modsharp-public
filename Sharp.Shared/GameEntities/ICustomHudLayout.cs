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
    void SetClassOverride(string panelId, string className, HudPanelClassStatus classStatus);

    /// <summary>
    ///     Overrides whether a panel has a pre-defined CSS class for single player.
    /// </summary>
    void SetClassOverrideForPlayer(PlayerSlot playerSlot, string panelId, string className, HudPanelClassStatus classStatus);

    /// <summary>
    ///     Overrides whether a panel has a pre-defined CSS class for single player.
    /// </summary>
    void SetClassOverrideForPlayer(IGameClient client, string panelId, string className, HudPanelClassStatus classStatus)
    {
        ArgumentNullException.ThrowIfNull(client);
        SetClassOverrideForPlayer(client.Slot, panelId, className, classStatus);
    }

    /// <summary>
    ///     Overrides whether a panel has a pre-defined CSS class for single player.
    /// </summary>
    void SetClassOverrideForPlayer(IPlayerController player, string panelId, string className, HudPanelClassStatus classStatus)
    {
        ArgumentNullException.ThrowIfNull(player);
        SetClassOverrideForPlayer(player.PlayerSlot, panelId, className, classStatus);
    }

    /// <summary>
    ///     Sets a dialog variable for every player. Labels can reference it with Panorama's
    ///     <c>{s:variable_name}</c> syntax.
    /// </summary>
    void SetDialogVariableString(string panelId, string variableName, string value);

    /// <summary>
    ///     Sets a dialog-variable override for single player.
    /// </summary>
    void SetDialogVariableStringForPlayer(PlayerSlot playerSlot, string panelId, string variableName, string value);

    /// <summary>
    ///     Sets a dialog-variable override for single player.
    /// </summary>
    void SetDialogVariableStringForPlayer(IGameClient client, string panelId, string variableName, string value)
    {
        ArgumentNullException.ThrowIfNull(client);
        SetDialogVariableStringForPlayer(client.Slot, panelId, variableName, value);
    }

    /// <summary>
    ///     Sets a dialog-variable override for single player.
    /// </summary>
    void SetDialogVariableStringForPlayer(IPlayerController player, string panelId, string variableName, string value)
    {
        ArgumentNullException.ThrowIfNull(player);
        SetDialogVariableStringForPlayer(player.PlayerSlot, panelId, variableName, value);
    }

    /// <summary>
    ///     Removes a dialog-variable override for single player so it inherits the global value.
    /// </summary>
    void ClearDialogVariableStringForPlayer(PlayerSlot playerSlot, string panelId, string variableName);

    /// <summary>
    ///     Removes a dialog-variable override for single player so it inherits the global value.
    /// </summary>
    void ClearDialogVariableStringForPlayer(IGameClient client, string panelId, string variableName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ClearDialogVariableStringForPlayer(client.Slot, panelId, variableName);
    }

    /// <summary>
    ///     Removes a dialog-variable override for single player so it inherits the global value.
    /// </summary>
    void ClearDialogVariableStringForPlayer(IPlayerController player, string panelId, string variableName)
    {
        ArgumentNullException.ThrowIfNull(player);
        ClearDialogVariableStringForPlayer(player.PlayerSlot, panelId, variableName);
    }

    /// <summary>
    ///     Enables or disables mouse input capture for this layout for a player.
    /// </summary>
    /// <remarks>
    ///     The server stores a single capture flag per (layout, player) and networks it to the client.
    ///     How capture from multiple layouts combines — and whether or when player movement is affected
    ///     or restored — is decided by the client HUD and is not guaranteed by this call.
    /// </remarks>
    void SetInputCaptureEnabled(PlayerSlot playerSlot, bool enabled);

    /// <summary>
    ///     Enables or disables mouse input capture for this layout for a player.
    /// </summary>
    void SetInputCaptureEnabled(IGameClient client, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(client);
        SetInputCaptureEnabled(client.Slot, enabled);
    }

    /// <summary>
    ///     Enables or disables input capture for this layout for a player.
    /// </summary>
    void SetInputCaptureEnabled(IPlayerController player, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(player);
        SetInputCaptureEnabled(player.PlayerSlot, enabled);
    }

    /// <summary>
    ///     Returns whether this layout currently captures input for a player.
    /// </summary>
    bool IsInputCaptureEnabled(PlayerSlot playerSlot);

    /// <summary>
    ///     Returns whether this layout currently captures input for a player.
    /// </summary>
    bool IsInputCaptureEnabled(IGameClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return IsInputCaptureEnabled(client.Slot);
    }

    /// <summary>
    ///     Returns whether this layout currently captures input for a player.
    /// </summary>
    bool IsInputCaptureEnabled(IPlayerController player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return IsInputCaptureEnabled(player.PlayerSlot);
    }
}
