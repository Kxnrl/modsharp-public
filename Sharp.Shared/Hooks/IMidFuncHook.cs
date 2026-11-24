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

namespace Sharp.Shared.Hooks;

[StructLayout(LayoutKind.Explicit, Size = 16, Pack = 8)]
public unsafe struct Xmm
{
    [FieldOffset(0)]
    public fixed byte U8[16];

    [FieldOffset(0)]
    public fixed ushort U16[8];

    [FieldOffset(0)]
    public fixed uint U32[4];

    [FieldOffset(0)]
    public fixed ulong U64[2];

    [FieldOffset(0)]
    public fixed float F32[4];

    [FieldOffset(0)]
    public fixed double F64[2];
}

/// <summary>
///     Context structure for 64-bit MidHook.
/// </summary>
/// <remarks>
///     <para>
///         This structure is used to pass the context of the hooked function to the destination, allowing full access
///         to the 64-bit registers at the moment the hook is called.
///     </para>
///     <para>
///         <b>Note:</b> <see cref="rip" /> will point to a trampoline containing the replaced instruction(s).
///     </para>
///     <para>
///         <b>Note:</b> <see cref="rsp" /> is read-only. Modifying it will have no effect.
///         Use <see cref="trampoline_rsp" /> to modify <c>rsp</c> if needed, but make sure the top of the stack is the
///         <c>rip</c> you want to resume at.
///     </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 408, Pack = 8)]
public struct MidHookContext
{
    public Xmm xmm0;
    public Xmm xmm1;
    public Xmm xmm2;
    public Xmm xmm3;
    public Xmm xmm4;
    public Xmm xmm5;
    public Xmm xmm6;
    public Xmm xmm7;
    public Xmm xmm8;
    public Xmm xmm9;
    public Xmm xmm10;
    public Xmm xmm11;
    public Xmm xmm12;
    public Xmm xmm13;
    public Xmm xmm14;
    public Xmm xmm15;

    public nint rflags;
    public nint r15;
    public nint r14;
    public nint r13;
    public nint r12;
    public nint r11;
    public nint r10;
    public nint r9;
    public nint r8;
    public nint rdi;
    public nint rsi;
    public nint rdx;
    public nint rcx;
    public nint rbx;
    public nint rax;
    public nint rbp;
    public nint rsp;
    public nint trampoline_rsp;
    public nint rip;
}

/// <summary>
///     Mid-function native hook.
/// </summary>
/// <remarks>
///     Accessing the inherited <see cref="IRuntimeNativeHook.Trampoline" /> property is not supported for mid-function hooks
///     and always throws <see cref="NotSupportedException" />.
/// </remarks>
public interface IMidFuncHook : IRuntimeNativeHook, IDisposable
{
    /// <summary>
    ///     Prepare hook.
    /// </summary>
    /// <param name="gamedata">gamedata key</param>
    /// <param name="hookFn">UnmanagedCallersOnly static function taking <c>MidHookContext*</c></param>
    /// <exception cref="EntryPointNotFoundException">Thrown when GameData is not found / null pointer / invalid field</exception>
    /// <exception cref="KeyNotFoundException">Thrown when failing to find gamedata</exception>
    /// <exception cref="InvalidOperationException">The hook is already installed</exception>
    /// <exception cref="ObjectDisposedException">The hook has been disposed</exception>
    void Prepare(string gamedata, nint hookFn);

    /// <summary>
    ///     Prepare hook.
    /// </summary>
    /// <remarks>
    ///     Convert the unmanaged function pointer manually to <see cref="nint" />.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         (nint) (delegate* unmanaged&lt;MidHookContext*, void&gt;) &amp;Test
    ///  </code>
    /// </example>
    /// <param name="pTargetFn">Native function address</param>
    /// <param name="hookFn">UnmanagedCallersOnly static function taking <c>MidHookContext*</c></param>
    /// <exception cref="InvalidOperationException">The hook is already installed</exception>
    /// <exception cref="ObjectDisposedException">The hook has been disposed</exception>
    void Prepare(nint pTargetFn, nint hookFn);
}
