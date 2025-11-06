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

using System.Runtime.CompilerServices;
using Sharp.Shared.Objects;
using UnitGenerator;

namespace Sharp.Shared.Units;

[UnitOf<int>(UnitGenerateOptions.ImplicitOperator
             | UnitGenerateOptions.ParseMethod
             | UnitGenerateOptions.ArithmeticOperator
             | UnitGenerateOptions.ValueArithmeticOperator
             | UnitGenerateOptions.Comparable
             | UnitGenerateOptions.JsonConverter)]
public partial struct EntityIndex
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNetworked()
        => value is >= 0 and < 16384;

    public static readonly EntityIndex WorldIndex   = 0;
    public static readonly EntityIndex InvalidIndex = -1;

    public EntityIndex(PlayerSlot slot)
        => value = slot.AsPrimitive() + 1;

    public EntityIndex(IGameClient client)
        => value = client.Slot.AsPrimitive() + 1;
}
