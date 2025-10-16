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
    ///     获取所有EconItem的字典
    /// </summary>
    FrozenDictionary<ushort, IEconItemDefinition> GetEconItems();

    /// <summary>
    ///     获取所有PaintKit的字典
    /// </summary>
    FrozenDictionary<uint, IPaintKit> GetPaintKits();

    /// <summary>
    ///     获取EconItemDefinition定义
    /// </summary>
    IEconItemDefinition? GetEconItemDefinitionByIndex(ushort index);

    /// <summary>
    ///     获取EconItemDefinition定义
    /// </summary>
    IEconItemDefinition? GetEconItemDefinitionByIndex(EconItemId index);

    /// <summary>
    ///     获取EconItemDefinition定义
    /// </summary>
    IEconItemDefinition? GetEconItemDefinitionByName(string name);
}
