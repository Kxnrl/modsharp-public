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
using System.Collections.Generic;
using Sharp.Shared.Types;

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
    /// <param name="decorated">If true, will treat <paramref name="tableName" /> as the mangled (symbol) name</param>
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
    ///     Find address of the given string in the module's data section.
    ///     An exact match ensures the string is not a partial substring of another string by verifying it is located at the start of the data or immediately preceded by a null terminator.
    ///     (e.g., searching for "Error" will match a standalone "Error", but will skip over "FatalError").
    /// </summary>
    /// <param name="str">The string literal to search for</param>
    nint FindStringExact(string str);

    /// <summary>
    ///     Find the address in memory that contains a pointer to the specific value provided.
    /// </summary>
    /// <param name="ptr">The value/address to search for within the module's memory space</param>
    nint FindPtr(nint ptr);

    /// <summary>
    ///     Finds virtual tables that contain the specified partial string in their type descriptor or name.
    /// </summary>
    /// <param name="str">The partial name to search for.</param>
    VirtualTableInfo[] FindVirtualTablesPartial(string str);

    /// <summary>
    ///     Checks if the object instance at the specified pointer inherits from a class matching the given name.
    /// </summary>
    /// <param name="ptr">The pointer to the object instance (e.g. <c>this</c> pointer).</param>
    /// <param name="name">The partial or full name of the class/interface to check for in the inheritance tree.</param>
    /// <returns><c>true</c> if the object is derived from the specified class name; otherwise, <c>false</c>.</returns>
    bool IsPointerDerivedFrom(nint ptr, string name);

    /// <summary>
    ///     Finds all references to the specified pointer within the module's executable code section (.text).
    /// </summary>
    /// <param name="ptr">The target pointer to find references to.</param>
    /// <returns>An array of addresses (instructions) in the .text section that reference <paramref name="ptr" />.</returns>
    nint[] GetReferencesFromPointer(nint ptr);

    /// <summary>
    ///     Finds the start address of a function that references the specific string literal.
    /// </summary>
    /// <param name="str">The string literal to look for references to.</param>
    /// <returns>The function address, or throws/returns 0 if not found.</returns>
    nint FindFunction(string str);

    /// <summary>
    ///     Finds the start address of a function that references the specific string literal.
    /// </summary>
    /// <param name="str">The string literal to look for references to.</param>
    /// <returns>The function address, or throws/returns 0 if not found.</returns>
    nint FindFunction(ReadOnlySpan<string> strs);

    /// <summary>
    ///     Finds all functions that reference the specific string literal.
    /// </summary>
    /// <param name="str">The string literal to look for references to.</param>
    nint[] FindFunctions(string str);

    /// <summary>
    ///     Finds all functions that reference the provided string literals.
    /// </summary>
    /// <param name="strs">A span of string literals.</param>
    nint[] FindFunctions(ReadOnlySpan<string> strs);

    /// <summary>
    ///     Finds the start address of a function that references the specific pointer (xref).
    /// </summary>
    /// <param name="ptr">The pointer to find references to.</param>
    nint FindFunction(nint ptr);

    /// <summary>
    ///     Finds the start address of a function that references any of the specific pointers (xref).
    /// </summary>
    /// <param name="ptrs">A span of pointers to find references to.</param>
    nint FindFunction(ReadOnlySpan<nint> ptrs);

    /// <summary>
    ///     Finds all functions that reference the specific pointer.
    /// </summary>
    /// <param name="ptr">The pointer to find references to.</param>
    nint[] FindFunctions(nint ptr);

    /// <summary>
    ///     Finds all functions that reference the provided pointers.
    /// </summary>
    /// <param name="ptrs">A span of pointers to find references to.</param>
    nint[] FindFunctions(ReadOnlySpan<nint> ptrs);

    /// <summary>
    ///     Scans the module's known data sections for a specific byte sequence.
    ///     <remarks>
    ///         This method skips executable (code) segments.
    ///     </remarks>
    /// </summary>
    /// <param name="data">The byte sequence to search for.</param>
    /// <param name="readOnly">
    ///     If <c>true</c>, restricts the search to read-only memory segments (e.g. <c>.rdata</c>).
    ///     If <c>false</c>, scans all non-executable segments (including writable <c>.data</c>).
    /// </param>
    nint FindData(ReadOnlySpan<byte> data, bool readOnly);

    /// <summary>
    ///     Determines the start and end boundaries of a function given an address inside it.
    /// </summary>
    /// <param name="middle">An address somewhere inside the function body.</param>
    /// <param name="start">The resolved start address of the function.</param>
    /// <param name="end">The resolved end address of the function.</param>
    /// <returns>True if the function identification was successful.</returns>
    bool GetFunctionRange(nint middle, out nint start, out nint end);
}
