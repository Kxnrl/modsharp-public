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

using System;
using Sharp.Shared.GameEntities;
using UnitGenerator;

namespace Sharp.Shared.Units;

[UnitOf<byte>(UnitGenerateOptions.ImplicitOperator
              | UnitGenerateOptions.ParseMethod
              | UnitGenerateOptions.ArithmeticOperator
              | UnitGenerateOptions.ValueArithmeticOperator
              | UnitGenerateOptions.Comparable
              | UnitGenerateOptions.JsonConverter
              | UnitGenerateOptions.JsonConverterDictionaryKeySupport)]
public partial struct PlayerSlot
{
    public static readonly PlayerSlot MaxPlayerSlot = new (63);

    public static readonly PlayerSlot MaxPlayerCount = new (64);

    public PlayerSlot(EntityIndex index)
    {
        if (index.AsPrimitive() is > 64 or <= 0)
        {
            throw new ArgumentException("Invalid player slot from EntityIndex");
        }

        value = (byte) (index.AsPrimitive() - 1);
    }

    public PlayerSlot(IPlayerController controller)
        => value = controller.PlayerSlot.AsPrimitive();

    public bool IsValid()
        => value <= MaxPlayerSlot.AsPrimitive();
}
