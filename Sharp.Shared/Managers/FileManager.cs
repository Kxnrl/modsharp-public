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

using System;
using System.Collections.Generic;
using Sharp.Shared.CStrike;

namespace Sharp.Shared.Managers;

public interface IFileManager
{
    /// <summary>
    ///     Get Source2 file system interface pointer
    /// </summary>
    nint GetValveFileSystem();

    /// <summary>
    ///     Check if file exists <br />
    ///     <remarks>Uses engine file system, supports vp access</remarks>
    /// </summary>
    bool FileExists(string filename, string pathId = "GAME");

    /// <summary>
    ///     Open file <br />
    ///     <remarks>Uses engine file system, supports vp access</remarks>
    /// </summary>
    IValveFile? OpenFile(string filename, string pathId = "GAME");

    /// <summary>
    ///     Open directory <br />
    ///     <remarks>Uses engine file system, supports vp access</remarks>
    /// </summary>
    IValveDirectory? OpenDirectory(string path, string pathId = "GAME");

    /// <summary>
    ///     Add path to engine file system search paths
    /// </summary>
    void AddSearchPath(string path, string pathId, int addType = 1, int priority = 0, int unknown = 0);

    /// <summary>
    ///     Remove search path from engine file system
    /// </summary>
    void RemoveSearchPath(string path, string pathId);
}

public interface IValveFile : INativeObject, IDisposable
{
    /// <summary>
    ///     Read data from file
    /// </summary>
    void Read(Span<byte> output);

    /// <summary>
    ///     Write data to file
    /// </summary>
    void Write(ReadOnlySpan<byte> input);

    /// <summary>
    ///     Get file size
    /// </summary>
    int Size();
}

public interface IValveDirectory : IDisposable
{
    /// <summary>
    ///     Iterate through directory
    /// </summary>
    IEnumerator<string> GetEnumerator();
}
