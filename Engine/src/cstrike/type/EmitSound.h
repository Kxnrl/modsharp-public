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

#ifndef CSTRIKE_TYPE_EMITSOUND_H
#define CSTRIKE_TYPE_EMITSOUND_H

#include "cstrike/type/CUtlLeanVector.h"
#include "cstrike/type/Vector.h"
#include "cstrike/type/Vector2D.h"

#include <cstdint>

constexpr int PITCH_NORM = 100; // non-pitch shifted
constexpr int PITCH_LOW  = 95;  // other values are possible - 0-255, where 255 is very high
constexpr int PITCH_HIGH = 120;

constexpr char SE_PARAM_NAME_VOLUME[]   = "volume";
constexpr char SE_PARAM_NAME_POSITION[] = "position";

struct SoundEventGuid_t
{
    constexpr SoundEventGuid_t() :
        m_Guid(0)
    {
    }

    constexpr SoundEventGuid_t(const SoundEventGuid_t& other) = default;

    explicit constexpr SoundEventGuid_t(const uint32_t id) :
        m_Guid(id) {}

    [[nodiscard]] constexpr bool     IsValid() const { return m_Guid != 0 && m_Guid != 0xFFFFFFFF; }
    [[nodiscard]] constexpr uint32_t GetGuid() const { return m_Guid; }

    operator uint32_t() const
    {
        return m_Guid;
    }

private:
    uint32_t m_Guid;
};

struct SndOpEventGuid_t
{
    constexpr SndOpEventGuid_t() :
        m_nGuid(0),
        m_hStackHash(-1)
    {
    }

    explicit constexpr SndOpEventGuid_t(uint64_t value)
    {
        m_nGuid      = SoundEventGuid_t(static_cast<const uint32_t>(value & 0xFFFFFFFF));
        m_hStackHash = static_cast<int32_t>(((value >> 32) & 0xFFFFFFFF));
    }

    constexpr SndOpEventGuid_t(const SndOpEventGuid_t& other) = default;

    operator uint64_t() const
    {
        return *reinterpret_cast<const uint64_t*>(this);
    }

    operator int64_t() const
    {
        return *reinterpret_cast<const int64_t*>(this);
    }

    [[nodiscard]] constexpr bool IsValid() const
    {
        return m_nGuid.IsValid() && m_hStackHash != -1;
    }

    [[nodiscard]] constexpr SoundEventGuid_t GetGuid() const
    {
        return m_nGuid;
    }

    [[nodiscard]] constexpr int32_t GetStackHash() const
    {
        return m_hStackHash;
    }

private:
    SoundEventGuid_t m_nGuid;
    int32_t          m_hStackHash;
};

static_assert(sizeof(SndOpEventGuid_t) == 8);

constexpr uint64_t                InvalidSndOpEventGuid = 0xFFFFFFFF00000000ull;
inline constinit SndOpEventGuid_t g_DefaultSndOpEventGuid(InvalidSndOpEventGuid);

#pragma pack(push, 1)
struct StartSoundEventInfo_t
{
    SndOpEventGuid_t m_nSndOpEventGuid = g_DefaultSndOpEventGuid;
    int32_t          m_nFlags{};
    uint64_t         m_nRecipients{};

    constexpr StartSoundEventInfo_t() :
        m_nSndOpEventGuid(InvalidSndOpEventGuid),
        m_nFlags(0),
        m_nRecipients(0ull)
    {
    }

    constexpr StartSoundEventInfo_t(const SndOpEventGuid_t& guid, int32_t flags, uint64_t recipients) :
        m_nSndOpEventGuid(guid),
        m_nFlags(flags),
        m_nRecipients(recipients)
    {
    }
};
#pragma pack(pop)
static_assert(sizeof(StartSoundEventInfo_t) == 20);

inline constinit StartSoundEventInfo_t g_DefaultStartSoundEventInfo{};

enum class SoundChannel_t : int32_t
{
    REPLACE    = -1, /**< Unknown */
    AUTO       = 0,  /**< Auto */
    WEAPON     = 1,  /**< Weapons */
    VOICE      = 2,  /**< Voices */
    ITEM       = 3,  /**< Items */
    BODY       = 4,  /**< Player? */
    STREAM     = 5,  /**< "Stream channel from the static or dynamic area" */
    STATIC     = 6,  /**< "Stream channel from the static area" */
    VOICE_BASE = 7,  /**< "Channel for network voice data" */
    USER_BASE  = 135 /**< Anything >= this is allocated to game code */
};

enum class SoundGender_t : uint8_t
{
    NONE             = 0x0,
    MALE             = 0x1,
    FEMALE           = 0x2,
    NAMVET           = 0x3,
    TEENGIRL         = 0x4,
    BIKER            = 0x5,
    MANAGER          = 0x6,
    GAMBLER          = 0x7,
    PRODUCER         = 0x8,
    COACH            = 0x9,
    MECHANIC         = 0xA,
    CEDA             = 0xB,
    CRAWLER          = 0xC,
    UNDISTRACTABLE   = 0xD,
    FALLEN           = 0xE,
    RIOT_CONTROL     = 0xF,
    CLOWN            = 0x10,
    JIMMY            = 0x11,
    HOSPITAL_PATIENT = 0x12,
    BRIDE            = 0x13,
    LAST             = 0x14,
};

enum class SoundLevel_t : uint32_t
{
    SoundLevel_None    = 0x0,
    SoundLevel_20dB    = 0x14,
    SoundLevel_25dB    = 0x19,
    SoundLevel_30dB    = 0x1e,
    SoundLevel_35dB    = 0x23,
    SoundLevel_40dB    = 0x28,
    SoundLevel_45dB    = 0x2d,
    SoundLevel_50dB    = 0x32,
    SoundLevel_55dB    = 0x37,
    SoundLevel_IDLE    = 0x3c,
    SoundLevel_60dB    = 0x3c,
    SoundLevel_65dB    = 0x41,
    SoundLevel_STATIC  = 0x42,
    SoundLevel_70dB    = 0x46,
    SoundLevel_NORM    = 0x4b,
    SoundLevel_75dB    = 0x4b,
    SoundLevel_80dB    = 0x50,
    SoundLevel_TALKING = 0x50,
    SoundLevel_85dB    = 0x55,
    SoundLevel_90dB    = 0x5a,
    SoundLevel_95dB    = 0x5f,
    SoundLevel_100dB   = 0x64,
    SoundLevel_105dB   = 0x69,
    SoundLevel_110dB   = 0x6e,
    SoundLevel_120dB   = 0x78,
    SoundLevel_130dB   = 0x82,
    SoundLevel_GUNFIRE = 0x8c,
    SoundLevel_140dB   = 0x8c,
    SoundLevel_150dB   = 0x96,
    SoundLevel_180dB   = 0xb4,
};

enum class SoundFlags_t
{
    SoundFlags_NOFLAGS         = 0,        // to keep the compiler happy
    SoundFlags_CHANGE_VOL      = (1 << 0), // change sound vol
    SoundFlags_CHANGE_PITCH    = (1 << 1), // change sound pitch
    SoundFlags_STOP            = (1 << 2), // stop the sound
    SoundFlags_SPAWNING        = (1 << 3), // we're spawning, used in some cases for ambients not sent over net, only a param between dll and server.
    SoundFlags_DELAY           = (1 << 4), // sound has an initial delay
    SoundFlags_STOP_LOOPING    = (1 << 5), // stop all looping sounds on the entity.
    SoundFlags_SPEAKER         = (1 << 6), // being played again by a microphone through a speaker
    SoundFlags_SHOULDPAUSE     = (1 << 7), // this sound should be paused if the game is paused
    SoundFlags_IGNORE_PHONEMES = (1 << 8),
    SoundFlags_IGNORE_NAME     = (1 << 9), // used to change all sounds emitted by an entity, regardless of scriptname
};

struct EmitSound_t
{
    const char* m_pSoundName;    // 0x0
    Vector      m_veSoundOrigin; // 0x8
    float       m_flVolume;      // 0x14
    float       m_flSoundTime;   // 0x18
private:
    [[maybe_unused]] uint8_t m_pad_0x1c[0x4];

public:
    uint32_t m_nForceGuid; // 0x20
private:
    [[maybe_unused]] uint8_t m_pad_0x24[0x4];

public:
    int16_t m_nPitch; // 0x28
    uint8_t m_nFlags; // 0x2a
}; // Size: 0x2b
static_assert(sizeof(EmitSound_t) == 48);

enum SOFieldDataType_t : std::int8_t
{
    SOFTYPE_NONE               = 0,
    SOFTYPE_BOOL               = 1,
    SOFTYPE_INT                = 2,
    SOFTYPE_UINT32             = 3,
    SOFTYPE_UINT64             = 4,
    SOFTYPE_VSND               = 5,
    SOFTYPE_TOKEN              = 6,
    SOFTYPE_ENUM               = 7,
    SOFTYPE_FLOAT              = 8,
    SOFTYPE_FLOAT_2            = 9,
    SOFTYPE_FLOAT_3            = 10,
    SOFTYPE_FLOAT_4            = 11,
    SOFTYPE_FLOAT_6            = 12,
    SOFTYPE_FLOAT_8            = 13,
    SOFTYPE_FLOAT_12           = 14,
    SOFTYPE_FLOAT_MAX_SPEAKERS = 15,
    SOFTYPE_STRING_HANDLE      = 16,
    SOFTYPE_FLOAT_16           = 17,
    SOFTYPE_DSP_PRESET         = 18,
    SOFTYPE_SNDEVT             = 19,
    SOFTYPE_MIXGROUP           = 20,
    SOFTYPE_MAX                = 21,
};

enum SOAtmoicDataType_t : std::int32_t
{
    SOATYPE_INVALID = 0,
    SOATYPE_NUMERIC = 1,
    SOATYPE_HANDLE  = 2,
    SOATYPE_INTEGER = 3,
    SOATYPE_UINT32  = 4,
    SOATYPE_UINT64  = 5,
    SOATYPE_VSND    = 6,
    SOATYPE_TOKEN   = 7,
};

struct CSosFieldData
{
    SOAtmoicDataType_t m_nAtmoicDataType{};
    SOFieldDataType_t  m_nFieldDataType{};
    int32_t            m_nDataSize{};
    int32_t            m_nAllocSize{};
};

struct CSosFieldDataBool final : CSosFieldData
{
    explicit CSosFieldDataBool(bool value)
    {
        m_nAtmoicDataType = SOATYPE_NUMERIC;
        m_nFieldDataType  = SOFTYPE_BOOL;
        m_nAllocSize      = 4;
        m_nDataSize       = 4;
        m_bValue          = value;
    }

private:
    bool m_bValue;
};

struct CSosFieldDataFloat final : CSosFieldData
{
    explicit CSosFieldDataFloat(float value)
    {
        m_nAtmoicDataType = SOATYPE_NUMERIC;
        m_nFieldDataType  = SOFTYPE_FLOAT;
        m_nAllocSize      = sizeof(float);
        m_nDataSize       = sizeof(float);
        m_flValue         = value;
    }

private:
    float m_flValue;
};

struct CSosFieldDataFloat2 final : CSosFieldData
{
    explicit CSosFieldDataFloat2(const Vector2D& value)
    {
        m_nAtmoicDataType = SOATYPE_NUMERIC;
        m_nFieldDataType  = SOFTYPE_FLOAT_2;

        m_nAllocSize = sizeof(Vector2D);
        m_nDataSize  = sizeof(Vector2D);

        m_vecValue = value;
    }

private:
    Vector2D m_vecValue;
};

struct CSosFieldDataFloat3 final : CSosFieldData
{
    explicit CSosFieldDataFloat3(const Vector& value) noexcept
    {
        m_nAtmoicDataType = SOATYPE_NUMERIC;
        m_nFieldDataType  = SOFTYPE_FLOAT_3;

        m_nAllocSize = sizeof(Vector);
        m_nDataSize  = sizeof(Vector);

        m_pValue  = static_cast<Vector*>(AllocateMemory(m_nAllocSize));
        *m_pValue = value;
    }

    ~CSosFieldDataFloat3()
    {
        FreeMemory(m_pValue);
    }

    CSosFieldDataFloat3(const CSosFieldDataFloat3&)            = delete;
    CSosFieldDataFloat3& operator=(const CSosFieldDataFloat3&) = delete;

    CSosFieldDataFloat3(CSosFieldDataFloat3&& other) noexcept
        :
        m_pValue(other.m_pValue)
    {
        other.m_pValue = nullptr;
    }

    CSosFieldDataFloat3& operator=(CSosFieldDataFloat3&& other) noexcept
    {
        if (this != &other)
        {
            FreeMemory(m_pValue);
            m_pValue       = other.m_pValue;
            other.m_pValue = nullptr;
        }
        return *this;
    }

private:
    Vector* m_pValue;
};

struct CSosFieldDataFloat4 final : CSosFieldData
{
    explicit CSosFieldDataFloat4(float* data)
    {
        m_nAtmoicDataType = SOATYPE_NUMERIC;
        m_nFieldDataType  = SOFTYPE_FLOAT_4;

        m_nAllocSize = sizeof(float) * 4;
        m_nDataSize  = sizeof(float) * 4;

        m_pValue = static_cast<float*>(AllocateMemory(m_nAllocSize));
        memcpy(m_pValue, data, m_nAllocSize);
    }

    ~CSosFieldDataFloat4()
    {
        FreeMemory(m_pValue);
    }

private:
    float* m_pValue;
};

struct CSosFieldDataInt final : CSosFieldData
{
    explicit CSosFieldDataInt(int value)
    {
        m_nAtmoicDataType = SOATYPE_INTEGER;
        m_nFieldDataType  = SOFTYPE_INT;

        m_nDataSize  = 4;
        m_nAllocSize = 4;

        m_iValue = value;
    }

private:
    int m_iValue;
};

struct CSosFieldDataUInt32 final : CSosFieldData
{
    explicit CSosFieldDataUInt32(uint32_t value)
    {
        m_nAtmoicDataType = SOATYPE_UINT32;
        m_nFieldDataType  = SOFTYPE_UINT32;

        m_nDataSize  = 4;
        m_nAllocSize = 4;

        m_nValue = value;
    }

private:
    uint32_t m_nValue;
};

struct CSosFieldDataUInt64 final : CSosFieldData
{
    explicit CSosFieldDataUInt64(uint64_t value)
    {
        m_nAtmoicDataType = SOATYPE_UINT64;
        m_nFieldDataType  = SOFTYPE_UINT64;

        m_nDataSize  = 8;
        m_nAllocSize = 8;

        m_nValue = value;
    }

private:
    uint64_t m_nValue;
};

struct CSosFieldDataToken final : CSosFieldData
{
    explicit CSosFieldDataToken(uint32_t value)
    {
        m_nAtmoicDataType = SOATYPE_TOKEN;
        m_nFieldDataType  = SOFTYPE_TOKEN;
        m_nAllocSize      = 4;
        m_nDataSize       = 4;

        m_nStringToken = value;
    }

private:
    uint32_t m_nStringToken;
};

struct CSosFieldDataString final : CSosFieldData
{
    explicit CSosFieldDataString(uint16_t handle)
    {
        m_nAtmoicDataType = SOATYPE_HANDLE;
        m_nFieldDataType  = SOFTYPE_STRING_HANDLE;
        m_nAllocSize      = 2;
        m_nDataSize       = 2;

        m_nStringHandle = handle;
    }

private:
    uint16_t m_nStringHandle;
};

struct CSosFieldDataSoundEvent final : CSosFieldData
{
    explicit CSosFieldDataSoundEvent(uint16_t handle)
    {
        m_nAtmoicDataType = SOATYPE_HANDLE;
        m_nFieldDataType  = SOFTYPE_SNDEVT;
        m_nAllocSize      = 2;
        m_nDataSize       = 2;

        m_nStringHandle = handle;
    }

private:
    uint16_t m_nStringHandle;
};

struct CSosFieldDataVsnd final : CSosFieldData
{
    explicit CSosFieldDataVsnd(uint32_t value)
    {
        m_nAtmoicDataType = SOATYPE_VSND;
        m_nFieldDataType  = SOFTYPE_VSND;

        m_nAllocSize = 4;
        m_nDataSize  = 4;

        m_nValue = value;
    }

private:
    uint32_t m_nValue;
};

struct CSoundEventDefinitionStaticFields
{
    uint16_t m_nStringTableIndex;   // 0x0
    uint16_t m_nBaseDataSize;       // 0x2
    uint16_t m_nMemoryOffset : 11;  // 0x4
    uint16_t m_nFieldTypeIndex : 5; // 0x4
private:
    uint16_t m_nStringHandle;
};

struct CSoundEventDefinitionInstanceFields
{
    uint32_t m_nParamHash; // 0x0
    int      m_Size;       // 0x4
    int      m_nIndex;     // 0x8
}; // Size: 0xc
static_assert(sizeof(CSoundEventDefinitionInstanceFields) == 0xC);

/*
#pragma pack(push, 1)
struct CSoundEvent
{
    uint32_t m_nGuid;          // 0x0
    uint32_t m_nEventBaseHash; // 0x4
    uint8_t* m_pSoundData;     // 0x8

    int     m_nStaticGroupCount;    // 0x10
    uint8_t m_nStaticGroupIndex[4]; // 0x14

    CUtlVector<std::uint32_t> m_vecSoundHash; // 0x18
    uint32_t                  m_nStackHash;   // 0x30

    uint32_t                                        m_nInstanceFieldAllocSize; // 0x34
    CUtlVector<CSoundEventDefinitionInstanceFields> m_vInstanceParams;         // 0x38

    CSoundEventDefinitionInstanceFields*                                             m_pInstanceFields; // 0x50
    CUtlVector<CUtlKeyValuePair<CUtlStringToken, CSoundEventDefinitionStaticFields>> m_vStaticParam;    // 0x58
private:
    char pad_70[0x28];

public:
    int m_nStaticFieldAllocSize; // 0x98
private:
    char pad_9c[0x4];

public:
    CSoundEventDefinitionStaticFields* m_pStaticFields; // 0xa0
    float                              m_flDuration;    // 0xa8
private:
    uint8_t m_nMemoryType{}; // 0xac
    char    pad_ad[0x3];
    uint8_t m_nPreloadVsnds{}; // 0xb0
    char    pad_b1[0xf];

public:
}; // Size: 0xc0
static_assert(sizeof(CSoundEvent) == 0xc0);
#pragma pack(pop)
*/

#endif