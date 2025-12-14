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
    ///     Find function address by IDA pattern (non-unique) <br />
    ///     <remarks>This method is typically used for iterating through addresses</remarks>
    /// </summary>
    nint FindPattern(string pattern, nint startAddress = 0);

    /// <summary>
    ///     Find virtual table by name
    /// </summary>
    nint GetVirtualTableByName(string tableName, bool decorated = false);

    /// <summary>
    ///     Get exported function address
    /// </summary>
    nint GetFunctionByName(string functionName);

    /// <summary>
    ///     Find function address by IDA pattern (unique match)
    /// </summary>
    nint FindPatternExactly(string pattern);

    /// <summary>
    ///     Find game VInterface
    /// </summary>
    nint FindInterface(string interfaceName);

    /// <summary>
    ///     Find multiple function addresses by IDA pattern
    /// </summary>
    List<nint> FindPatternMulti(string pattern);

    /// <summary>
    ///     Find address of the given string in data section
    /// </summary>
    nint FindString(string str);

    /// <summary>
    ///     Find address whose value equals to the given pointer
    /// </summary>
    nint FindPtr(nint ptr);
}
