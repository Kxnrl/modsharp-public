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

namespace Sharp.Shared.Hooks;

public interface IRuntimeNativeHook
{
    /// <summary>
    ///     Install hook
    /// </summary>
    /// <returns>
    ///     Whether it succeeded; returns <c>false</c> without touching the target when the hook is not prepared,
    ///     is already installed, or the underlying hook cannot be created
    /// </returns>
    bool Install();

    /// <summary>
    ///     Uninstall hook
    /// </summary>
    /// <remarks>
    ///     Multiple hooks stacked on the same target must be uninstalled in reverse install order;
    ///     when stacking hooks, consider keeping them in a shared library
    /// </remarks>
    void Uninstall();

    /// <summary>
    ///     Original address
    /// </summary>
    nint Trampoline { get; }
}
