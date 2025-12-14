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
using System.Collections.Generic;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;

namespace Sharp.Shared;

public record SchemaClass
{
    public required string                                     ClassName   { get; init; }
    public required ushort                                     ChainOffset { get; init; }
    public required FrozenDictionary<string, SchemaClassField> Fields      { get; init; }
    public required List<string>                               BaseClasses { get; init; }
}

public record SchemaClassField
{
    public required int                Offset    { get; init; }
    public required bool               Networked { get; init; }
    public required string             Type      { get; init; }
    public required bool               IsArray   { get; init; }
    public required SchemaTypeCategory Category  { get; init; }
}

public record SchemaField
{
    public int  Offset      { get; init; }
    public bool Networked   { get; init; }
    public int  ChainOffset { get; init; }
    public int  ArraySize   { get; init; }
}

/// <summary>
///     Do not modify anything here if you don't know what you're doing <br />
///     <remarks>Modifying can lead to unexpected results</remarks>
/// </summary>
public static class SharedGameObject
{
    public static FrozenDictionary<string, SchemaClass>         SchemaInfo                = null!;
    public static FrozenDictionary<ushort, IEconItemDefinition> EconItemDefinitionsById   = null!;
    public static FrozenDictionary<string, IEconItemDefinition> EconItemDefinitionsByName = null!;
    public static FrozenDictionary<uint, IPaintKit>             PaintKits                 = null!;
}
