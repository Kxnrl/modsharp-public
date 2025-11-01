// This file modified from https://github.com/alliedmodders/hl2sdk/tree/cs2

#ifndef CSTRIKE_TYPE_CUTLBUFFER_H
#define CSTRIKE_TYPE_CUTLBUFFER_H

#include "cstrike/type/CUtlLeanVector.h"

class CByteswap
{
    unsigned int m_bSwapBytes : 1;
    unsigned int m_bBigEndian : 1;
};

class CBufferString;

class CUtlBuffer
{
public:
    using UtlBufferOverflowFunc_t = bool (CUtlBuffer::*)(int nSize);

    enum BufferFlags_t : uint8_t
    {
        NONE               = 0,
        TEXT_BUFFER        = 1 << 0,
        EXTERNAL_GROWABLE  = 1 << 1,
        CONTAINS_CRLF      = 1 << 2,
        READ_ONLY          = 1 << 3,
        AUTO_TABS_DISABLED = 1 << 4,
    };

    MS_CLASS_IMPORT CUtlBuffer(int growSize = 0, int initSize = 0, BufferFlags_t nFlags = NONE);
    MS_CLASS_IMPORT CUtlBuffer(const void* pBuffer, int size, BufferFlags_t nFlags = NONE);

    CUtlBuffer(const CUtlBuffer&)            = delete;
    CUtlBuffer& operator=(const CUtlBuffer&) = delete;

    MS_CLASS_IMPORT void GetString(char* pString, int nMaxChars = 0);
    MS_CLASS_IMPORT const void* PeekGet(int nMaxSize, int nOffset);

    int32_t TellPut() const
    {
        return m_Put;
    }

    CUtlLeanVector<uint8_t> m_Memory;
    int32_t                 m_Get;
    int32_t                 m_Put;

    uint8_t m_Error;
    uint8_t m_Flags;
    uint8_t m_Reserved;

    int32_t m_nTab;
    int32_t m_nMaxPut;
    int32_t m_nOffset;

    UtlBufferOverflowFunc_t m_GetOverflowFunc;
    UtlBufferOverflowFunc_t m_PutOverflowFunc;

    CByteswap m_Byteswap;
};

#endif
