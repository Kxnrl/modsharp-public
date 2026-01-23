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

#ifndef CSTRIKE_TYPE_TRACE_H
#define CSTRIKE_TYPE_TRACE_H

#include "cstrike/cstrike.h"
#include "cstrike/entity/CBaseEntity.h"
#include "cstrike/entity/CBaseModelEntity.h"
#include "cstrike/type/CGameTrace.h"
#include "cstrike/type/Vector.h"

#include <cstdint>

class CTraceFilter : public RnQueryAttr_t
{
public:
    bool m_bIterateEntities;

    virtual ~CTraceFilter() {}
    virtual bool ShouldHit(CBaseEntity* pHitEntity) { return true; }

    // 避免其他继承问题
    explicit CTraceFilter()
    {
        m_nInteractsWith   = 0;
        m_nCollisionGroup  = 0;
        m_bIterateEntities = false;
    }

    CTraceFilter(uint64_t nInteractsWith, uint8_t nCollisionGroup)
    {
        m_nInteractsWith   = nInteractsWith;
        m_nCollisionGroup  = nCollisionGroup;
        m_bIterateEntities = false;
    }

    CTraceFilter(uint64_t nInteractsWith, uint8_t nCollisionGroup, RnQueryObjectFlags flags) :
        CTraceFilter(nInteractsWith, nCollisionGroup)
    {
        m_nObjectSetMask = flags;
    }

    void SetSkip1(CBaseModelEntity* pEntity)
    {
        m_nEntityIdsToIgnore[0] = pEntity->GetActualEHandle();

        const auto& m_hOwner = pEntity->m_hOwnerEntity();
        if (m_hOwner.IsValid())
            m_nOwnerIdsToIgnore[0] = m_hOwner;

        m_nHierarchyIds[0] = pEntity->m_Collision().m_collisionAttribute().m_nHierarchyId();
    }

    void SetSkip2(CBaseModelEntity* pEntity)
    {
        m_nEntityIdsToIgnore[1] = pEntity->GetActualEHandle();

        const auto& m_hOwner = pEntity->m_hOwnerEntity();
        if (m_hOwner.IsValid())
            m_nOwnerIdsToIgnore[1] = m_hOwner;

        m_nHierarchyIds[1] = pEntity->m_Collision().m_collisionAttribute().m_nHierarchyId();
    }

    void SetExcludeLayers(uint64_t nInteractsExclude)
    {
        m_nInteractsExclude = nInteractsExclude;
    }
};
#ifdef PLATFORM_WINDOWS
static_assert(sizeof(CTraceFilter) == 72);
#endif

class CTraceFilterIgnoreEntities : public CTraceFilter
{
protected:
    CBaseModelEntity* m_pIgnoreEntities[2];

public:
    explicit CTraceFilterIgnoreEntities(uint64_t nInteractsWith, uint8_t nCollisionGroup, RnQueryObjectFlags flags) :
        CTraceFilter(nInteractsWith, nCollisionGroup, flags)
    {
        m_bIterateEntities = true;

        m_pIgnoreEntities[0] = nullptr;
        m_pIgnoreEntities[1] = nullptr;
    }

    void SetIgnoreEntities(CBaseModelEntity* skip1, CBaseModelEntity* skip2)
    {
        if (skip1)
        {
            m_pIgnoreEntities[0] = skip1;
            SetSkip1(skip1);
        }

        if (skip2)
        {
            m_pIgnoreEntities[1] = skip2;
            SetSkip2(skip2);
        }
    }

    bool ShouldHit(CBaseEntity* pHitEntity) override
    {
        if (m_pIgnoreEntities[0] && pHitEntity == m_pIgnoreEntities[0])
            return false;

        if (m_pIgnoreEntities[1] && pHitEntity == m_pIgnoreEntities[1])
            return false;

        return true;
    }
};

class CTraceFilterIgnorePlayers : public CTraceFilterIgnoreEntities
{
public:
    explicit CTraceFilterIgnorePlayers(uint64_t nInteractsWith, uint8_t nCollisionGroup, RnQueryObjectFlags flags) :
        CTraceFilterIgnoreEntities(nInteractsWith, nCollisionGroup, flags)
    {
    }

    bool ShouldHit(CBaseEntity* pHitEntity) override
    {
        if (m_pIgnoreEntities[0] && pHitEntity == m_pIgnoreEntities[0])
            return false;

        if (m_pIgnoreEntities[1] && pHitEntity == m_pIgnoreEntities[1])
            return false;

        if (pHitEntity->IsPlayerPawn())
            return false;

        return true;
    }
};

class CTraceFilterPlayerMovementCS : public CTraceFilter
{
};

class CGamePhysicsQueryInterface
{
public:
    bool TraceLine(CRay_t* ray, Vector* start, Vector* end, CTraceFilter* filter, CGameTrace* trace)
    {
        return address::server::CGamePhysicsQueryInterface_TraceShape(this, ray, start, end, filter, trace);
    }
    bool TraceShape(ShapeRay_t* ray, Vector* start, Vector* end, CTraceFilter* filter, CGameTrace* trace)
    {
        return address::server::CGamePhysicsQueryInterface_TraceShape(this, ray, start, end, filter, trace);
    }
};

struct CTraceResult_t
{
    CBaseEntity*      m_pHitEntity;       // 0
    float             m_flFraction;       // 8
    RayType_t         m_eRayType;         // 12
    bool              m_bStartInSolid;    // 13
    int32_t           m_nTriangle;        // 16
    Vector            m_vStartPos;        // 20
    Vector            m_vEndPos;          // 32
    Vector            m_vPlaneNormal;     // 44
    CPhysSurfaceProp* m_pSurface;         // 56
    CHitBoxData*      m_pHitBoxData;      // 64
    void*             m_pPhysicsBody;     // 72
    void*             m_pPhysicsShape;    // 80
    Vector            m_vHitPoint;        // 88
    float             m_flHitOffset;      // 100
    uint32_t          m_nContents;        // 104
    int16_t           m_nHitBoxBoneIndex; // 108

    CTraceResult_t() :
        m_pHitEntity(nullptr),
        m_flFraction(0),
        m_eRayType(RAY_TYPE_LINE),
        m_bStartInSolid(false),
        m_nTriangle(0),
        m_pSurface(nullptr),
        m_pHitBoxData(nullptr),
        m_pPhysicsBody(nullptr),
        m_pPhysicsShape(nullptr),
        m_flHitOffset(0),
        m_nContents(0),
        m_nHitBoxBoneIndex(0)
    {
        m_vStartPos.Init();
        m_vEndPos.Init();
        m_vPlaneNormal.Init();
        m_vHitPoint.Init();
    }

    void Init(const CGameTrace& trace)
    {
        m_pHitEntity       = trace.GetHitEntity();
        m_flFraction       = trace.GetFraction();
        m_eRayType         = trace.GetRayType();
        m_bStartInSolid    = trace.IsStartInSolid();
        m_nTriangle        = trace.GetTriangle();
        m_vStartPos        = trace.GetStartPosition();
        m_vEndPos          = trace.GetEndPosition();
        m_vPlaneNormal     = trace.GetPlaneNormal();
        m_pSurface         = trace.GetSurfaceProp();
        m_pHitBoxData      = trace.GetHitBoxData();
        m_pPhysicsBody     = trace.GetPhysicsBody();
        m_pPhysicsShape    = trace.GetPhysicsShape();
        m_vHitPoint        = trace.GetHitPoint();
        m_flHitOffset      = trace.GetHitOffset();
        m_nContents        = trace.GetContents();
        m_nHitBoxBoneIndex = trace.GetHitBoxBoneIndex();
    }
};
static_assert(sizeof(CTraceResult_t) == 112);
#ifdef PLATFORM_WINDOWS
static_assert(offsetof(CTraceResult_t, m_flFraction) == 8);
static_assert(offsetof(CTraceResult_t, m_eRayType) == 12);
static_assert(offsetof(CTraceResult_t, m_bStartInSolid) == 13);
static_assert(offsetof(CTraceResult_t, m_nTriangle) == 16);
static_assert(offsetof(CTraceResult_t, m_vStartPos) == 20);
static_assert(offsetof(CTraceResult_t, m_vEndPos) == 32);
static_assert(offsetof(CTraceResult_t, m_vPlaneNormal) == 44);
static_assert(offsetof(CTraceResult_t, m_pSurface) == 56);
static_assert(offsetof(CTraceResult_t, m_pHitBoxData) == 64);
static_assert(offsetof(CTraceResult_t, m_pPhysicsBody) == 72);
static_assert(offsetof(CTraceResult_t, m_pPhysicsShape) == 80);
static_assert(offsetof(CTraceResult_t, m_vHitPoint) == 88);
static_assert(offsetof(CTraceResult_t, m_flHitOffset) == 100);
static_assert(offsetof(CTraceResult_t, m_nContents) == 104);
static_assert(offsetof(CTraceResult_t, m_nHitBoxBoneIndex) == 108);
#endif

#endif