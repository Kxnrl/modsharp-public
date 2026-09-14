// Ported from https://github.com/Wend4r/sourcesdk/blob/main/public/tier1/utlcommon.h

#ifndef CSTRIKE_TYPE_UTLCOMMON_H
#define CSTRIKE_TYPE_UTLCOMMON_H

#include <cstdint>
#include <cstddef>
#include <cstring>
#include <type_traits>

#ifndef VALIGNOF
#    define VALIGNOF(x) alignof(x)
#endif

template <size_t NUM, class T, int ALIGN>
struct alignas(ALIGN) AlignedByteArrayExplicit_t
{
    uint8_t m_data[NUM * sizeof(T)];

    uint8_t*       Base() { return m_data; }
    const uint8_t* Base() const { return m_data; }
    int            Count() const { return NUM * static_cast<int>(sizeof(T)); }
    uint8_t&       operator[](int i) { return m_data[i]; }
    const uint8_t& operator[](int i) const { return m_data[i]; }
};

template <size_t NUM, class T>
using AlignedByteArray_t = AlignedByteArrayExplicit_t<NUM, T, VALIGNOF(T)>;

struct undefined_t;

template <bool bCondition, class TypeIfTrue, class TypeIfFalse>
struct CTypeSelect;

template <class TypeIfTrue, class TypeIfFalse>
struct CTypeSelect<true, TypeIfTrue, TypeIfFalse>
{
    using type = TypeIfTrue;
};

template <class TypeIfTrue, class TypeIfFalse>
struct CTypeSelect<false, TypeIfTrue, TypeIfFalse>
{
    using type = TypeIfFalse;
};

struct empty_t {};

template <class KeyT, class ValueT = empty_t>
struct CUtlKeyValuePair
{
    using ValueReturn_t = ValueT;

    KeyT   m_key;
    ValueT m_value;

    CUtlKeyValuePair() = default;

    explicit CUtlKeyValuePair(const KeyT& k) : m_key(k), m_value() {}

    template <typename KInit>
    explicit CUtlKeyValuePair(const KInit& k) : m_key(k), m_value() {}

    template <typename KInit, typename VInit>
    CUtlKeyValuePair(const KInit& k, const VInit& v) : m_key(k), m_value(v) {}

    ValueT&       GetValue()       { return m_value; }
    const ValueT& GetValue() const { return m_value; }
};

template <class KeyT>
struct CUtlKeyValuePair<KeyT, empty_t>
{
    using ValueReturn_t = const KeyT;

    KeyT m_key{};

    CUtlKeyValuePair() = default;

    explicit CUtlKeyValuePair(const KeyT& k) : m_key(k) {}

    template <typename KInit>
    explicit CUtlKeyValuePair(const KInit& k) : m_key(k) {}

    template <typename KInit>
    CUtlKeyValuePair(const KInit& k, empty_t) : m_key(k) {}

    const KeyT& GetValue() const { return m_key; }
};

template <typename T, typename = void>
struct HasAltArgumentType : std::false_type {};

template <typename T>
struct HasAltArgumentType<T, std::void_t<typename T::AltArgumentType_t>> : std::true_type {};

template <typename ArgT, typename AltT = undefined_t>
struct ArgumentTypeInfoImpl
{
    using Arg_t = ArgT;
    using Alt_t = AltT;
};

template <typename T, typename = void>
struct ArgumentTypeInfo : ArgumentTypeInfoImpl<const T&, undefined_t> {};

template <typename T>
struct ArgumentTypeInfo<T, std::enable_if_t<HasAltArgumentType<T>::value>>
    : ArgumentTypeInfoImpl<const T&, typename T::AltArgumentType_t> {};

template <> struct ArgumentTypeInfo<bool>               : ArgumentTypeInfoImpl<bool> {};
template <> struct ArgumentTypeInfo<char>               : ArgumentTypeInfoImpl<char> {};
template <> struct ArgumentTypeInfo<signed char>        : ArgumentTypeInfoImpl<signed char> {};
template <> struct ArgumentTypeInfo<unsigned char>      : ArgumentTypeInfoImpl<unsigned char> {};
template <> struct ArgumentTypeInfo<signed short>       : ArgumentTypeInfoImpl<signed short> {};
template <> struct ArgumentTypeInfo<unsigned short>     : ArgumentTypeInfoImpl<unsigned short> {};
template <> struct ArgumentTypeInfo<signed int>         : ArgumentTypeInfoImpl<signed int> {};
template <> struct ArgumentTypeInfo<unsigned int>       : ArgumentTypeInfoImpl<unsigned int> {};
template <> struct ArgumentTypeInfo<signed long>        : ArgumentTypeInfoImpl<signed long> {};
template <> struct ArgumentTypeInfo<unsigned long>      : ArgumentTypeInfoImpl<unsigned long> {};
template <> struct ArgumentTypeInfo<signed long long>   : ArgumentTypeInfoImpl<signed long long> {};
template <> struct ArgumentTypeInfo<unsigned long long> : ArgumentTypeInfoImpl<unsigned long long> {};
template <> struct ArgumentTypeInfo<float>              : ArgumentTypeInfoImpl<float> {};
template <> struct ArgumentTypeInfo<double>             : ArgumentTypeInfoImpl<double> {};
template <> struct ArgumentTypeInfo<long double>        : ArgumentTypeInfoImpl<long double> {};
template <> struct ArgumentTypeInfo<wchar_t>            : ArgumentTypeInfoImpl<wchar_t> {};

template <typename T> struct ArgumentTypeInfo<T*>              : ArgumentTypeInfoImpl<T*> {};

template <typename T> struct ArgumentTypeInfo<const T>          : ArgumentTypeInfo<T> {};
template <typename T> struct ArgumentTypeInfo<volatile T>       : ArgumentTypeInfo<T> {};
template <typename T> struct ArgumentTypeInfo<const volatile T> : ArgumentTypeInfo<T> {};
template <typename T> struct ArgumentTypeInfo<T&>               : ArgumentTypeInfo<T> {};

template <class KeyT>
struct DefaultHashFunctor
{
    unsigned int operator()(const KeyT& key) const
    {
        return key.GetHashCode();
    }
};

template <class KeyT>
struct DefaultEqualFunctor
{
    bool operator()(const KeyT& a, const KeyT& b) const
    {
        return a == b;
    }
};

struct Mix32HashFunctor
{
    unsigned int operator()(uint32_t n) const
    {
        n = ( n + 0x7ed55d16 ) + ( n << 12 );
        n = ( n ^ 0xc761c23c ) ^ ( n >> 19 );
        n = ( n + 0x165667b1 ) + ( n << 5 );
        n = ( n + 0xd3a2646c ) ^ ( n << 9 );
        n = ( n + 0xfd7046c5 ) + ( n << 3 );
        n = ( n ^ 0xb55a4f09 ) ^ ( n >> 16 );
        return n;
    }
};

struct Mix64HashFunctor
{
    unsigned int operator()(uint64_t s) const
    {
        s = ( ~s ) + ( s << 21 );
        s = s ^ ( s >> 24 );
        s = ( s + ( s << 3 ) ) + ( s << 8 );
        s = s ^ ( s >> 14 );
        s = ( s + ( s << 2 ) ) + ( s << 4 );
        s = s ^ ( s >> 28 );
        s = s + ( s << 31 );
        return (unsigned int)s;
    }
};

struct StringHashFunctor
{
    unsigned int operator()(const char* s) const
    {
        uint32_t h = 2166136261u;
        if (s)
        {
            for ( ; *s; ++s )
            {
                h = (h ^ (unsigned char)*s) * 16777619u;
            }
        }
        return (h ^ (h << 17)) + (h >> 21);
    }
};

struct PointerHashFunctor
{
    unsigned int operator()(const void* key) const
    {
        uintptr_t val = reinterpret_cast<uintptr_t>(key);
#if defined(_M_X64) || defined(__x86_64__)
        return Mix64HashFunctor()(static_cast<uint64_t>(val));
#else
        return Mix32HashFunctor()(static_cast<uint32_t>(val));
#endif
    }
};

struct StringEqualFunctor
{
    bool operator()(const char* a, const char* b) const
    {
        if (a == b)
            return true;
        if (!a || !b)
            return false;
        return strcmp(a, b) == 0;
    }
};

#include "CHashKey.h"

template <>
struct DefaultHashFunctor<int32_t>
{
    unsigned int operator()(int32_t key) const
    {
        return Mix32HashFunctor()(static_cast<uint32_t>(key));
    }
};

template <>
struct DefaultHashFunctor<uint32_t>
{
    unsigned int operator()(uint32_t key) const
    {
        return Mix32HashFunctor()(key);
    }
};

template <>
struct DefaultHashFunctor<int64_t>
{
    unsigned int operator()(int64_t key) const
    {
        return Mix64HashFunctor()(static_cast<uint64_t>(key));
    }
};

template <>
struct DefaultHashFunctor<uint64_t>
{
    unsigned int operator()(uint64_t key) const
    {
        return Mix64HashFunctor()(key);
    }
};

template <>
struct DefaultHashFunctor<const char*>
{
    unsigned int operator()(const char* key) const
    {
        return StringHashFunctor()(key);
    }
};

template <class T>
struct DefaultHashFunctor<T*>
{
    unsigned int operator()(T* key) const
    {
        return PointerHashFunctor()(key);
    }
};

template <>
struct DefaultHashFunctor<CUtlStringToken>
{
    unsigned int operator()(const CUtlStringToken& key) const
    {
        return key.GetHashCode();
    }
};

#endif
