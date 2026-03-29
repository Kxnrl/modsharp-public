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

#ifndef MS_ROOT_TYPES_H
#define MS_ROOT_TYPES_H

#ifdef PLATFORM_WINDOWS
#    ifndef WIN32_LEAN_AND_MEAN
#        define WIN32_LEAN_AND_MEAN
#    endif
#    include <windows.h>
#elif defined PLATFORM_POSIX
#    include <dlfcn.h>
#else
#    error "Unsupported platform"
#endif

#ifdef PLATFORM_WINDOWS

using LibModule = HMODULE;
#    define LIB_FILE_PREFIX ""
#    define LIB_FILE_EXTENSION ".dll"

#    define MS_EXPORT extern "C" __declspec(dllexport)

#else

using LibModule = void*;
#    define LIB_FILE_PREFIX "lib"
#    define LIB_FILE_EXTENSION ".so"

#    define MS_EXPORT extern "C" __attribute__((visibility("default")))

#endif

#endif