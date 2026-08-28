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
using System.Runtime.CompilerServices;
using Sharp.Shared.CStrike;

namespace Sharp.Core.CStrike;

internal abstract class ContextObject : IContextObject
{
    public bool IsDisposed { get; private set; }

    internal void MarkAsDisposed()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        OnDisposed();
    }

    protected virtual void OnDisposed()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }
}

internal abstract class NativeObject : ContextObject, INativeObject
{
    // ReSharper disable once InconsistentNaming
    protected nint _this { get; init; }

    protected NativeObject(nint @this)
        => _this = @this;

    public static NativeObject? Create(nint ptr)
        => throw new NotSupportedException("You could not create this!");

    public override int GetHashCode()
        => _this.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is NativeObject cs)
        {
            return GetAbsPtr() == cs.GetAbsPtr();
        }

        return false;
    }

    public nint GetAbsPtr()
        => _this;

    public bool Equals(INativeObject? other)
    {
        if (other is null)
        {
            return false;
        }

        return _this == other.GetAbsPtr();
    }
}
