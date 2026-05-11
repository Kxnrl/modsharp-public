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
using Sharp.Shared.Objects;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Objects;

internal partial class GlobalVars : NativeObject, IGlobalVars
{
    public const int RealTimeOffset                     = 0x00;
    public const int FrameCountOffset                   = 0x04;
    public const int AbsoluteFrameTimeOffset            = 0x08;
    public const int AbsoluteFrameStartTimeStdDevOffset = 0x0C;
    public const int MaxClientsOffset                   = 0x10;
    public const int CurTimeOffset                      = 0x30;
    public const int FrameTimeOffset                    = 0x34;
    public const int RenderTimeOffset                   = 0x38;
    public const int InSimulationOffset                 = 0x40;
    public const int TickCountOffset                    = 0x44;
    public const int SubTickFractionOffset              = 0x50;
    public const int MapNameOffset                      = 0x60;
    public const int MaxEntitiesOffset                  = 0x78;

    public float RealTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(RealTimeOffset);
        }
    }

    public int FrameCount
    {
        get
        {
            CheckDisposed();

            return _this.GetInt32(FrameCountOffset);
        }
    }

    public float AbsoluteFrameTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(AbsoluteFrameTimeOffset);
        }
    }

    public float AbsoluteFrameStartTimeStdDev
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(AbsoluteFrameStartTimeStdDevOffset);
        }
    }

    public int MaxClients
    {
        get
        {
            CheckDisposed();

            return _this.GetInt32(MaxClientsOffset);
        }
    }

    public float FrameTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(FrameTimeOffset);
        }
    }

    public float CurTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(CurTimeOffset);
        }
    }

    public float RenderTime
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(RenderTimeOffset);
        }
    }

    public bool InSimulation
    {
        get
        {
            CheckDisposed();

            return _this.GetBool(InSimulationOffset);
        }
    }

    public int TickCount
    {
        get
        {
            CheckDisposed();

            return _this.GetInt32(TickCountOffset);
        }
    }

    public float SubTickFraction
    {
        get
        {
            CheckDisposed();

            return _this.GetFloat(SubTickFractionOffset);
        }
    }

    public string MapName
    {
        get
        {
            CheckDisposed();

            return _this.GetPtrString(MapNameOffset);
        }
    }

    public int MaxEntities
    {
        get
        {
            CheckDisposed();

            return _this.GetInt32(MaxEntitiesOffset);
        }
    }
}
