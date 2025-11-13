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

#ifndef CSTRIKE_TYPE_MATRIX3X4_H
#define CSTRIKE_TYPE_MATRIX3X4_H

#include "cstrike/type/Quaternion.h"
#include "cstrike/type/Vector.h"

#include <cstring>

enum MatrixAxisType_t
{
    FORWARD_AXIS = 0,
    LEFT_AXIS    = 1,
    UP_AXIS      = 2,

    X_AXIS = 0,
    Y_AXIS = 1,
    Z_AXIS = 2,

    ORIGIN     = 3,
    PROJECTIVE = 3,
};

struct matrix3x4_t
{
    matrix3x4_t() = default;
    matrix3x4_t(
        float m00, float m01, float m02, float m03,
        float m10, float m11, float m12, float m13,
        float m20, float m21, float m22, float m23)
    {
        m_flMatVal[0][0] = m00;
        m_flMatVal[0][1] = m01;
        m_flMatVal[0][2] = m02;
        m_flMatVal[0][3] = m03;
        m_flMatVal[1][0] = m10;
        m_flMatVal[1][1] = m11;
        m_flMatVal[1][2] = m12;
        m_flMatVal[1][3] = m13;
        m_flMatVal[2][0] = m20;
        m_flMatVal[2][1] = m21;
        m_flMatVal[2][2] = m22;
        m_flMatVal[2][3] = m23;
    }

    inline void Init(const Vector& xAxis, const Vector& yAxis, const Vector& zAxis, const Vector& vecOrigin);

    //-----------------------------------------------------------------------------
    // Creates a matrix where the X axis = forward
    // the Y axis = left, and the Z axis = up
    //-----------------------------------------------------------------------------
    matrix3x4_t(const Vector& xAxis, const Vector& yAxis, const Vector& zAxis, const Vector& vecOrigin)
    {
        Init(xAxis, yAxis, zAxis, vecOrigin);
    }

    matrix3x4_t& operator=(const matrix3x4_t& src)
    {
        if (this == &src)
            return *this;

        memcpy(this->Base(), src.Base(), sizeof(float) * 3 * 4);
        return *this;
    }

    inline Vector GetColumn(MatrixAxisType_t column) const;
    inline void   SetColumn(const Vector& in, MatrixAxisType_t column);

    inline void   SetOrigin(const Vector& pos) { SetColumn(pos, ORIGIN); }
    inline Vector GetOrigin() const { return GetColumn(ORIGIN); }

    inline Vector GetForward() const { return GetColumn(FORWARD_AXIS); }
    inline Vector GetLeft() const { return GetColumn(LEFT_AXIS); }
    inline Vector GetRight() const { return -GetColumn(LEFT_AXIS); }
    inline Vector GetUp() const { return GetColumn(UP_AXIS); }

    inline void Invalidate(void)
    {
        for (auto& i : m_flMatVal)
        {
            for (float& j : i)
            {
                j = FLT_MIN;
            }
        }
    }

    float* operator[](int i)
    {
        return m_flMatVal[i];
    }
    const float* operator[](int i) const
    {
        return m_flMatVal[i];
    }

    float*       Base() { return &m_flMatVal[0][0]; }
    const float* Base() const { return &m_flMatVal[0][0]; }

    float m_flMatVal[3][4]{};
};

inline void matrix3x4_t::Init(const Vector& xAxis, const Vector& yAxis, const Vector& zAxis, const Vector& vecOrigin)
{
    SetColumn(xAxis, MatrixAxisType_t::X_AXIS);
    SetColumn(yAxis, MatrixAxisType_t::Y_AXIS);
    SetColumn(zAxis, MatrixAxisType_t::Z_AXIS);
    SetColumn(vecOrigin, MatrixAxisType_t::ORIGIN);
}

inline Vector matrix3x4_t::GetColumn(MatrixAxisType_t column) const
{
    return {m_flMatVal[0][column], m_flMatVal[1][column], m_flMatVal[2][column]};
}

inline void matrix3x4_t::SetColumn(const Vector& in, MatrixAxisType_t column)
{
    m_flMatVal[0][column] = in.x;
    m_flMatVal[1][column] = in.y;
    m_flMatVal[2][column] = in.z;
}

struct MS_ALIGN16 matrix3x4a_t : public matrix3x4_t
{
public:
    /*
    matrix3x4a_t() { if (((size_t)Base()) % 16 != 0) { Error( "matrix3x4a_t missaligned" ); } }
    */
    matrix3x4a_t& operator=(const matrix3x4_t& src)
    {
        memcpy(Base(), src.Base(), sizeof(float) * 3 * 4);
        return *this;
    };

    matrix3x4a_t& operator=(const matrix3x4a_t& src)
    {
        if (this == &src)
            return *this;

        memcpy(Base(), src.Base(), sizeof(float) * 3 * 4);
        return *this;
    }
} MS_ALIGN16_POST;

#endif
