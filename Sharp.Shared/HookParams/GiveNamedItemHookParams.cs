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

using Sharp.Shared.GameObjects;

namespace Sharp.Shared.HookParams;

public interface IGiveNamedItemHookParams : IFunctionParams, IPlayerPawnFunctionParams
{
    /// <summary>
    ///     Gets the class name of the item (e.g. "weapon_ak47").
    /// </summary>
    string Classname { get; }

    /// <summary>
    ///     Gets a value indicating whether to ignore the CEconItemView.
    ///     If <c>true</c>, the given item will not have a skin.
    /// </summary>
    bool IgnoreCEconItemView { get; }

    /// <summary>
    ///     Gets the CEconItemView passed to the engine.
    ///     If it is <c>null</c> or not <see cref="IEconItemView.Initialized" />,
    ///     the engine falls back to the player's loadout.
    /// </summary>
    IEconItemView? EconItemView { get; }

    /// <summary>
    ///     Overrides the parameters used to give the item.
    /// </summary>
    /// <param name="classname">The new item class name (e.g. "weapon_ak47").</param>
    /// <param name="ignoreCEconItemView">If set to <c>true</c>, the item will be created without a skin.</param>
    void SetOverride(string classname, bool ignoreCEconItemView = false);

    /// <summary>
    ///     Replaces the CEconItemView pointer passed to the engine.
    ///     The memory is owned by the caller and must stay valid at least until the hooked GiveNamedItem call returns.
    ///     Has no effect when <see cref="IgnoreCEconItemView" /> is <c>true</c>.
    /// </summary>
    void SetEconItemView(nint econItemView);

    /// <inheritdoc cref="SetEconItemView(nint)" />
    void SetEconItemView(IEconItemView? econItemView);
}
