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

#ifndef MS_ROOT_SCOPE_TIMER_H
#define MS_ROOT_SCOPE_TIMER_H

#ifdef _DEBUG
#include <chrono>
#include <cstdio>
#include <string>

class ScopedTimer
{
    std::string                                                 _name;
    std::chrono::time_point<std::chrono::high_resolution_clock> _start;

public:
    ScopedTimer() = delete;

    explicit ScopedTimer(std::string name) :
        _name(std::move(name)), _start(std::chrono::high_resolution_clock::now())
    {
    }

    ~ScopedTimer()
    {
        auto end      = std::chrono::high_resolution_clock::now();
        auto duration = std::chrono::duration<double, std::milli>(end - _start).count();

        printf("[TIMER] %s took %f ms\n", _name.c_str(), duration);
    }
};

#else
#include <string>

class ScopedTimer
{
public:
    explicit ScopedTimer(const char*)
    {
    }

    explicit ScopedTimer(const std::string&)
    {
    }
};

#endif

#endif