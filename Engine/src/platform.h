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

#ifndef MS_ROOT_PLATFORM_H
#define MS_ROOT_PLATFORM_H

#ifdef PLATFORM_WINDOWS

#    define MS_DECL_ALIGN(x) __declspec(align(x))
#    define MS_ALIGN4 MS_DECL_ALIGN(4)
#    define MS_ALIGN8 MS_DECL_ALIGN(8)
#    define MS_ALIGN16 MS_DECL_ALIGN(16)
#    define MS_ALIGN32 MS_DECL_ALIGN(32)
#    define MS_ALIGN128 MS_DECL_ALIGN(128)

#    define MS_ALIGN4_POST
#    define MS_ALIGN8_POST
#    define MS_ALIGN16_POST
#    define MS_ALIGN32_POST
#    define MS_ALIGN128_POST

#    define MS_IMPORT extern "C" __declspec(dllimport)
#    define MS_CLASS_IMPORT __declspec(dllimport)
#    define MS_GLOBAL_IMPORT extern __declspec(dllimport)

#    define MS_EXPORT_C_API extern "C" __declspec(dllexport)

#    define MS_FMTFUNCTION(a, b)

#    define MS_FORCEINLINE __forceinline

#    define MS_NOINLINE __declspec(noinline)

#else

#    define MS_DECL_ALIGN(x) __attribute__((aligned(x)))

#    define MS_ALIGN4
#    define MS_ALIGN8
#    define MS_ALIGN16
#    define MS_ALIGN32
#    define MS_ALIGN128

#    define MS_ALIGN4_POST MS_DECL_ALIGN(4)
#    define MS_ALIGN8_POST MS_DECL_ALIGN(8)
#    define MS_ALIGN16_POST MS_DECL_ALIGN(16)
#    define MS_ALIGN32_POST MS_DECL_ALIGN(32)
#    define MS_ALIGN128_POST MS_DECL_ALIGN(128)

#    define MS_IMPORT extern "C"
#    define MS_CLASS_IMPORT
#    define MS_GLOBAL_IMPORT extern

#    define MS_EXPORT_C_API extern "C" __attribute__((visibility("default")))

#    define MS_FMTFUNCTION(fmtargnumber, firstvarargnumber) __attribute__((format(printf, fmtargnumber, firstvarargnumber)))

#    define MS_FORCEINLINE inline __attribute__((always_inline))

#    define MS_NOINLINE __attribute__((noinline))

#endif

#endif