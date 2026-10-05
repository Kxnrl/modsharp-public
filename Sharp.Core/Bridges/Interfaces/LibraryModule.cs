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
using System.Runtime.InteropServices;
using Sharp.Core.CStrike;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Bridges.Interfaces;

[NativeVirtualObject(HasDestructors = true)]
internal unsafe partial class LibraryModule : NativeObject, ILibraryModule
{
#region Native

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct RuntimeVirtualTableInfo
    {
        public CUtlString DemangledName { get; set; }
        public nint       Address       { get; set; }
        public ulong      Offset        { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct RuntimeExportInfo
    {
        public CUtlString RawName   { get; set; }
        public CUtlString Signature { get; set; }
        public nint       Address   { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct RuntimeModuleInfo
    {
        public nint  BaseAddress { get; set; }
        public nuint Size        { get; set; }
        public nint  Name        { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct RuntimeSegmentInfo
    {
        public nint         Address { get; set; }
        public nuint        Size    { get; set; }
        public MemoryAccess Access  { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct RuntimeFunctionInfo
    {
        public nint Start { get; set; }
        public nint End   { get; set; }
    }

    public partial nint FindPatternEx(string svPattern, nint startAddress = 0);

    public partial nint GetVTableByNameEx(string svTableName, bool decorated = false);

    public partial nint GetFunctionByNameEx(string svFunctionName);

    public partial nint FindInterfaceEx(string svInterfaceName);

    public partial bool FindPatternMultiEx(string svPattern, CUtlLeanVectorBase<nint, int>* count);

    public partial nint FindStringEx(string str);

    public partial nint FindDataEx(byte* needle, ulong needleSize, bool readOnly);

    public partial nint FindPtrEx(nint ptr);

    public partial void FindVTablePartialEx(string str, CUtlLeanVectorBase<RuntimeVirtualTableInfo, int>* list);

    public partial bool IsPointerDerivedFromEx(nint ptr, string name);

    public partial bool GetReferencesEx(nint ptr, CUtlLeanVectorBase<nint, int>* result);

    public partial bool FindAllFunctionsFromStringsRefsEx(CUtlLeanVectorBase<CUtlString, int>* strs,
        CUtlLeanVectorBase<nint, int>*                                                         result);

    public partial bool FindAllFunctionsFromPointersRefsEx(CUtlLeanVectorBase<nint, int>* strs,
        CUtlLeanVectorBase<nint, int>*                                                    result);

    public partial bool GetFunctionRangeEx(nint middle, nint* start, nint* end);

    public partial nint FindStringExactEx(string str);

    public partial bool FindExportFunctionsEx(string name, CUtlLeanVectorBase<RuntimeExportInfo, int>* results);

    public partial void GetModuleInfoEx(RuntimeModuleInfo* info);

    public partial bool FindPtrsEx(nint ptr, CUtlLeanVectorBase<nint, int>* results);

    public partial nint FindStringWithOptionsEx(string str, bool readOnly, bool exact);

    public partial bool GetVFunctionsEx(string tableName, CUtlLeanVectorBase<nint, int>* results);

    public partial bool GetSegmentsEx(CUtlLeanVectorBase<RuntimeSegmentInfo, int>* results);

    public partial nint GetTypeInfoFromNameEx(string name);

    public partial bool GetFunctionEntriesEx(CUtlLeanVectorBase<RuntimeFunctionInfo, int>* results);

    public partial nint FindVirtualTableByNameEx(string tableName, bool decorated = false);

    #endregion

    public nint BaseAddress
    {
        get
        {
            RuntimeModuleInfo info = default;
            GetModuleInfoEx(&info);
            return info.BaseAddress;
        }
    }

    public nuint ImageSize
    {
        get
        {
            RuntimeModuleInfo info = default;
            GetModuleInfoEx(&info);
            return info.Size;
        }
    }

    public string ModuleName
    {
        get
        {
            RuntimeModuleInfo info = default;
            GetModuleInfoEx(&info);
            return NativeString.ReadString(info.Name);
        }
    }

    public nint FindPattern(string pattern, nint startAddress = 0)
    {
        if (startAddress == 0)
        {
            return FindPatternEx(pattern);
        }

        foreach (var result in FindPatternMulti(pattern))
        {
            if ((nuint) result >= (nuint) startAddress)
            {
                return result;
            }
        }

        return 0;
    }

    public nint GetVirtualTableByName(string tableName, bool decorated = false)
        => GetVTableByNameEx(tableName, decorated);

    public bool TryGetVirtualTableByName(string tableName, out nint address, bool decorated = false)
    {
        address = FindVirtualTableByNameEx(tableName, decorated);
        return address != 0;
    }

    public nint GetExportFunction(string exportName)
        => GetFunctionByNameEx(exportName);

    [Obsolete("Use GetExportFunction instead.")]
    public nint GetFunctionByName(string functionName)
        => GetExportFunction(functionName);

    public NativeFunctionInfo[] FindExportFunctions(string exportName)
    {
        using var vector = new CUtlLeanVectorBase<RuntimeExportInfo, int>();

        if (!FindExportFunctionsEx(exportName, &vector))
        {
            return [];
        }

        var results = new NativeFunctionInfo[vector.Count];

        try
        {
            for (var i = 0; i < vector.Count; i++)
            {
                ref var current = ref vector[i];
                results[i] = new NativeFunctionInfo
                {
                    RawName = current.RawName.Get(), Signature = current.Signature.Get(), Address = current.Address,
                };
            }
        }
        finally
        {
            for (var i = 0; i < vector.Count; i++)
            {
                ref var current = ref vector[i];
                current.RawName.Dispose();
                current.Signature.Dispose();
            }
        }

        return results;
    }

    public nint FindPatternExactly(string pattern)
    {
        var results = FindPatternMulti(pattern);

        return results.Count switch
        {
            0   => 0,
            > 1 => throw new InvalidOperationException("Found more than one result for the pattern."),
            _   => results[0],
        };
    }

    public nint FindInterface(string interfaceName)
        => FindInterfaceEx(interfaceName);

    public List<nint> FindPatternMulti(string pattern)
    {
        using var vector = new CUtlLeanVectorBase<nint, int>();

        if (!FindPatternMultiEx(pattern, &vector))
        {
            return [];
        }

        var results    = new List<nint>(vector.Count);
        var nativeSpan = new ReadOnlySpan<nint>(vector.Base(), vector.Count);

        results.AddRange(nativeSpan);

        return results;
    }

    public nint FindString(string str)
        => FindString(str, false, false);

    public nint FindStringExact(string str)
        => FindString(str, false, true);

    public nint FindString(string str, bool readOnly, bool exact)
        => FindStringWithOptionsEx(str, readOnly, exact);

    public nint FindPtr(nint ptr)
        => FindPtrEx(ptr);

    public nint[] FindPointers(nint ptr)
    {
        using var vector = new CUtlLeanVectorBase<nint, int>();

        if (!FindPtrsEx(ptr, &vector))
        {
            return [];
        }

        var results = new nint[vector.Count];
        vector.AsReadOnlySpan().CopyTo(results);
        return results;
    }

    public nint[] GetVirtualFunctions(string tableName)
    {
        using var vector = new CUtlLeanVectorBase<nint, int>();

        if (!GetVFunctionsEx(tableName, &vector))
        {
            return [];
        }

        var results = new nint[vector.Count];
        vector.AsReadOnlySpan().CopyTo(results);
        return results;
    }

    public VirtualTableInfo[] FindVirtualTablesPartial(string str)
    {
        using var leanVector = new CUtlLeanVectorBase<RuntimeVirtualTableInfo, int>();

        FindVTablePartialEx(str, &leanVector);
        var count = leanVector.Count;

        if (count == 0)
        {
            return [];
        }

        var results = new VirtualTableInfo[count];

        for (var i = 0; i < count; i++)
        {
            ref var current = ref leanVector[i];

            results[i] = new VirtualTableInfo
            {
                DemangledName = current.DemangledName.Get(), Address = current.Address, Offset = current.Offset,
            };

            current.DemangledName.Dispose();
        }

        return results;
    }

    public bool IsPointerDerivedFrom(nint ptr, string name)
        => IsPointerDerivedFromEx(ptr, name);

    public nint[] GetReferencesFromPointer(nint ptr)
    {
        using var resultVector = new CUtlLeanVectorBase<nint, int>();

        if (!GetReferencesEx(ptr, &resultVector))
        {
            return [];
        }

        var result = new nint[resultVector.Count];
        new Span<nint>(resultVector.Base(), resultVector.Count).CopyTo(result);

        return result;
    }

    public nint FindFunction(string str)
        => FindFunction(new ReadOnlySpan<string>(ref str));

    public nint FindFunction(ReadOnlySpan<string> strs)
    {
        using var strVector = new CUtlLeanVectorBase<CUtlString, int>();
        strVector.EnsureCapacity(strs.Length);

        try
        {
            foreach (var str in strs)
            {
                strVector.AddToTail(new CUtlString(str));
            }

            using var resultVector = new CUtlLeanVectorBase<nint, int>();

            if (!FindAllFunctionsFromStringsRefsEx(&strVector, &resultVector) || resultVector.Count == 0)
            {
                throw new InvalidOperationException("No function entries found.");
            }

            if (resultVector.Count != 1)
            {
                throw new InvalidOperationException($"Found {resultVector.Count} matching function entries.");
            }

            return *resultVector.Base();
        }
        finally
        {
            var vectorSpan = new Span<CUtlString>(strVector.Base(), strVector.Count);

            foreach (ref var utlString in vectorSpan)
            {
                utlString.Dispose();
            }
        }
    }

    public nint[] FindFunctions(string str)
        => FindFunctions(new ReadOnlySpan<string>(ref str));

    public nint[] FindFunctions(ReadOnlySpan<string> strs)
    {
        using var strVector = new CUtlLeanVectorBase<CUtlString, int>();
        strVector.EnsureCapacity(strs.Length);

        try
        {
            foreach (var str in strs)
            {
                strVector.AddToTail(new CUtlString(str));
            }

            using var resultVector = new CUtlLeanVectorBase<nint, int>();

            if (!FindAllFunctionsFromStringsRefsEx(&strVector, &resultVector))
            {
                var preview = strs.Length > 0 ? strs[0] : "empty";

                if (strs.Length > 1)
                {
                    preview += ", ...";
                }

                throw new Exception($"No function entries found for {strs.Length} strings. (First: {preview})");
            }

            var result = new nint[resultVector.Count];
            new Span<nint>(resultVector.Base(), resultVector.Count).CopyTo(result);

            return result;
        }
        finally
        {
            var vectorSpan = new Span<CUtlString>(strVector.Base(), strVector.Count);

            foreach (ref var utlString in vectorSpan)
            {
                utlString.Dispose();
            }
        }
    }

    public nint FindFunction(nint ptr)
    {
        Span<nint> ptrSpan = [ptr];

        return FindFunction(ptrSpan);
    }

    public nint FindFunction(ReadOnlySpan<nint> ptrs)
    {
        if (ptrs.Length == 0)
        {
            throw new InvalidOperationException("No pointer was passed in");
        }

        using var ptrVector = new CUtlLeanVectorBase<nint, int>();
        ptrVector.EnsureCapacity(ptrs.Length);

        foreach (var t in ptrs)
        {
            ptrVector.AddToTail(t);
        }

        using var resultVector = new CUtlLeanVectorBase<nint, int>();

        if (!FindAllFunctionsFromPointersRefsEx(&ptrVector, &resultVector) || resultVector.Count == 0)
        {
            throw new InvalidOperationException("No result was found for pointer.");
        }

        if (resultVector.Count != 1)
        {
            throw new InvalidOperationException($"Found {resultVector.Count} matching function entries.");
        }

        return *resultVector.Base();
    }

    public nint[] FindFunctions(nint ptr)
    {
        Span<nint> ptrSpan = [ptr];

        return FindFunctions(ptrSpan);
    }

    public nint[] FindFunctions(ReadOnlySpan<nint> ptrs)
    {
        if (ptrs.Length == 0)
        {
            throw new InvalidOperationException("No pointer was passed in");
        }

        using var ptrVector = new CUtlLeanVectorBase<nint, int>();
        ptrVector.EnsureCapacity(ptrs.Length);

        foreach (var t in ptrs)
        {
            ptrVector.AddToTail(t);
        }

        using var resultVector = new CUtlLeanVectorBase<nint, int>();

        if (!FindAllFunctionsFromPointersRefsEx(&ptrVector, &resultVector))
        {
            var firstPtr = ptrs.IsEmpty ? "empty" : $"0x{ptrs[0]:X}...";

            throw new Exception($"No result was found for pointers ({ptrs.Length} items, starting with {firstPtr})");
        }

        var result = new nint[resultVector.Count];
        new Span<nint>(resultVector.Base(), resultVector.Count).CopyTo(result);

        return result;
    }

    public nint FindData(ReadOnlySpan<byte> data, bool readOnly)
    {
        unchecked
        {
            fixed (byte* ptr = data)
            {
                return FindDataEx(ptr, (ulong) data.Length, readOnly);
            }
        }
    }

    public bool GetFunctionRange(nint middle, out nint start, out nint end)
    {
        fixed (nint* pStart = &start, pEnd = &end)
        {
            return GetFunctionRangeEx(middle, pStart, pEnd);
        }
    }

    public ModuleSegmentInfo[] GetSegments()
    {
        using var vector = new CUtlLeanVectorBase<RuntimeSegmentInfo, int>();

        if (!GetSegmentsEx(&vector))
        {
            return [];
        }

        var results = new ModuleSegmentInfo[vector.Count];
        for (var i = 0; i < vector.Count; ++i)
        {
            ref var segment = ref vector[i];
            results[i] = new ModuleSegmentInfo
            {
                Address = segment.Address, Size = segment.Size, Access = segment.Access,
            };
        }

        return results;
    }

    public bool TryGetTypeInfo(string typeName, out nint typeInfo)
    {
        typeInfo = GetTypeInfoFromNameEx(typeName);
        return typeInfo != 0;
    }

    public NativeFunctionRange[] GetKnownFunctionRanges()
    {
        using var vector = new CUtlLeanVectorBase<RuntimeFunctionInfo, int>();

        if (!GetFunctionEntriesEx(&vector))
        {
            return [];
        }

        var results = new NativeFunctionRange[vector.Count];
        for (var i = 0; i < vector.Count; ++i)
        {
            ref var function = ref vector[i];
            results[i] = new NativeFunctionRange { Start = function.Start, End = function.End };
        }

        return results;
    }
}
