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
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.Shared;

/// <summary>
///     Custom target resolver registered with <see cref="ITargetingManager.RegisterResolver" />.
/// </summary>
public interface ITargetResolver
{
    /// <summary>
    ///     Returns the target token handled by this resolver (must be non-empty and start with <c>'@'</c>, e.g. <c>"@vip"</c>).
    /// </summary>
    string GetTarget();

    /// <summary>
    ///     Resolves matching clients for this target token.
    /// </summary>
    /// <param name="activator">The client who initiated targeting, or <see langword="null" /> for server console.</param>
    /// <returns>Matching game clients.</returns>
    IEnumerable<IGameClient> Resolve(IGameClient? activator);
}
