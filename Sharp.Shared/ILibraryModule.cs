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

using System.Collections.Generic;

namespace Sharp.Shared;

public interface ILibraryModule
{
    /// <summary>
    ///     通过IDA Pattern找函数地址 (不唯一) <br />
    ///     <remarks>该方法通常用于遍历地址</remarks>
    /// </summary>
    nint FindPattern(string pattern, nint startAddress = 0);

    /// <summary>
    ///     查找虚表
    /// </summary>
    nint GetVirtualTableByName(string tableName, bool decorated = false);

    /// <summary>
    ///     从<b>.text</b>中查找函数地址
    /// </summary>
    nint GetFunctionByName(string functionName);

    /// <summary>
    ///     通过IDA Pattern找函数地址 (唯一)
    /// </summary>
    nint FindPatternExactly(string pattern);

    /// <summary>
    ///     查找Game VInterface
    /// </summary>
    nint FindInterface(string interfaceName);

    /// <summary>
    ///     通过IDA Pattern找函数地址
    /// </summary>
    List<nint> FindPatternMulti(string pattern);

    /// <summary>
    ///     查找string
    /// </summary>
    nint FindString(string str);

    /// <summary>
    ///     查找指针
    /// </summary>
    nint FindPtr(nint ptr);
}
