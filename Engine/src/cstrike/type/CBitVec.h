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

#ifndef CSTRIKE_TYPE_CBITVEC_H
#define CSTRIKE_TYPE_CBITVEC_H

#include <array>
#include <cstdint>

template <int NUM_BITS>
class CFixedBitVecBase
{
public:
    static constexpr std::size_t BitsPerDWord = std::numeric_limits<uint32_t>::digits;
    static constexpr std::size_t NumDWords    = (NUM_BITS + BitsPerDWord - 1) / BitsPerDWord;

    constexpr std::size_t GetNumBits() const noexcept { return NUM_BITS; }
    constexpr std::size_t GetNumDWords() const noexcept { return NumDWords; }

    constexpr uint32_t*       Base() noexcept { return m_ints.data(); }
    constexpr const uint32_t* Base() const noexcept { return m_ints.data(); }

private:
    std::array<uint32_t, NumDWords> m_ints{};
};

template <class BASE_OPS>
class CBitVecT : public BASE_OPS
{
    static constexpr std::size_t WordIndex(std::size_t bitNum) noexcept
    {
        return bitNum / BASE_OPS::BitsPerDWord;
    }

    static constexpr uint32_t BitMask(std::size_t bitNum) noexcept
    {
        return 1U << (bitNum % BASE_OPS::BitsPerDWord);
    }

public:
    CBitVecT() = default;

    constexpr bool IsBitSet(int bitNum) const noexcept
    {
        const std::size_t bit_idx = bitNum;
        return (this->Base()[WordIndex(bit_idx)] & BitMask(bit_idx)) != 0;
    }

    void Set(int bitNum) noexcept
    {
        const std::size_t bit_idx = bitNum;
        this->Base()[WordIndex(bit_idx)] |= BitMask(bit_idx);
    }

    void Clear(int bitNum) noexcept
    {
        const std::size_t bit_idx = bitNum;
        this->Base()[WordIndex(bit_idx)] &= ~BitMask(bit_idx);
    }

    void ClearAll() noexcept
    {
        if (this->Base())
        {
            std::fill(this->Base(), this->Base() + this->GetNumDWords(), 0);
        }
    }

    CBitVecT<BASE_OPS>& operator=(const CBitVecT<BASE_OPS>& other) = default;

    bool operator==(const CBitVecT<BASE_OPS>& other) const noexcept
    {
        return static_cast<const BASE_OPS&>(*this) == static_cast<const BASE_OPS&>(other);
    }

    bool operator!=(const CBitVecT<BASE_OPS>& other) { return !operator==(other); }
};

template <int NUM_BITS>
class CBitVec : public CBitVecT<CFixedBitVecBase<NUM_BITS> >
{
public:
    CBitVec() = default;
};

#endif