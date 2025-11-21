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

#ifndef MS_ROOT_MATH_H
#define MS_ROOT_MATH_H

#include <cmath>

inline constexpr float  FLOAT_EPSILON  = 0.0001f;
inline constexpr double DOUBLE_EPSILON = 0.0001;

inline bool FloatEquals(float l, float r)
{
    return std::abs(l - r) < FLOAT_EPSILON;
}

inline bool DoubleEquals(double l, double r)
{
    return std::abs(l - r) < DOUBLE_EPSILON;
}

#endif