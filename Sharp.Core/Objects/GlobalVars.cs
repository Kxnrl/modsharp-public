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

using Sharp.Core.CStrike;
using Sharp.Shared.Objects;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Objects;

internal partial class GlobalVars : NativeObject, IGlobalVars
{
    public float RealTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(0);
        }
    }

    public int FrameCount
    {
        get
        {
            CheckDisposed();

            return _this.GetInt32(4);
        }
    }

    public float AbsoluteFrameTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(8);
        }
    }

    public float AbsoluteFrameStartTimeStdDev
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(12);
        }
    }

    public int MaxClients
    {
        get
        {
            CheckDisposed();

            return _this.GetInt32(16);
        }
    }

    public float FrameTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(0x34);
        }
    }

    public float CurTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(0x30);
        }
    }

    public float RenderTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(56);
        }
    }

    public bool InSimulation
    {
        get
        {
            CheckDisposed();

            return _this.GetBool(68);
        }
    }

    public int TickCount
    {
        get
        {
            CheckDisposed();

            return _this.GetInt32(0x44);
        }
    }

    public float SubTickFraction
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(0x50);
        }
    }

    public string MapName
    {
        get
        {
            CheckDisposed();

            return _this.GetPtrString(0x60);
        }
    }

    public int MaxEntities
    {
        get
        {
            CheckDisposed();

            return _this.GetInt32(0x78);
        }
    }
}
