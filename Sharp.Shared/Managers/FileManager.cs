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
    ///     获取Source2的文件系统接口指针
    /// </summary>
    nint GetValveFileSystem();

    /// <summary>
    ///     检查文件是否存在 <br />
    ///     <remarks>使用引擎的文件系统, 支持vp访问</remarks>
    /// </summary>
    bool FileExists(string filename, string pathId = "GAME");

    /// <summary>
    ///     打开文件 <br />
    ///     <remarks>使用引擎的文件系统, 支持vp访问</remarks>
    /// </summary>
    IValveFile? OpenFile(string filename, string pathId = "GAME");

    /// <summary>
    ///     打开文件夹 <br />
    ///     <remarks>使用引擎的文件系统, 支持vp访问</remarks>
    /// </summary>
    IValveDirectory? OpenDirectory(string path, string pathId = "GAME");

    /// <summary>
    ///     添加路径到引擎文件系统的搜索路径
    /// </summary>
    void AddSearchPath(string path, string pathId, int addType = 1, int priority = 0, int unknown = 0);

    /// <summary>
    ///     从引擎文件系统移除搜索路径
    /// </summary>
    void RemoveSearchPath(string path, string pathId);
}

public interface IValveFile : INativeObject, IDisposable
{
    /// <summary>
    ///     读
    /// </summary>
    void Read(Span<byte> output);

    /// <summary>
    ///     写
    /// </summary>
    void Write(ReadOnlySpan<byte> input);

    /// <summary>
    ///     文件大小
    /// </summary>
    int Size();
}

public interface IValveDirectory : IDisposable
{
    /// <summary>
    ///     迭代文件夹
    /// </summary>
    IEnumerator<string> GetEnumerator();
}
