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

#include "scan.h"

#include "module.h"
#include <algorithm>
#include <bit>
#include <cstring>
#include <immintrin.h>
#include <ranges>

#ifdef PLATFORM_WINDOWS
#    include <array>
#    include <bitset>
#    include <intrin.h>
#else
#    include <cpuid.h>
#endif

struct Pattern
{
    struct Element
    {
        uint8_t byte;
        uint8_t mask;
    };

    static_assert(sizeof(Element) == 2, "Element must be 2 bytes");

    Pattern() = default;

    static Pattern FromHexString(std::string_view input)
    {
        if (input.empty()) [[unlikely]]
            throw;

        Pattern p{};
        p._elements.reserve(input.size() / 3 + 1);

        static constexpr auto hex_char_to_byte = [](char c) -> int8_t {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return -1;
        };

        p._elements.reserve(input.size() / 3 + 1);

        for (auto subrange : input | std::views::split(' '))
        {
            if (subrange.empty()) [[unlikely]]
                continue;
            std::string_view byte(subrange.begin(), subrange.end());

            if (byte[0] == '?')
            {
                p._elements.push_back({.byte = 0x00, .mask = 0x00});
            }
            else
            {
                const auto high = hex_char_to_byte(byte[0]);
                const auto low  = hex_char_to_byte(byte[1]);
                p._elements.push_back({.byte = static_cast<uint8_t>((high << 4) | low), .mask = 0xFF});
            }
        }

        // Clean up trailing wildcards (mask == 0)
        auto rit = std::ranges::find_if(std::ranges::reverse_view(p._elements),
                                        [](const Element& e) { return e.mask != 0; });

        p._elements.erase(rit.base(), p._elements.end());
        return p;
    }

    static Pattern FromString(std::string_view input, bool zero_terminated = false)
    {
        Pattern p{};
        p._elements.reserve(input.size() + (zero_terminated ? 1 : 0));

        for (const char c : input) p._elements.push_back({.byte = static_cast<uint8_t>(c), .mask = 0xFF});

        if (zero_terminated) p._elements.push_back({.byte = 0x00, .mask = 0xFF});

        return p;
    }

    [[nodiscard]] const std::vector<Element>& bytes() const
    {
        return _elements;
    }

    bool is_match(const uint8_t* data) const
    {
        const Element* current = _elements.data();
        const Element* end     = current + _elements.size() - 1;

        current++;
        data++;

        while (current != end)
        {
            // if mask is 0x00 (wildcard), result is 0 (success)
            // if mask is 0xFF (match), result is 0 only if data == pattern
            if ((data[0] ^ current->byte) & current->mask)
            {
                return false;
            }

            current++;
            data++;
        }

        return true;
    }

    bool is_match(uint8_t* data) const
    {
        return is_match(const_cast<const uint8_t*>(data));
    }

private:
    std::vector<Element> _elements{};
};

class InstructionSet
{
public:
    InstructionSet()
    {
#if defined(__clang__) || defined(__GNUC__) || defined(__GNUG__)
        _supportAvx2 = __builtin_cpu_supports("avx2");
#else

        std::array<int, 4>               cpui{};
        std::bitset<32>                  f_1_ECX_{};
        std::bitset<32>                  f_1_EDX_{};
        std::bitset<32>                  f_7_EBX_{};
        std::vector<std::array<int, 4> > data_{};

        // Calling __cpuid with 0x0 as the function_id argument
        // gets the number of the highest valid function ID.
        __cpuid(cpui.data(), 0);
        auto nIds_ = cpui[0];
        data_.reserve(nIds_);

        for (int i = 0; i <= nIds_; ++i)
        {
            __cpuidex(cpui.data(), i, 0);
            data_.push_back(cpui);
        }

        // load bitset with flags for function 0x00000001
        if (nIds_ >= 1)
        {
            f_1_ECX_ = data_[1][2];
        }

        // load bitset with flags for function 0x00000007
        if (nIds_ >= 7)
        {
            f_7_EBX_ = data_[7][1];
        }

        if (f_1_ECX_[28] && f_7_EBX_[5])
        {
            _supportAvx2 = true;
        }
#endif
    }

    [[nodiscard]] bool SupportAvx2() const
    {
        return _supportAvx2;
    }

private:
    bool _supportAvx2{};
};

static InstructionSet s_InstructionSet{};

#if defined(__clang__) || defined(__GNUC__) || defined(__GNUG__)
#    define ATTRIBUTE_AVX2 __attribute__((target("avx2")))
#    define ATTRIBUTE_SSE __attribute__((target("sse4.1")))
#else
#    define ATTRIBUTE_AVX2
#    define ATTRIBUTE_SSE
#endif

namespace detail
{
enum class SearchAction : uint8_t
{
    Continue,
    Stop,
};

template <typename Callback>
static void FindDataScalar(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size, Callback on_match) noexcept
{
    if (size < needle_size) [[unlikely]]
        return;

    const auto first_byte = needle[0];
    uint8_t*   end        = data + size - needle_size + 1;

    for (uint8_t* current = data; current < end; ++current)
    {
        current = std::find(current, end, first_byte);
        if (current == end) break;

        if (std::memcmp(current, needle, needle_size) == 0)
        {
            if (on_match(current) == SearchAction::Stop)
                return;
        }
    }
}

template <typename Callback>
static void FindPatternScalar(uint8_t* data, std::size_t size, const std::vector<Pattern::Element>& pattern, Callback on_match) noexcept
{
    const auto pattern_size = pattern.size();
    if (size < pattern_size) [[unlikely]]
        return;

    const auto first_byte = pattern[0].byte;
    uint8_t*   end        = data + size - pattern_size + 1;

    for (uint8_t* current = data; current < end; ++current)
    {
        current = std::find(current, end, first_byte);
        if (current == end) break;

        bool match = true;
        for (std::size_t i = 1; i < pattern_size; ++i)
        {
            if ((current[i] ^ pattern[i].byte) & pattern[i].mask)
            {
                match = false;
                break;
            }
        }

        if (match && on_match(current) == SearchAction::Stop) return;
    }
}

// Implementation from: http://0x80.pl/notesen/2016-11-28-simd-strfind.html

template <typename Callback>
ATTRIBUTE_SSE static void FindDataSSEImpl(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size, Callback on_match)
{
    if (needle_size < 2 || size < needle_size) [[unlikely]]
    {
        FindDataScalar(data, size, needle, needle_size, on_match);
        return;
    }

    auto* current  = data;
    auto* end      = data + size;
    auto* loop_end = end - needle_size + 1;

    constexpr auto simd_size = sizeof(__m128i);

    const auto v_first = _mm_set1_epi8(static_cast<char>(needle[0]));
    const auto v_last  = _mm_set1_epi8(static_cast<char>(needle[needle_size - 1]));

    auto process_mask = [&](uint32_t mask, uint8_t* base) -> bool {
        while (mask != 0)
        {
            uint32_t bit_pos   = std::countr_zero(mask);
            uint8_t* match_ptr = base + bit_pos;

            // only check middle bytes, first and last are already checked
            if (std::memcmp(match_ptr + 1, needle + 1, needle_size - 2) == 0) [[unlikely]]
            {
                if (on_match(match_ptr) == SearchAction::Stop) return true;
            }
            mask &= (mask - 1);
        }
        return false;
    };

    const auto* unroll_limit = loop_end - (4 * simd_size);

    while (current <= unroll_limit)
    {
        auto v_block1_first = _mm_loadu_si128(reinterpret_cast<__m128i*>(current));
        auto v_block2_first = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + simd_size));
        auto v_block3_first = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + 2 * simd_size));
        auto v_block4_first = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + 3 * simd_size));

        auto v_block1_last = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + needle_size - 1));
        auto v_block2_last = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + simd_size + needle_size - 1));
        auto v_block3_last = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + 2 * simd_size + needle_size - 1));
        auto v_block4_last = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + 3 * simd_size + needle_size - 1));

        uint32_t mask1 = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block1_first), _mm_cmpeq_epi8(v_last, v_block1_last)));
        uint32_t mask2 = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block2_first), _mm_cmpeq_epi8(v_last, v_block2_last)));
        uint32_t mask3 = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block3_first), _mm_cmpeq_epi8(v_last, v_block3_last)));
        uint32_t mask4 = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block4_first), _mm_cmpeq_epi8(v_last, v_block4_last)));

        if ((mask1 | mask2 | mask3 | mask4) != 0) [[unlikely]]
        {
            if (mask1 && process_mask(mask1, current)) return;
            if (mask2 && process_mask(mask2, current + simd_size)) return;
            if (mask3 && process_mask(mask3, current + 2 * simd_size)) return;
            if (mask4 && process_mask(mask4, current + 3 * simd_size)) return;
        }

        current += 4 * simd_size;
    }

    while (current + simd_size <= loop_end)
    {
        const auto v_block_first = _mm_loadu_si128(reinterpret_cast<const __m128i*>(current));
        const auto v_block_last  = _mm_loadu_si128(reinterpret_cast<const __m128i*>(current + needle_size - 1));

        uint32_t mask = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block_first), _mm_cmpeq_epi8(v_last, v_block_last)));

        if (mask && process_mask(mask, current)) return;

        current += simd_size;
    }

    if (current < loop_end)
    {
        FindDataScalar(current, static_cast<std::size_t>(end - current), needle, needle_size, on_match);
    }
}

template <typename Callback>
ATTRIBUTE_AVX2 static void FindDataAvx2Impl(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size, Callback on_match)
{
    if (needle_size < 2 || size < needle_size) [[unlikely]]
    {
        FindDataScalar(data, size, needle, needle_size, on_match);
        return;
    }

    auto* current  = data;
    auto* end      = data + size;
    auto* loop_end = end - needle_size + 1;

    constexpr auto simd_size = sizeof(__m256i);

    const auto v_first = _mm256_set1_epi8(static_cast<char>(needle[0]));
    const auto v_last  = _mm256_set1_epi8(static_cast<char>(needle[needle_size - 1]));

    auto process_mask = [&](uint32_t mask, uint8_t* base) -> bool {
        while (mask != 0)
        {
            uint32_t bit_pos   = std::countr_zero(mask);
            uint8_t* match_ptr = base + bit_pos;

            // only check middle bytes, first and last are already checked
            if (std::memcmp(match_ptr + 1, needle + 1, needle_size - 2) == 0) [[unlikely]]
            {
                if (on_match(match_ptr) == SearchAction::Stop) return true;
            }
            mask &= (mask - 1);
        }
        return false;
    };

    const auto* unroll_limit = loop_end - (4 * simd_size);

    while (current <= unroll_limit)
    {
        auto v_block1_first = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current));
        auto v_block2_first = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + simd_size));
        auto v_block3_first = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + 2 * simd_size));
        auto v_block4_first = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + 3 * simd_size));

        auto v_block1_last = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + needle_size - 1));
        auto v_block2_last = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + simd_size + needle_size - 1));
        auto v_block3_last = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + 2 * simd_size + needle_size - 1));
        auto v_block4_last = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + 3 * simd_size + needle_size - 1));

        uint32_t mask1 = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block1_first), _mm256_cmpeq_epi8(v_last, v_block1_last)));
        uint32_t mask2 = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block2_first), _mm256_cmpeq_epi8(v_last, v_block2_last)));
        uint32_t mask3 = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block3_first), _mm256_cmpeq_epi8(v_last, v_block3_last)));
        uint32_t mask4 = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block4_first), _mm256_cmpeq_epi8(v_last, v_block4_last)));

        if ((mask1 | mask2 | mask3 | mask4) != 0) [[unlikely]]
        {
            if (mask1 && process_mask(mask1, current)) return;
            if (mask2 && process_mask(mask2, current + simd_size)) return;
            if (mask3 && process_mask(mask3, current + 2 * simd_size)) return;
            if (mask4 && process_mask(mask4, current + 3 * simd_size)) return;
        }

        current += 4 * simd_size;
    }

    while (current + simd_size <= loop_end)
    {
        const auto v_block_first = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(current));
        const auto v_block_last  = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(current + needle_size - 1));

        uint32_t mask = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block_first), _mm256_cmpeq_epi8(v_last, v_block_last)));

        if (mask && process_mask(mask, current)) return;

        current += simd_size;
    }

    if (current < loop_end)
    {
        FindDataScalar(current, static_cast<std::size_t>(end - current), needle, needle_size, on_match);
    }
}

static CAddress FindDataSSE(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size)
{
    CAddress   result{};
    const auto base = reinterpret_cast<uintptr_t>(data);

    FindDataSSEImpl(data, size, needle, needle_size,
                    [&result, base](CAddress match) {
                        result = match - base;
                        return detail::SearchAction::Stop;
                    });
    return result;
}

static std::vector<CAddress> FindDataMultiSSE(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size)
{
    std::vector<CAddress> results{};
    results.reserve(8);

    const auto base = reinterpret_cast<uintptr_t>(data);

    FindDataSSEImpl(data, size, needle, needle_size,
                    [&results, base](CAddress match) {
                        results.emplace_back(match - base);
                        return detail::SearchAction::Continue;
                    });
    return results;
}

ATTRIBUTE_AVX2

static CAddress FindDataAVX2(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size)
{
    CAddress   result{};
    const auto base = reinterpret_cast<uintptr_t>(data);

    FindDataAvx2Impl(data, size, needle, needle_size,
                     [&result, base](CAddress match) {
                         result = match - base;
                         return detail::SearchAction::Stop;
                     });
    return result;
}

ATTRIBUTE_AVX2

static std::vector<CAddress> FindDataMultiAVX2(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size)
{
    std::vector<CAddress> results{};
    results.reserve(8);

    const auto base = reinterpret_cast<uintptr_t>(data);

    FindDataAvx2Impl(data, size, needle, needle_size,
                     [&results, base](CAddress match) {
                         results.emplace_back(match - base);
                         return detail::SearchAction::Continue;
                     });
    return results;
}

template <typename Callback>
ATTRIBUTE_SSE static void FindPatternSSEImpl(uint8_t* data, std::size_t size, const Pattern& pattern, Callback on_match)
{
    const auto& pattern_bytes = pattern.bytes();
    const auto  pattern_size  = pattern_bytes.size();

    if (pattern_size < 2 || size < pattern_size) [[unlikely]]
    {
        FindPatternScalar(data, size, pattern_bytes, on_match);
        return;
    }

    auto* current  = data;
    auto* end      = data + size;
    auto* loop_end = end - pattern_size + 1;

    constexpr auto simd_size = sizeof(__m128i);

    const auto v_first = _mm_set1_epi8(static_cast<char>(pattern_bytes.front().byte));
    const auto v_last  = _mm_set1_epi8(static_cast<char>(pattern_bytes.back().byte));

    auto process_mask = [&](uint32_t mask, uint8_t* base) -> bool {
        while (mask != 0)
        {
            uint32_t bit_pos   = std::countr_zero(mask);
            uint8_t* match_ptr = base + bit_pos;

            if (pattern.is_match(match_ptr)) [[unlikely]]
            {
                if (on_match(match_ptr) == SearchAction::Stop) return true;
            }
            mask &= (mask - 1);
        }
        return false;
    };

    const auto* unroll_limit = loop_end - (4 * simd_size);

    while (current <= unroll_limit)
    {
        auto v_block1_first = _mm_loadu_si128(reinterpret_cast<__m128i*>(current));
        auto v_block2_first = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + simd_size));
        auto v_block3_first = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + 2 * simd_size));
        auto v_block4_first = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + 3 * simd_size));

        auto v_block1_last = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + pattern_size - 1));
        auto v_block2_last = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + simd_size + pattern_size - 1));
        auto v_block3_last = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + 2 * simd_size + pattern_size - 1));
        auto v_block4_last = _mm_loadu_si128(reinterpret_cast<__m128i*>(current + 3 * simd_size + pattern_size - 1));

        uint32_t mask1 = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block1_first), _mm_cmpeq_epi8(v_last, v_block1_last)));
        uint32_t mask2 = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block2_first), _mm_cmpeq_epi8(v_last, v_block2_last)));
        uint32_t mask3 = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block3_first), _mm_cmpeq_epi8(v_last, v_block3_last)));
        uint32_t mask4 = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block4_first), _mm_cmpeq_epi8(v_last, v_block4_last)));

        if ((mask1 | mask2 | mask3 | mask4) != 0) [[unlikely]]
        {
            if (mask1 && process_mask(mask1, current)) return;
            if (mask2 && process_mask(mask2, current + simd_size)) return;
            if (mask3 && process_mask(mask3, current + 2 * simd_size)) return;
            if (mask4 && process_mask(mask4, current + 3 * simd_size)) return;
        }

        current += 4 * simd_size;
    }

    while (current + simd_size <= loop_end)
    {
        const auto v_block_first = _mm_loadu_si128(reinterpret_cast<const __m128i*>(current));
        const auto v_block_last  = _mm_loadu_si128(reinterpret_cast<const __m128i*>(current + pattern_size - 1));

        uint32_t mask = _mm_movemask_epi8(_mm_and_si128(
            _mm_cmpeq_epi8(v_first, v_block_first), _mm_cmpeq_epi8(v_last, v_block_last)));

        if (mask && process_mask(mask, current)) return;

        current += simd_size;
    }

    if (current < loop_end)
    {
        FindPatternScalar(current, static_cast<std::size_t>(end - current), pattern_bytes, on_match);
    }
}

template <typename Callback>
ATTRIBUTE_AVX2 static void FindPatternAvx2Impl(uint8_t* data, std::size_t size, const Pattern& pattern, Callback on_match)
{
    const auto& pattern_bytes = pattern.bytes();
    const auto  pattern_size  = pattern_bytes.size();

    if (pattern_size < 2 || size < pattern_size) [[unlikely]]
    {
        FindPatternScalar(data, size, pattern_bytes, on_match);
        return;
    }

    auto* current  = data;
    auto* end      = data + size;
    auto* loop_end = end - pattern_size + 1;

    constexpr auto simd_size = sizeof(__m256i);

    const auto v_first = _mm256_set1_epi8(static_cast<char>(pattern_bytes.front().byte));
    const auto v_last  = _mm256_set1_epi8(static_cast<char>(pattern_bytes.back().byte));

    auto process_mask = [&](uint32_t mask, uint8_t* base) -> bool {
        while (mask != 0)
        {
            uint32_t bit_pos   = std::countr_zero(mask);
            uint8_t* match_ptr = base + bit_pos;

            if (pattern.is_match(match_ptr)) [[unlikely]]
            {
                if (on_match(match_ptr) == SearchAction::Stop) return true;
            }
            mask &= (mask - 1);
        }
        return false;
    };

    const auto* unroll_limit = loop_end - (4 * simd_size);

    while (current <= unroll_limit)
    {
        auto v_block1_first = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current));
        auto v_block2_first = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + simd_size));
        auto v_block3_first = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + 2 * simd_size));
        auto v_block4_first = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + 3 * simd_size));

        auto v_block1_last = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + pattern_size - 1));
        auto v_block2_last = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + simd_size + pattern_size - 1));
        auto v_block3_last = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + 2 * simd_size + pattern_size - 1));
        auto v_block4_last = _mm256_loadu_si256(reinterpret_cast<__m256i*>(current + 3 * simd_size + pattern_size - 1));

        uint32_t mask1 = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block1_first), _mm256_cmpeq_epi8(v_last, v_block1_last)));
        uint32_t mask2 = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block2_first), _mm256_cmpeq_epi8(v_last, v_block2_last)));
        uint32_t mask3 = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block3_first), _mm256_cmpeq_epi8(v_last, v_block3_last)));
        uint32_t mask4 = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block4_first), _mm256_cmpeq_epi8(v_last, v_block4_last)));

        if ((mask1 | mask2 | mask3 | mask4) != 0) [[unlikely]]
        {
            if (mask1 && process_mask(mask1, current)) return;
            if (mask2 && process_mask(mask2, current + simd_size)) return;
            if (mask3 && process_mask(mask3, current + 2 * simd_size)) return;
            if (mask4 && process_mask(mask4, current + 3 * simd_size)) return;
        }

        current += 4 * simd_size;
    }

    while (current + simd_size <= loop_end)
    {
        const auto v_block_first = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(current));
        const auto v_block_last  = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(current + pattern_size - 1));

        uint32_t mask = _mm256_movemask_epi8(_mm256_and_si256(
            _mm256_cmpeq_epi8(v_first, v_block_first), _mm256_cmpeq_epi8(v_last, v_block_last)));

        if (mask && process_mask(mask, current)) return;

        current += simd_size;
    }

    if (current < loop_end)
    {
        FindPatternScalar(current, static_cast<std::size_t>(end - current), pattern_bytes, on_match);
    }
}

static CAddress FindPatternSSE(uint8_t* data, std::size_t size, const Pattern& pattern)
{
    CAddress   result{};
    const auto base = reinterpret_cast<uintptr_t>(data);

    FindPatternSSEImpl(data, size, pattern,
                       [&result, base](CAddress match) {
                           result = match - base;
                           return detail::SearchAction::Stop;
                       });
    return result;
}

static std::vector<CAddress> FindPatternMultiSSE(uint8_t* data, std::size_t size, const Pattern& pattern)
{
    std::vector<CAddress> results{};
    results.reserve(8);

    const auto base = reinterpret_cast<uintptr_t>(data);

    FindPatternSSEImpl(data, size, pattern,
                       [&results, base](CAddress match) {
                           results.emplace_back(match - base);
                           return detail::SearchAction::Continue;
                       });
    return results;
}

ATTRIBUTE_AVX2
static CAddress FindPatternAVX2(uint8_t* data, std::size_t size, const Pattern& pattern)
{
    CAddress   result{};
    const auto base = reinterpret_cast<uintptr_t>(data);

    FindPatternAvx2Impl(data, size, pattern,
                        [&result, base](CAddress match) {
                            result = match - base;
                            return detail::SearchAction::Stop;
                        });
    return result;
}

ATTRIBUTE_AVX2
static std::vector<CAddress> FindPatternMultiAVX2(uint8_t* data, std::size_t size, const Pattern& pattern)
{
    std::vector<CAddress> results{};
    results.reserve(8);

    const auto base = reinterpret_cast<uintptr_t>(data);

    FindPatternAvx2Impl(data, size, pattern,
                        [&results, base](CAddress match) {
                            results.emplace_back(match - base);
                            return detail::SearchAction::Continue;
                        });
    return results;
}

template <typename T, typename Callback>
ATTRIBUTE_SSE static void FindValueSSEImpl(std::uintptr_t data, std::size_t size, T value, Callback on_match) noexcept
{
    constexpr auto value_size    = sizeof(T);
    constexpr auto simd_size     = sizeof(__m128i);
    constexpr int  unroll_factor = 4;

    const auto* p_data = reinterpret_cast<const uint8_t*>(data);

    __m128i v_value;
    if constexpr (value_size == 8)
    {
        long long val_i;
        std::memcpy(&val_i, &value, sizeof(val_i));
        v_value = _mm_set1_epi64x(val_i);
    }
    else
    {
        int val_i;
        std::memcpy(&val_i, &value, sizeof(val_i));
        v_value = _mm_set1_epi32(val_i);
    }

    auto get_cmp_mask = [&v_value](__m128i v_data) ATTRIBUTE_SSE -> uint32_t {
        if constexpr (value_size == 8)
        {
            return static_cast<uint32_t>(_mm_movemask_pd(
                _mm_castsi128_pd(_mm_cmpeq_epi64(v_data, v_value))));
        }
        else
        {
            return static_cast<uint32_t>(_mm_movemask_ps(
                _mm_castsi128_ps(_mm_cmpeq_epi32(v_data, v_value))));
        }
    };

    auto process_mask = [&on_match, p_data](uint32_t mask, const uint8_t* base) -> bool {
        while (mask != 0)
        {
            const int         element_index = std::countr_zero(mask);
            const std::size_t offset        = (base - p_data) + (element_index * value_size);

            if (on_match(offset) == SearchAction::Stop) return true;

            mask &= (mask - 1);
        }
        return false;
    };

    const auto*       current    = p_data;
    const auto* const scalar_end = p_data + size - value_size;
    const auto* const unroll_end = p_data + size - (unroll_factor * simd_size);

    while (current <= unroll_end)
    {
        const auto v_data1 = _mm_loadu_si128(reinterpret_cast<const __m128i*>(current));
        const auto v_data2 = _mm_loadu_si128(reinterpret_cast<const __m128i*>(current + simd_size));
        const auto v_data3 = _mm_loadu_si128(reinterpret_cast<const __m128i*>(current + 2 * simd_size));
        const auto v_data4 = _mm_loadu_si128(reinterpret_cast<const __m128i*>(current + 3 * simd_size));

        uint32_t mask1 = get_cmp_mask(v_data1);
        uint32_t mask2 = get_cmp_mask(v_data2);
        uint32_t mask3 = get_cmp_mask(v_data3);
        uint32_t mask4 = get_cmp_mask(v_data4);

        if ((mask1 | mask2 | mask3 | mask4) != 0) [[unlikely]]
        {
            if (mask1 && process_mask(mask1, current)) return;
            if (mask2 && process_mask(mask2, current + simd_size)) return;
            if (mask3 && process_mask(mask3, current + 2 * simd_size)) return;
            if (mask4 && process_mask(mask4, current + 3 * simd_size)) return;
        }

        current += unroll_factor * simd_size;
    }

    const auto* const simd_end = p_data + size - simd_size;
    while (current <= simd_end)
    {
        uint32_t mask = get_cmp_mask(_mm_loadu_si128(reinterpret_cast<const __m128i*>(current)));

        if (mask && process_mask(mask, current)) return;

        current += simd_size;
    }

    while (current <= scalar_end)
    {
        T val;
        std::memcpy(&val, current, value_size);

        if (val == value)
        {
            if (on_match(static_cast<std::size_t>(current - p_data)) == SearchAction::Stop)
            {
                return;
            }
        }
        current += value_size;
    }
}

template <typename T, typename Callback>
ATTRIBUTE_AVX2 static void FindValueAVX2Impl(std::uintptr_t data, std::size_t size, T value, Callback on_match) noexcept
{
    constexpr auto value_size    = sizeof(T);
    constexpr auto simd_size     = sizeof(__m256i);
    constexpr int  unroll_factor = 4;

    const auto* p_data = reinterpret_cast<const uint8_t*>(data);

    __m256i v_value;
    if constexpr (value_size == 8)
    {
        long long val_i;
        std::memcpy(&val_i, &value, sizeof(val_i));
        v_value = _mm256_set1_epi64x(val_i);
    }
    else
    {
        int val_i;
        std::memcpy(&val_i, &value, sizeof(val_i));
        v_value = _mm256_set1_epi32(val_i);
    }

    auto get_cmp_mask = [&v_value](__m256i v_data) ATTRIBUTE_AVX2 -> uint32_t {
        if constexpr (value_size == 8)
        {
            return static_cast<uint32_t>(_mm256_movemask_pd(
                _mm256_castsi256_pd(_mm256_cmpeq_epi64(v_data, v_value))));
        }
        else
        {
            return static_cast<uint32_t>(_mm256_movemask_ps(
                _mm256_castsi256_ps(_mm256_cmpeq_epi32(v_data, v_value))));
        }
    };

    auto process_mask = [&on_match, p_data](uint32_t mask, const uint8_t* base) -> bool {
        while (mask != 0)
        {
            const int         element_index = std::countr_zero(mask);
            const std::size_t offset        = (base - p_data) + (element_index * value_size);

            if (on_match(offset) == SearchAction::Stop) return true;

            mask &= (mask - 1);
        }
        return false;
    };

    const auto*       current    = p_data;
    const auto* const scalar_end = p_data + size - value_size;
    const auto* const unroll_end = p_data + size - (unroll_factor * simd_size);

    while (current <= unroll_end)
    {
        const auto v_data1 = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(current));
        const auto v_data2 = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(current + simd_size));
        const auto v_data3 = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(current + 2 * simd_size));
        const auto v_data4 = _mm256_loadu_si256(reinterpret_cast<const __m256i*>(current + 3 * simd_size));

        uint32_t mask1 = get_cmp_mask(v_data1);
        uint32_t mask2 = get_cmp_mask(v_data2);
        uint32_t mask3 = get_cmp_mask(v_data3);
        uint32_t mask4 = get_cmp_mask(v_data4);

        if ((mask1 | mask2 | mask3 | mask4) != 0) [[unlikely]]
        {
            if (mask1 && process_mask(mask1, current)) return;
            if (mask2 && process_mask(mask2, current + simd_size)) return;
            if (mask3 && process_mask(mask3, current + 2 * simd_size)) return;
            if (mask4 && process_mask(mask4, current + 3 * simd_size)) return;
        }

        current += unroll_factor * simd_size;
    }

    const auto* const simd_end = p_data + size - simd_size;
    while (current <= simd_end)
    {
        uint32_t mask = get_cmp_mask(_mm256_loadu_si256(reinterpret_cast<const __m256i*>(current)));

        if (mask && process_mask(mask, current)) return;

        current += simd_size;
    }

    while (current <= scalar_end)
    {
        T val;
        std::memcpy(&val, current, value_size);

        if (val == value)
        {
            if (on_match(static_cast<std::size_t>(current - p_data)) == SearchAction::Stop)
            {
                return;
            }
        }
        current += value_size;
    }
}
} // namespace detail

CAddress scan::FindPattern(uint8_t* data, std::size_t size, std::string_view pattern) noexcept
{
    auto pat = Pattern::FromHexString(pattern);

    if (s_InstructionSet.SupportAvx2())
        return detail::FindPatternAVX2(data, size, pat);

    return detail::FindPatternSSE(data, size, pat);
}

std::vector<CAddress> scan::FindPatternMulti(uint8_t* data, std::size_t size, std::string_view pattern) noexcept
{
    auto pat = Pattern::FromHexString(pattern);

    if (s_InstructionSet.SupportAvx2())
        return detail::FindPatternMultiAVX2(data, size, pat);

    return detail::FindPatternMultiSSE(data, size, pat);
}

CAddress scan::FindStr(uint8_t* data, std::size_t size, const std::string& str, bool zero_terminated, bool exact) noexcept
{
    const uint8_t* needle      = zero_terminated ? reinterpret_cast<const uint8_t*>(str.c_str()) : reinterpret_cast<const uint8_t*>(str.data());
    std::size_t    needle_size = zero_terminated ? str.size() + 1 : str.size();

    if (!exact)
    {
        return FindData(data, size, needle, needle_size);
    }

    CAddress   result{};
    const auto base = reinterpret_cast<uintptr_t>(data);

    auto callback = [&result, base, data](CAddress match) {
        auto offset = match.GetPtr() - base;
        if (offset == 0 || data[offset - 1] == '\0')
        {
            result = offset;
            return detail::SearchAction::Stop;
        }
        return detail::SearchAction::Continue;
    };

    if (s_InstructionSet.SupportAvx2())
    {
        detail::FindDataAvx2Impl(data, size, needle, needle_size, callback);
    }
    else
    {
        detail::FindDataSSEImpl(data, size, needle, needle_size, callback);
    }

    return result;
}

CAddress scan::FindPtr(std::uintptr_t data, std::size_t size, std::uintptr_t ptr) noexcept
{
    CAddress result{};
    auto     callback = [&result](CAddress address) {
        result = address;
        return detail::SearchAction::Stop;
    };

    if (s_InstructionSet.SupportAvx2())
    {
        detail::FindValueAVX2Impl(data, size, ptr, callback);
    }
    else
    {
        detail::FindValueSSEImpl(data, size, ptr, callback);
    }

    return result;
}

CAddress scan::FindRVA(std::uintptr_t data, std::size_t size, uint32_t rva) noexcept
{
    CAddress result{};
    auto     callback = [&result](CAddress address) {
        result = address;
        return detail::SearchAction::Stop;
    };

    if (s_InstructionSet.SupportAvx2())
    {
        detail::FindValueAVX2Impl(data, size, rva, callback);
    }
    else
    {
        detail::FindValueSSEImpl(data, size, rva, callback);
    }

    return result;
}

std::vector<CAddress> scan::FindRVAs(std::uintptr_t data, std::size_t size, uint32_t rva) noexcept
{
    std::vector<CAddress> result{};
    auto                  callback = [&result](CAddress address) {
        result.emplace_back(address);
        return detail::SearchAction::Continue;
    };

    if (s_InstructionSet.SupportAvx2())
    {
        detail::FindValueAVX2Impl(data, size, rva, callback);
    }
    else
    {
        detail::FindValueSSEImpl(data, size, rva, callback);
    }

    return result;
}

std::vector<CAddress> scan::FindPtrs(std::uintptr_t data, std::size_t size, std::uintptr_t ptr) noexcept
{
    std::vector<CAddress> result{};
    auto                  callback = [&result](CAddress address) {
        result.emplace_back(address);
        return detail::SearchAction::Continue;
    };

    if (s_InstructionSet.SupportAvx2())
    {
        detail::FindValueAVX2Impl(data, size, ptr, callback);
    }
    else
    {
        detail::FindValueSSEImpl(data, size, ptr, callback);
    }

    return result;
}

CAddress scan::FindData(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size) noexcept
{
    if (s_InstructionSet.SupportAvx2()) return detail::FindDataAVX2(data, size, needle, needle_size);

    return detail::FindDataSSE(data, size, needle, needle_size);
}

std::vector<CAddress> scan::FindDataMulti(uint8_t* data, std::size_t size, const uint8_t* needle, std::size_t needle_size) noexcept
{
    if (s_InstructionSet.SupportAvx2()) return detail::FindDataMultiAVX2(data, size, needle, needle_size);

    return detail::FindDataMultiSSE(data, size, needle, needle_size);
}
