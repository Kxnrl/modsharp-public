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

using Sharp.Shared.Enums;

namespace Sharp.Shared.Types;

public readonly record struct HookReturnValue<T>
{
    // Hook的操作类型
    public EHookAction Action { get; }

    /// <summary>
    ///     Hook的返回值, 如果是void则为空
    /// </summary>
    public T? ReturnValue { get; }

    public HookReturnValue(EHookAction action)
    {
        Action      = action;
        ReturnValue = default;
    }

    public HookReturnValue(EHookAction action, T? returnValue)
    {
        Action      = action;
        ReturnValue = returnValue;
    }
}
