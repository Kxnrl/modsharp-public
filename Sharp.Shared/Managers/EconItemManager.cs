/*
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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

using System.Collections.Frozen;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;

namespace Sharp.Shared.Managers;

public interface IEconItemManager
{
    /// <summary>
    ///     Get dictionary of all EconItems
    /// </summary>
    FrozenDictionary<ushort, IEconItemDefinition> GetEconItems();

    /// <summary>
    ///     Get dictionary of all PaintKits
    /// </summary>
    FrozenDictionary<uint, IPaintKit> GetPaintKits();

    /// <summary>
    ///     Get EconItemDefinition by index
    /// </summary>
    IEconItemDefinition? GetEconItemDefinitionByIndex(ushort index);

    /// <summary>
    ///     Get EconItemDefinition by index
    /// </summary>
    IEconItemDefinition? GetEconItemDefinitionByIndex(EconItemId index);

    /// <summary>
    ///     Get EconItemDefinition by name
    /// </summary>
    IEconItemDefinition? GetEconItemDefinitionByName(string name);
}
