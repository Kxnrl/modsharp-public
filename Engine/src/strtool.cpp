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

#include "strtool.h"

#include <algorithm>
#include <cstdarg>
#include <cstring>
#include <ctime>
#include <numeric>
#include <sstream>
#include <string>
#include <unordered_set>
#include <vector>

std::vector<std::string> StringSplit(const char* str, const char* delimiter)
{
    std::vector<std::string> result;

    if (!str || str[0] == 0 || !delimiter || *delimiter == '\0')
        return result;

    std::string_view strV(str);
    size_t           pos;

    while ((pos = strV.find(delimiter)) != std::string_view::npos)
    {
        result.emplace_back(strV.substr(0, pos));
        strV.remove_prefix(pos + std::string_view(delimiter).size());
    }

    result.emplace_back(strV);
    return result;
}

std::vector<std::string> DeduplicateStringListCaseInSensitivity(const std::vector<std::string>& input)
{
    std::unordered_set<std::string> seen;
    std::vector<std::string>        result;

    for (const auto& s : input)
    {
        auto lower_s = LowercaseString(s.c_str());
        if (seen.insert(lower_s).second)
        {
            result.push_back(s);
        }
    }
    return result;
}

std::string StringJoin(const std::vector<std::string>& strings, const char* delimiter)
{
    if (strings.empty())
        return {};

    return std::accumulate(std::next(strings.begin()), strings.end(),
                           strings[0],
                           [&](const std::string& a, const std::string& b) {
                               return a + delimiter + b;
                           });
}

size_t StrCopy(char* dst, size_t size, const char* src)
{
    const char* osrc  = src;
    size_t      nleft = size;

    /* Copy as many bytes as will fit. */
    if (nleft != 0)
    {
        while (--nleft != 0)
        {
            if ((*dst++ = *src++) == '\0')
                break;
        }
    }

    /* Not enough room in dst, add NUL and traverse rest of src. */
    if (nleft == 0)
    {
        if (size != 0)
            *dst = '\0'; /* NUL-terminate dst */
        while (*src++);
    }

    return (src - osrc - 1); /* count does not include NUL */
}

size_t StrCopyFast(char* dst, size_t size, const char* src)
{
    const auto textLength = strnlen(src, size - 1);
    const auto copyLength = textLength < size ? textLength : size - 1;
    memcpy(dst, src, copyLength);
    dst[copyLength] = '\0';
    return copyLength;
}

bool StrIsNumber(const std::string& s)
{
    return !s.empty() && std::ranges::find_if(s, [](unsigned char c) { return !std::isdigit(c); }) == s.end();
}

const char* FString(const char* buffer, ...)
{
    va_list args;
    va_start(args, buffer);

    thread_local static char message[1024];
    vsnprintf(message, sizeof(message), buffer, args);

    va_end(args);

    return message;
}

int strpos(const char* str, const char* subStr)
{
    const char* ptr = strstr(str, subStr);
    if (ptr != nullptr)
    {
        return static_cast<int>(ptr - str);
    }
    return -1;
}

int stripos(const char* str, const char* subStr)
{
    if (str == nullptr || subStr == nullptr)
        return -1;

    for (int i = 0; str[i]; i++)
    {
        int j = 0;

        while (str[i + j] && subStr[j] && std::tolower(str[i + j]) == std::tolower(subStr[j]))
        {
            j++;
        }
        if (!subStr[j])
        {
            return i;
        }
    }
    return -1;
}

std::string LowercaseString(const char* str)
{
    std::string result(str);
    std::ranges::transform(result, result.begin(), [](char c) { return static_cast<char>(std::tolower(c)); });
    return result;
}

std::string XorString(char key, const char* str)
{
    std::string s(str);
    for (char& c : s)
    {
        c ^= key;
    }
    return s;
}

std::string HexArrayToString(const int64_t* array, const size_t size)
{
    std::string result;
    for (size_t i = 0; i < size; ++i)
    {
        const auto v = array[i];

        // 忽略非ASCII码
        if (v > 127 || v < 0)
        {
            continue;
        }
        result.push_back(static_cast<char>(v));
    }
    return result;
}

bool GetTimeFormatString(char* buf, size_t len)
{
    const auto s = std::time(nullptr);
    auto       n = std::localtime(&s);
    if (n == nullptr)
        return false;
    return strftime(buf, len, "%m/%d %H:%M:%S", n) > 0;
}