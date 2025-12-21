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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Sequential)]
public struct Vector : IComparable, IComparable<Vector>, IEquatable<Vector>
{
    /// <summary>
    ///     X or PITCH (Up / Down)
    /// </summary>
    public float X { get; set; }

    /// <summary>
    ///     Y or YAW (Left / Right)
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    ///     Z or ROLL (Fall over)
    /// </summary>
    public float Z { get; set; }

    public Vector(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector(Vector other)
    {
        X = other.X;
        Y = other.Y;
        Z = other.Z;
    }

    public override bool Equals(object? obj)
    {
        if (obj is Vector h)
        {
            return this == h;
        }

        return false;
    }

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z);

    public static bool operator ==(Vector a, Vector b)
        => MathF.Abs(a.X - b.X) < 0.01f && MathF.Abs(a.Y - b.Y) < 0.01f && MathF.Abs(a.Z - b.Z) < 0.01f;

    public static bool operator !=(Vector a, Vector b)
        => !(a == b);

    public Vector()
    {
        X = 0.0f;
        Y = 0.0f;
        Z = 0.0f;
    }

    public float this[int key]
    {
        get
            => key switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                _ => throw new ArgumentOutOfRangeException(nameof(key), key, null),
            };
        set
        {
            switch (key)
            {
                case 0:
                    X = value;

                    break;
                case 1:
                    Y = value;

                    break;
                case 2:
                    Z = value;

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, null);
            }
        }
    }

    public static Vector operator +(Vector a, Vector b)
        => new (a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector operator -(Vector a, Vector b)
        => new (a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector operator *(Vector vec, float fl)
        => new (vec.X * fl, vec.Y * fl, vec.Z * fl);

    public static Vector operator *(Vector a, Vector b)
        => new (a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    public static Vector operator +(Vector vec, float fl)
        => new (vec.X + fl, vec.Y + fl, vec.Z + fl);

    public static Vector operator -(Vector vec, float fl)
        => new (vec.X - fl, vec.Y - fl, vec.Z - fl);

    public static Vector operator /(Vector vec, float fl)
    {
        Debug.Assert(fl != 0.0f);
        var vf = 1.0f / fl;

        return new Vector(vec.X * vf, vec.Y * vf, vec.Z * vf);
    }

    public static Vector operator /(Vector a, Vector b)
    {
        Debug.Assert(b.X != 0.0f && b.Y != 0.0f && b.Z != 0.0f);

        return new Vector(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    }

    public Vector AnglesToVectorForward()
    {
        SinCos(Y, out var sy, out var cy);
        SinCos(X, out var sp, out var cp);

        return new Vector(cp * cy, cp * sy, -sp);
    }

    public void AnglesToVector(out Vector forward, out Vector right, out Vector up)
    {
        SinCos(Y, out var sy, out var cy);
        SinCos(X, out var sp, out var cp);
        SinCos(Z, out var sr, out var cr);

        forward = new Vector(cp * cy,                                cp * sy,                               -sp);
        right   = new Vector((-1 * sr * sp * cy) + (-1  * cr * -sy), (-1 * sr * sp * sy) + (-1  * cr * cy), -1 * sr * cp);
        up      = new Vector((cr * sp * cy)      + (-sr * -sy),      (cr * sp * sy)      + (-sr * cy),      cr * cp);
    }

    public void AnglesToVectorSource2(out Vector forward, out Vector right, out Vector up)
    {
        SinCos(Y, out var sy, out var cy);
        SinCos(X, out var sp, out var cp);
        SinCos(Z, out var sr, out var cr);

        forward = new Vector(cp * cy,                      cp * sy,                     -sp);
        right   = new Vector((sr * sp * cy) + (cr  * -sy), (sr * sp * sy) + (cr  * cy), sr * cp);
        up      = new Vector((cr * sp * cy) + (-sr * -sy), (cr * sp * sy) + (-sr * cy), cr * cp);
    }

    public bool IsValid()
        => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);

    public bool IsZero(float tolerance = 0.01f)
        => X > -tolerance && X < tolerance && Y > -tolerance && Y < tolerance && Z > -tolerance && Z < tolerance;

    public float Dot(Vector other)
        => DotProduct(this, other);

    public float Length()
        => MathF.Sqrt((X * X) + (Y * Y) + (Z * Z));

    public float LengthSqr()
        => (X * X) + (Y * Y) + (Z * Z);

    public float Length2D()
        => MathF.Sqrt((X * X) + (Y * Y));

    public float Length2DSqr()
        => (X * X) + (Y * Y);

    /// <summary>
    ///     Calculates the squared distance to another vector.
    /// </summary>
    public float DistToSqr(Vector other)
    {
        var delta = new Vector(X - other.X, Y - other.Y, Z - other.Z);

        return delta.LengthSqr();
    }

    public float DistTo(Vector other)
        => MathF.Sqrt(DistToSqr(other));

    /// <summary>
    ///     Normalizes the vector (makes it a unit vector with length 1).
    /// </summary>
    public void Normalize()
    {
        var length = MathF.Sqrt((X * X) + (Y * Y) + (Z * Z));

        X /= length;
        Y /= length;
        Z /= length;
    }

    /// <summary>
    ///     Negates the vector (inverts its direction).
    /// </summary>
    public void Negate()
    {
        X = -X;
        Y = -Y;
        Z = -Z;
    }

    public Vector Cross(Vector rhs)
        => new ((Y * rhs.Z) - (Z * rhs.Y), (Z * rhs.X) - (X * rhs.Z), (X * rhs.Y) - (Y * rhs.X));

    public Vector2D AsVector2D()
        => new (X, Y);

    private static void SinCos(float value, out float sin, out float cos)
    {
        var cv = value * (MathF.PI / 180.0f);
        sin = MathF.Sin(cv);
        cos = MathF.Cos(cv);
    }

    public override string ToString()
        => $"{{{X:F6}, {Y:F6}, {Z:F6}}}";

    // Since Vector is also used for velocity, if IComparable is actually used, 
    // we compare the lengths (magnitudes). 
    // In any other scenario, comparing raw coordinates is meaningless.
    public int CompareTo(object? obj)
    {
        if (obj is not Vector rhs)
        {
            throw new InvalidOperationException("?");
        }

        return (int) (LengthSqr() - rhs.LengthSqr());
    }

    // Since Vector is also used for velocity, if IComparable is actually used, 
    // we compare the lengths (magnitudes). 
    // In any other scenario, comparing raw coordinates is meaningless.
    public int CompareTo(Vector rhs)
        => (int) (LengthSqr() - rhs.LengthSqr());

    /// <summary>
    ///     Calculates the Dot Product.
    /// </summary>
    public static float DotProduct(in Vector a, in Vector b)
        => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    /// <summary>
    ///     Calculates the Magnitude (Length).
    /// </summary>
    public static float Magnitude(in Vector v)
        => MathF.Sqrt((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z));

    /// <summary>
    ///     Converts a direction vector into Euler angles.
    /// </summary>
    public Vector DirectionToAngles()
    {
        var y = MathF.Atan2(Y,  X)                             * (180f / MathF.PI);
        var x = MathF.Atan2(-Z, MathF.Sqrt((X * X) + (Y * Y))) * (180f / MathF.PI);

        return new Vector(x, y, 0.0f);
    }

    /// <summary>
    ///     Calculates the Euler angles required to look at a specific position.
    /// </summary>
    /// <param name="position">The target position to look at.</param>
    public Vector AnglesTo(Vector position)
    {
        var dir = position - this;

        return dir.DirectionToAngles();
    }

    public Vector VectorToAngles()
    {
        float yaw,
              pitch;

        if (this[1] == 0 && this[0] == 0)
        {
            yaw = 0;

            pitch = this[2] > 0 ? 270 : 90;
        }
        else
        {
            yaw = (float) ((Math.Atan2(this[1], this[0]) * 180) / Math.PI);

            if (yaw < 0)
            {
                yaw += 360;
            }

            var tmp = (float) Math.Sqrt((this[0] * this[0]) + (this[1] * this[1]));
            pitch = (float) ((Math.Atan2(-this[2], tmp) * 180) / Math.PI);

            if (pitch < 0)
            {
                pitch += 360;
            }
        }

        return new Vector(pitch, yaw, 0);
    }

    public bool Equals(Vector other)
        => MathF.Abs(X    - other.X) < float.Epsilon
           && MathF.Abs(Y - other.Y) < float.Epsilon
           && MathF.Abs(Z - other.Z) < float.Epsilon;
}

[StructLayout(LayoutKind.Sequential)]
public struct Vector2D : IComparable, IComparable<Vector2D>, IEquatable<Vector2D>
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2D(float x, float y)
    {
        X = x;
        Y = y;
    }

    public Vector2D(Vector other)
    {
        X = other.X;
        Y = other.Y;
    }

    public bool IsValid()
        => float.IsFinite(X) && float.IsFinite(Y);

    public float LengthSqr()
        => (X * X) + (Y * Y);

    public float Length()
        => MathF.Sqrt(LengthSqr());

    public float Dot(Vector2D other)
        => (X * other.X) + (Y * other.Y);

    // Since Vector is also used for velocity, if IComparable is actually used, 
    // we compare the lengths (magnitudes). 
    // In any other scenario, comparing raw coordinates is meaningless.
    public int CompareTo(object? obj)
    {
        if (obj is not Vector2D rhs)
        {
            throw new InvalidOperationException("?");
        }

        return (int) (LengthSqr() - rhs.LengthSqr());
    }

    // Since Vector is also used for velocity, if IComparable is actually used, 
    // we compare the lengths (magnitudes). 
    // In any other scenario, comparing raw coordinates is meaningless.
    public int CompareTo(Vector2D rhs)
        => (int) (LengthSqr() - rhs.LengthSqr());

    public static bool operator ==(Vector2D a, Vector2D b)
        => MathF.Abs(a.X - b.X) < 0.01f && MathF.Abs(a.Y - b.Y) < 0.01f;

    public static bool operator !=(Vector2D a, Vector2D b)
        => !(a == b);

    public override int GetHashCode()
        => HashCode.Combine(X, Y);

    public override bool Equals(object? obj)
    {
        if (obj is Vector2D h)
        {
            return this == h;
        }

        return false;
    }

    public bool Equals(Vector2D other)
        => MathF.Abs(X    - other.X) < float.Epsilon
           && MathF.Abs(Y - other.Y) < float.Epsilon;
}

[StructLayout(LayoutKind.Sequential)]
public struct Vector4D : IEquatable<Vector4D>
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }

    public Vector4D(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Vector4D(Vector4D other)
    {
        X = other.X;
        Y = other.Y;
        Z = other.Z;
        W = other.W;
    }

    public bool IsValid()
        => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z) && float.IsFinite(W);

    public float LengthSqr()
        => (X * X) + (Y * Y) + (Z * Z) + (W * W);

    public float Length()
        => MathF.Sqrt(LengthSqr());

    public float Dot(Vector4D other)
        => (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);

    public static bool operator ==(Vector4D a, Vector4D b)
        => MathF.Abs(a.X    - b.X) < 0.01f
           && MathF.Abs(a.Y - b.Y) < 0.01f
           && MathF.Abs(a.Z - b.Z) < 0.01f
           && MathF.Abs(a.W - b.W) < 0.01f;

    public static bool operator !=(Vector4D a, Vector4D b)
        => !(a == b);

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z, W);

    public override bool Equals(object? obj)
    {
        if (obj is Vector4D h)
        {
            return this == h;
        }

        return false;
    }

    public bool Equals(Vector4D other)
        => MathF.Abs(X    - other.X) < float.Epsilon
           && MathF.Abs(Y - other.Y) < float.Epsilon
           && MathF.Abs(Z - other.Z) < float.Epsilon
           && MathF.Abs(W - other.W) < float.Epsilon;
}
