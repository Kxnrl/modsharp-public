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

namespace Sharp.Modules.CommandCenter.Shared;

public interface ICommandCenter
{
    const string Identity = nameof(ICommandCenter);

    /// <summary>
    ///     Gets or creates the command registry for the specified module identity.
    /// </summary>
    /// <param name="moduleIdentity">
    ///     The unique identity of the module. Pass the module's assembly name so registrations are cleaned up automatically on <c>OnLibraryDisconnect</c>.
    /// </param>
    /// <returns>The module-scoped <see cref="ICommandRegistry" />.</returns>
    public ICommandRegistry GetRegistry(string moduleIdentity);
}
