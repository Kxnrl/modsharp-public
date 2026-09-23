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

using Sharp.Core.CStrike;
using Sharp.Generator;

namespace Sharp.Core.Bridges.Interfaces;

internal unsafe partial class FullFileSystem : NativeObject
{
    [NativeVirtualMethod(Offset = 11, LinuxOffset = 11)]
    public partial nint Read(byte* output, int size, nint fileHandle);

    [NativeVirtualMethod(Offset = 12, LinuxOffset = 12)]
    public partial nint Write(byte* input, int size, nint fileHandle);

    [NativeVirtualMethod(Offset = 13, LinuxOffset = 13)]
    public partial nint Open(string filename, string options, string pathId);

    [NativeVirtualMethod(Offset = 14, LinuxOffset = 14)]
    public partial void Close(nint fileHandle);

    [NativeVirtualMethod(Offset = 18, LinuxOffset = 17)]
    public partial int Size(nint fileHandle);

    [NativeVirtualMethod(Offset = 21, LinuxOffset = 21)]
    public partial bool FileExists(string filename, string pathId);

    [NativeVirtualMethod(Offset = 31, LinuxOffset = 31)]
    public partial void AddSearchPath(string path, string pathId, int addType, int priority, int unknown);

    [NativeVirtualMethod(Offset = 32, LinuxOffset = 32)]
    public partial void RemoveSearchPath(string path, string pathId);

    [NativeVirtualMethod(Offset = 61, LinuxOffset = 60)]
    public partial byte* FindNext(FileFindHandle_t* fileFindHandle);

    [NativeVirtualMethod(Offset = 62, LinuxOffset = 61)]
    public partial bool FindIsDirectory(FileFindHandle_t* fileFindHandle);

    [NativeVirtualMethod(Offset = 63, LinuxOffset = 62)]
    public partial void FindClose(FileFindHandle_t* fileFindHandle);

    [NativeVirtualMethod(Offset = 64, LinuxOffset = 63)]
    public partial byte* FindFirstEx(string wildCard, string pathId, FileFindHandle_t* pFileFindHandle);
}

public struct FileFindHandle_t
{
#pragma warning disable CS0169 // 字段从未被使用

    // ReSharper disable InconsistentNaming

    private nint m_pFileSystem;

    private nint m_pFindDataIndex;

    // ReSharper restore InconsistentNaming

#pragma warning restore CS0169 // 字段从未被使用
}
