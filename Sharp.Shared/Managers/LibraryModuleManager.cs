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

namespace Sharp.Shared.Managers;

/// <summary>
///     Provides access to loaded game libraries (modules) for memory scanning and address retrieval.
/// </summary>
/// <remarks>
///     <para>
///         This interface exposes handles to common game modules like <c>server.dll</c> (Windows) or <c>libserver.so</c> (Linux).
///     </para>
///     <para>
///         Use the returned <see cref="ILibraryModule"/> instances to perform operations such as <c>FindPattern</c>, or accessing exported functions.
///     </para>
/// </remarks>
public interface ILibraryModuleManager
{
    ILibraryModule Server                { get; }
    ILibraryModule Engine                { get; }
    ILibraryModule Tier0                 { get; }
    ILibraryModule Schema                { get; }
    ILibraryModule Resource              { get; }
    ILibraryModule VScript               { get; }
    ILibraryModule VPhysics              { get; }
    ILibraryModule SoundSystem           { get; }
    ILibraryModule NetworkSystem         { get; }
    ILibraryModule WorldRenderer         { get; }
    ILibraryModule MatchMaking           { get; }
    ILibraryModule FileSystem            { get; }
    ILibraryModule SteamNetworkingSocket { get; }
    ILibraryModule MaterialSystem        { get; }
}
