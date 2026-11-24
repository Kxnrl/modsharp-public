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

namespace Sharp.Modules.InputManager.Shared;

/// <summary>
///     Input keys mapped to default CS2 key bindings.
/// </summary>
public enum InputKey
{
    W,
    S,
    A,
    D,
    F,
    Tab,
    E,
    R,
    Space,
    Shift,
    Attack1,
    Attack2,

    [Obsolete("Currently does nothing. It will be implemented in the future release. This is just a placeholder.")]
    F3,

    [Obsolete("Currently does nothing. It will be implemented in the future release. This is just a placeholder.")]
    F4,

    [Obsolete("Currently does nothing. It will be implemented in the future release. This is just a placeholder.")]
    G,
}
