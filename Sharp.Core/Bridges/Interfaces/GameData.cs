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
using Sharp.Core.CStrike;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Bridges.Interfaces;

[NativeVirtualObject(HasDestructors = true)]
internal unsafe partial class GameData : NativeObject, IGameData
{
    private static readonly HashSet<string> RegisteredPaths = new (StringComparer.OrdinalIgnoreCase);

#region Native

    public partial bool GetOffset(string name, out int offset);

    public partial bool GetAddress(string name, out nint address);

    public partial bool GetVFuncIndex(string name, out int index);

    public partial bool Register(string path, byte* error, int maxlength);

    public partial bool Unregister(string path, byte* error, int maxlength);

#endregion

    public int GetVFuncIndex(string classname, string name)
    {
        if (GetVFuncIndex($"{classname}::{name}", out var offset))
        {
            return offset;
        }

        var baseClasses = SharedGameObject.SchemaInfo[classname].BaseClasses;

        foreach (var baseClass in baseClasses)
        {
            if (GetVFuncIndex($"{baseClass}::{name}", out offset))
            {
                return offset;
            }
        }

        throw new KeyNotFoundException($"Failed to find VFunc index \"{name}\" with classname \"{classname}\" in GameData");
    }

    public void Register(string path)
    {
        if (!path.EndsWith(".jsonc", StringComparison.OrdinalIgnoreCase))
        {
            path = $"{path}.jsonc";
        }

        if (!RegisteredPaths.Add(path))
        {
            return;
        }

        var error = stackalloc byte[512];

        if (!Register(path, error, 512))
        {
            throw new InvalidOperationException(NativeString.ReadString(error));
        }
    }

    public void Unregister(string path)
    {
        if (!path.EndsWith(".jsonc", StringComparison.OrdinalIgnoreCase))
        {
            path = $"{path}.jsonc";
        }

        if (!RegisteredPaths.Remove(path))
        {
            return;
        }

        var error = stackalloc byte[512];

        if (!Unregister(path, error, 512))
        {
            throw new InvalidOperationException(NativeString.ReadString(error));
        }
    }

    public int GetOffset(string name)
        => GetOffset(name, out var offset) ? offset : throw new KeyNotFoundException(name);

    public int GetOffset(string classname, string name)
    {
        if (GetOffset($"{classname}::{name}", out var offset))
        {
            return offset;
        }

        var baseClasses = SharedGameObject.SchemaInfo[classname].BaseClasses;

        foreach (var baseClass in baseClasses)
        {
            if (GetOffset($"{baseClass}::{name}", out offset))
            {
                return offset;
            }
        }

        throw new KeyNotFoundException($"Failed to find Offset \"{name}\" with classname \"{classname}\" in GameData");
    }

    public int GetVFuncIndex(string name)
        => GetVFuncIndex(name, out var idx) ? idx : throw new KeyNotFoundException(name);

    public nint GetAddress(string name)
        => GetAddress(name, out var address) ? address : throw new KeyNotFoundException(name);

    public nint GetAddress(string classname, string name)
    {
        if (GetAddress($"{classname}::{name}", out var address))
        {
            return address;
        }

        var baseClasses = SharedGameObject.SchemaInfo[classname].BaseClasses;

        foreach (var baseClass in baseClasses)
        {
            if (GetAddress($"{baseClass}::{name}", out address))
            {
                return address;
            }
        }

        throw new KeyNotFoundException($"Failed to find MemSig \"{name}\" with classname \"{classname}\" in GameData");
    }

    internal nint GetRequiredVirtualTable(string module, string classname)
    {
        var entry = LibraryModule.Create(Natives.Core.GetLibraryModule(module))
                    ?? throw new EntryPointNotFoundException($"Model '{module}' is nullptr");

        var vtable = entry.GetVirtualTableByName(classname);

        if (vtable == nint.Zero)
        {
            throw new EntryPointNotFoundException($"Failed to get '{classname}' vtable in '{module}'");
        }

        return vtable;
    }

    internal nint GetRequiredAddress(string name)
    {
        var ptr = GetAddress(name);

        if (ptr == nint.Zero)
        {
            throw new EntryPointNotFoundException($"Failed to get address of '{name}'");
        }

        return ptr;
    }
}
