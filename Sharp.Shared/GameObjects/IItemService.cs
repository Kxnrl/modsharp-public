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

using Sharp.Shared.Attributes;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;

namespace Sharp.Shared.GameObjects;

/// <summary>
///     ItemServices is only for CCSPlayerPawn
/// </summary>
[NetClass("CCSPlayer_ItemServices")]
public interface IItemService : IPlayerPawnComponent
{
    /// <summary>
    ///     Does the player have a defuser
    /// </summary>
    bool HasDefuser { get; set; }

    /// <summary>
    ///     Does the player have a helmet
    /// </summary>
    bool HasHelmet { get; set; }

    IBaseWeapon? GiveNamedItem(string classname);

    IBaseWeapon? GiveNamedItem(EconItemId id);

    void RemoveAllItems(bool removeSuit);
}