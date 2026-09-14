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

using Sharp.Shared.Types;

namespace Sharp.Shared.Enums;

public enum ScriptValueType
{
    Null   = 0,
    Bool   = 1,
    Number = 2,
    String = 3,
    Entity = 4,
    Vector = 5,

    /// <summary>
    ///     JS object. <see cref="ScriptValue.Handle" /> is an index into this call's handle table;
    ///     read/write via <see cref="ScriptObject" />.
    /// </summary>
    Object = 6,

    /// <summary>
    ///     JS array. <see cref="ScriptValue.Handle" /> is an index into this call's handle table;
    ///     read/write via <see cref="ScriptArray" />.
    /// </summary>
    Array = 7,

    /// <summary>
    ///     Used when raising ThrowError; do not use this unless you know what you are doing.<br />
    ///     Wire-only sentinel: a thrown error (not any value type), hence -1 outside the value range.
    /// </summary>
    Error = -1,
}
