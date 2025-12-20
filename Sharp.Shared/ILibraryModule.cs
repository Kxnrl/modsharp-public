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
    /// <param name="pattern">The IDA-style signature string (e.g. "48 89 ? 24 ??")</param>
    nint FindPattern(string pattern, nint startAddress = 0);

    /// <summary>
    ///     Find virtual table by name
    /// </summary>
    /// <param name="tableName">The name of the vtable</param>
    /// <param name="decorated">If true, will treat <paramref name="tableName"/> as the mangled (symbol) name</param>
    nint GetVirtualTableByName(string tableName, bool decorated = false);

    /// <summary>
    ///     Get exported function address (similar to GetProcAddress or dlsym).
    /// </summary>
    /// <param name="functionName">The name of the exported function</param>
    nint GetFunctionByName(string functionName);

    /// <summary>
    ///     Find function address by IDA pattern (expecting a unique match).
    /// </summary>
    /// <param name="pattern">The IDA-style signature string (e.g. "48 89 ? 24 ??")</param>
    /// <returns>The address of the match, or 0 if not found/ambiguous</returns>
    nint FindPatternExactly(string pattern);

    /// <summary>
    ///     Find a game VInterface pointer exposed by this module (via CreateInterface).
    /// </summary>
    /// <param name="interfaceName">The versioned interface name (e.g., "Source2Server001")</param>
    nint FindInterface(string interfaceName);

    /// <summary>
    ///     Find multiple function addresses by IDA pattern.
    /// </summary>
    /// <param name="pattern">The IDA-style signature string (e.g. "48 89 ? 24 ??")</param>
    /// <returns>A list of all memory addresses matching the pattern</returns>
    List<nint> FindPatternMulti(string pattern);

    /// <summary>
    ///     Find address of the given string in the module's data section.
    /// </summary>
    /// <param name="str">The string literal to search for</param>
    nint FindString(string str);

    /// <summary>
    ///     Find the address in memory that contains a pointer to the specific value provided.
    /// </summary>
    /// <param name="ptr">The value/address to search for within the module's memory space</param>
    nint FindPtr(nint ptr);
}
