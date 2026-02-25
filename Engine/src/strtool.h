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

#ifndef MS_ROOT_STRTOOL_H
#define MS_ROOT_STRTOOL_H

#include "platform.h"

#include <array>
#include <string>
#include <vector>

template <size_t N>
struct FixedString
{
    std::array<char, N> data;

    constexpr FixedString(std::string_view view) :
        data{}
    {
        for (size_t i = 0; i < view.size(); ++i) data[i] = view[i];
    }

    constexpr const char* c_str() const { return data.data(); }
    constexpr size_t      size() const { return N - 1; }
};

template <size_t N, size_t M>
constexpr auto concat(const char (&a)[N], const char (&b)[M])
{
    constexpr size_t new_length = N + M - 2 /*2 null terminators*/;

    char combined[new_length + 1]{};

    for (size_t i = 0; i < N - 1; ++i)
    {
        combined[i] = a[i];
    }

    for (size_t i = 0; i < M - 1; ++i)
    {
        combined[(N - 1) + i] = b[i];
    }
    return FixedString<new_length>(combined);
}

std::vector<std::string> StringSplit(const char* str, const char* delimiter);
std::vector<std::string> DeduplicateStringListCaseInSensitivity(const std::vector<std::string>& input);
std::string              StringJoin(const std::vector<std::string>& strings, const char* delimiter);
size_t                   StrCopy(char* dst, size_t size, const char* src);
size_t                   StrCopyFast(char* dst, size_t size, const char* src);
bool                     StrIsNumber(const std::string& s);

// FormatString (const char*)
const char* FString(const char* buffer, ...) MS_FMTFUNCTION(1, 2);

std::string LowercaseString(const char* str);
bool        GetTimeFormatString(char* buf, size_t len);

#endif