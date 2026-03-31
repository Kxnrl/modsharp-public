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

using System;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Core;

public readonly record struct PreviousMenu(Func<IGameClient, Menu> MenuFactory, Menu Menu, int Page, int Cursor);

public readonly record struct BuiltMenuItem(string                   Title,
    MenuItemState            State,
    float                    Width,
    Action<IMenuController>? Action     = null,
    string?                  Color      = null,
    MenuItemActionKind       ActionKind = MenuItemActionKind.None,
    string?                  HintText   = null);
