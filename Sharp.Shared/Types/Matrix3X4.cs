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
using System.Runtime.CompilerServices;

namespace Sharp.Shared.Types;

// ReSharper disable once InconsistentNaming
[InlineArray(3)]
public struct Matrix3x4
{
    [InlineArray(4)]
    public struct Row
    {
        private float _element0;
    }

    private Row _element0;

    public Vector GetOrigin()
        => new (this[0][3], this[1][3], this[2][3]);

    public unsafe Vector GetAngles()
    {
        var angles = new Vector();

        var forward = stackalloc float[3];
        var left    = stackalloc float[3];
        var up      = stackalloc float[3];

        //
        // Extract the basis vectors from the matrix. Since we only need the Z
        // component of the up vector, we don't get X and Y.
        //
        forward[0] = this[0][0];
        forward[1] = this[1][0];
        forward[2] = this[2][0];
        left[0]    = this[0][1];
        left[1]    = this[1][1];
        left[2]    = this[2][1];
        up[2]      = this[2][2];

        var xyDist = MathF.Sqrt((forward[0] * forward[0]) + (forward[1] * forward[1]));

        // enough here to get angles?
        if (xyDist > 0.001f)
        {
            // (yaw)	y = ATAN( forward.y, forward.x );		-- in our space, forward is the X axis
            angles[1] = Rad2Deg(MathF.Atan2(forward[1], forward[0]));

            // (pitch)	x = ATAN( -forward.z, sqrt(forward.x*forward.x+forward.y*forward.y) );
            angles[0] = Rad2Deg(MathF.Atan2(-forward[2], xyDist));

            // (roll)	z = ATAN( left.z, up.z );
            angles[2] = Rad2Deg(MathF.Atan2(left[2], up[2]));
        }
        else // forward is mostly Z, gimbal lock-
        {
            // (yaw)	y = ATAN( -left.x, left.y );			-- forward is mostly z, so use right for yaw
            angles[1] = Rad2Deg(MathF.Atan2(-left[0], left[1]));

            // (pitch)	x = ATAN( -forward.z, sqrt(forward.x*forward.x+forward.y*forward.y) );
            angles[0] = Rad2Deg(MathF.Atan2(-forward[2], xyDist));

            // Assume no roll in this case as one degree of freedom has been lost (i.e. yaw == roll)
            angles[2] = 0;
        }

        return angles;
    }

    public static float Rad2Deg(float x)
        => x * (180f / MathF.PI);
}
