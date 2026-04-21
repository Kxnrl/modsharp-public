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

#ifndef MS_MEMORY_SCAN_H
#define MS_MEMORY_SCAN_H

#include <cstdint>
#include <string>
#include <vector>

struct CAddress;

namespace scan
{
CAddress              FindPattern(uint8_t* data, std::size_t size, std::string_view pattern) noexcept;
std::vector<CAddress> FindPatternMulti(uint8_t* data, std::size_t size, std::string_view pattern) noexcept;
CAddress              FindStr(uint8_t* data, std::size_t size, const std::string& str, bool zero_terminated = false, bool exact = false) noexcept;

CAddress              FindRVA(std::uintptr_t data, std::size_t size, uint32_t rva) noexcept;
std::vector<CAddress> FindRVAs(std::uintptr_t data, std::size_t size, uint32_t rva) noexcept;

CAddress              FindPtr(std::uintptr_t data, std::size_t size, std::uintptr_t ptr) noexcept;
std::vector<CAddress> FindPtrs(std::uintptr_t data, std::size_t size, std::uintptr_t ptr) noexcept;

CAddress              FindData(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size) noexcept;
std::vector<CAddress> FindDataMulti(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size) noexcept;
} // namespace scan

#endif