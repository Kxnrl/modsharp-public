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

namespace Sharp.Shared.Types;

public readonly record struct VirtualTableInfo
{
    /// <summary>
    ///     Demangled vtable name
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         On Windows the demangled name has prefixes, such as 'class' or 'struct' (.?AVCNetworkGameServer@@ --> class
    ///         CNetworkGameServer)
    ///     </para>
    ///     While this is not the case on Linux (18CNetworkGameServer --> CNetworkGameServer)
    /// </remarks>
    public required string DemangledName { get; init; }

    /// <summary>
    ///     Vtable address
    /// </summary>
    public nint Address { get; init; }

    /// <summary>
    ///     Offset to Primary vtable address
    /// </summary>
    /// <remarks>
    ///     For example:
    ///     <para>0x1000               Primary vtable address of CNetworkGameServer</para>
    ///     <para>0x1008(offset = 8)   Vtable address of the first base class</para>
    ///     <para>0x1010(offset = 16)  Vtable address of the second base class</para>
    /// </remarks>
    public ulong Offset { get; init; }
}
