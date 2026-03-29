// This file modified from https://github.com/alliedmodders/hl2sdk/tree/cs2

#ifndef CSTRIKE_TYPE_GAMETRACE_H
#define CSTRIKE_TYPE_GAMETRACE_H

#include "cstrike/type/CTransform.h"

#include <cstdint>

class IPhysicsBody;
class IPhysicsShape;
class CBaseEntity;

using HPhysicsBody  = IPhysicsBody*;
using HPhysicsShape = IPhysicsShape*;

enum RayType_t : uint8_t
{
    RAY_TYPE_LINE = 0,
    RAY_TYPE_SPHERE,
    RAY_TYPE_HULL,
    RAY_TYPE_CAPSULE,
    RAY_TYPE_MESH,
};

enum CollisionFunctionMask_t
{
    FCOLLISION_FUNC_ENABLE_SOLID_CONTACT   = (1 << 0),
    FCOLLISION_FUNC_ENABLE_TRACE_QUERY     = (1 << 1),
    FCOLLISION_FUNC_ENABLE_TOUCH_EVENT     = (1 << 2),
    FCOLLISION_FUNC_ENABLE_SELF_COLLISIONS = (1 << 3),
    FCOLLISION_FUNC_IGNORE_FOR_HITBOX_TEST = (1 << 4),
    FCOLLISION_FUNC_ENABLE_TOUCH_PERSISTS  = (1 << 5),
};

// these are on by default
#define FCOLLISION_FUNC_DEFAULT (FCOLLISION_FUNC_ENABLE_SOLID_CONTACT | FCOLLISION_FUNC_ENABLE_TRACE_QUERY | FCOLLISION_FUNC_ENABLE_TOUCH_EVENT)

enum RnQueryObjectFlags : uint8_t
{
    //  RNQUERY_OBJECTS_STATIC (0x1) is used in
    //      CLightQueryGameSystem::OnPostSimulate
    //      ray test by loading rays.bin
    //      CBaePlayerPawn::IsPlayerAimingAtTarget
    //      CBaseRagdoll related
    //      CSoundOpvarSetAutoRoomEntity::Spawn
    //      CNavSpaceBuildTreeJob

    // RNQUERY_OBJECTS_STATIC | RNQUERY_OBJECTS_KEYFRAMED (0x3) is used in
    //      when decoy hit entity isn't player pawn

    //  RNQUERY_OBJECTS_KEYFRAMED | RNQUERY_OBJECTS_DYNAMIC (0x6) is used in
    //      checking if a grenade projectile has hit any player
    //      filtering bullet trace result
    //      checking if hit entity is a *_door_rotating
    //      checking if player is aiming at a CPointCommentaryNode

    // RNQUERY_OBJECTS_LOCATABLE (0x8) is used in
    //      checking if a CSmokeGrenadeProjectile is exploded from inferno
    //      CGamePhysicsQueryInterface::EntitiesAlong* to add g_LocatableEntities to results

    //  RNQUERY_OBJECTS_KEYFRAMED | RNQUERY_OBJECTS_DYNAMIC | RNQUERY_OBJECTS_LOCATABLE (0xF) is used in
    //      CBotBreakableEnumerator
    //      CInferno::Think damage
    //      CBombTarget
    //      UTIL_EntitiesAlongRay
    //      CEnvShake::ApplyShake with type rumble
    //      CBaseFilter Think

    // RNQUERY_OBJECTS_STATIC | RNQUERY_OBJECTS_KEYFRAMED | RNQUERY_OBJECTS_DYNAMIC | RNQUERY_OBJECTS_LOCATABLE (0xF) is used in pretty much every trace filter

    RNQUERY_OBJECTS_STATIC    = (1 << 0), // static body
    RNQUERY_OBJECTS_KEYFRAMED = (1 << 1), // keyframed body
    RNQUERY_OBJECTS_DYNAMIC   = (1 << 2), // dynamic body
    RNQUERY_OBJECTS_LOCATABLE = (1 << 3),

    RNQUERY_OBJECTS_ALL_GAME_ENTITIES = RNQUERY_OBJECTS_KEYFRAMED | RNQUERY_OBJECTS_DYNAMIC | RNQUERY_OBJECTS_LOCATABLE,
    RNQUERY_OBJECTS_ALL               = RNQUERY_OBJECTS_STATIC | RNQUERY_OBJECTS_ALL_GAME_ENTITIES,
};

enum HitGroup_t : uint32_t
{
    HITGROUP_INVALID = ~0u,
    HITGROUP_GENERIC = 0,
    HITGROUP_HEAD,
    HITGROUP_CHEST,
    HITGROUP_STOMACH,
    HITGROUP_LEFTARM,
    HITGROUP_RIGHTARM,
    HITGROUP_LEFTLEG,
    HITGROUP_RIGHTLEG,
    HITGROUP_NECK,
    HITGROUP_UNUSED,
    HITGROUP_GEAR,
    HITGROUP_SPECIAL,
    HITGROUP_COUNT,
};

enum HitBoxShapeType_t : uint8_t
{
    HITBOX_SHAPE_HULL = 0,
    HITBOX_SHAPE_SPHERE,
    HITBOX_SHAPE_CAPSULE,
};

// https://github.com/alliedmodders/hl2sdk/blob/cs2/public/gametrace.h
class CPhysSurfacePropertiesPhysics;
class CPhysSurfacePropertiesSoundNames;
class CPhysSurfacePropertiesAudio;

class CPhysSurfaceProp
{
public:
    CPhysSurfaceProp() :
        m_pName(nullptr), m_nNameHash(0), m_nBaseNameHash(0), m_nListIndex(0), m_nBaseListIndex(0), m_bHidden(false), m_pDescription(nullptr)
    {
    }

public:
    const char* m_pName;
    uint32_t    m_nNameHash;
    uint32_t    m_nBaseNameHash;
    int32_t     m_nListIndex;
    int32_t     m_nBaseListIndex;
    bool        m_bHidden;
    const char* m_pDescription;
    /*
    CPhysSurfacePropertiesPhysics m_physics;
    CPhysSurfacePropertiesSoundNames m_audioSounds;
    CPhysSurfacePropertiesAudio m_audioParams;
    */
};

class ShapeRay_t
{
    ShapeRay_t() { Init(Vector(0.0f, 0.0f, 0.0f)); }
    ShapeRay_t(const Vector& vStartOffset) { Init(vStartOffset); }
    ShapeRay_t(const Vector& vCenter, float flRadius) { Init(vCenter, flRadius); }
    ShapeRay_t(const Vector& vMins, const Vector& vMaxs) { Init(vMins, vMaxs); }
    ShapeRay_t(const Vector& vCenterA, const Vector& vCenterB, float flRadius) { Init(vCenterA, vCenterB, flRadius); }
    ShapeRay_t(const Vector& vMins, const Vector& vMaxs, const Vector* pVertices, int nNumVertices) { Init(vMins, vMaxs, pVertices, nNumVertices); }

    void Init(const Vector& vStartOffset)
    {
        m_Line.m_vStartOffset = vStartOffset;
        m_Line.m_flRadius     = 0.0f;
        m_eType               = RAY_TYPE_LINE;
    }

    void Init(const Vector& vCenter, float flRadius)
    {
        if (flRadius > 0.0f)
        {
            m_Sphere.m_vCenter  = vCenter;
            m_Sphere.m_flRadius = flRadius;
            m_eType             = RAY_TYPE_SPHERE;
        }
        else
        {
            Init(vCenter);
        }
    }

    void Init(const Vector& vMins, const Vector& vMaxs)
    {
        if (vMins != vMaxs)
        {
            m_Hull.m_vMins = vMins;
            m_Hull.m_vMaxs = vMaxs;
            m_eType        = RAY_TYPE_HULL;
        }
        else
        {
            Init(vMins);
        }
    }

    void Init(const Vector& vCenterA, const Vector& vCenterB, float flRadius)
    {
        if (vCenterA != vCenterB)
        {
            if (flRadius > 0.0f)
            {
                m_Capsule.m_vCenter[0] = vCenterA;
                m_Capsule.m_vCenter[1] = vCenterB;
                m_Capsule.m_flRadius   = flRadius;
                m_eType                = RAY_TYPE_CAPSULE;
            }
            else
            {
                Init(vCenterA, vCenterB);
            }
        }
        else
        {
            Init(vCenterA, flRadius);
        }
    }

    void Init(const Vector& vMins, const Vector& vMaxs, const Vector* pVertices, int nNumVertices)
    {
        m_Mesh.m_vMins        = vMins;
        m_Mesh.m_vMaxs        = vMaxs;
        m_Mesh.m_pVertices    = pVertices;
        m_Mesh.m_nNumVertices = nNumVertices;
        m_eType               = RAY_TYPE_MESH;
    }

    struct Line_t
    {
        Vector m_vStartOffset;
        float  m_flRadius;
    };
    static_assert(sizeof(Line_t) == 16);

    struct Sphere_t
    {
        Vector m_vCenter;
        float  m_flRadius;
    };
    static_assert(sizeof(Sphere_t) == 16);

    struct Hull_t
    {
        Vector m_vMins;
        Vector m_vMaxs;
    };
    static_assert(sizeof(Hull_t) == 24);

    struct Capsule_t
    {
        Vector m_vCenter[2];
        float  m_flRadius;
    };
    static_assert(sizeof(Capsule_t) == 28);

    struct Mesh_t
    {
        Vector        m_vMins;
        Vector        m_vMaxs;
        const Vector* m_pVertices;
        int           m_nNumVertices;
    };
    static_assert(sizeof(Mesh_t) == 40);
#ifdef PLATFORM_WINDOWS
    static_assert(offsetof(Mesh_t, m_pVertices) == 24);
    static_assert(offsetof(Mesh_t, m_nNumVertices) == 32);
#endif

    union {
        Line_t    m_Line;
        Sphere_t  m_Sphere;
        Hull_t    m_Hull;
        Capsule_t m_Capsule;
        Mesh_t    m_Mesh;
    };

public:
    RayType_t m_eType;
};
static_assert(sizeof(ShapeRay_t) == 48);
#ifdef PLATFORM_WINDOWS
static_assert(offsetof(ShapeRay_t, m_eType) == 40);
#endif

class CRay_t
{
public:
    Vector  m_vecStart;
    Vector  m_vecEnd;
    Vector  m_vecMins;
    Vector  m_vecMaxs;
    int32_t m_nMasks; // Flags?
    uint8_t m_Unknown;

public:
    CRay_t()
    {
        m_vecStart.Init();
        m_vecEnd.Init();
        m_vecMins.Init();
        m_vecMaxs.Init();
        m_nMasks  = 0;
        m_Unknown = 0;
    }
};
static_assert(sizeof(CRay_t) == 56);

class CHitBoxData
{
public:
    const char*       m_pName;            // 0
    const char*       m_pSurfaceProperty; // 8
    const char*       m_pBoneName;        // 16
    Vector            m_vMinBounds;       // 24
    Vector            m_vMaxBounds;       // 36
    float             m_flShapeRadius;    // 48
    uint32_t          m_nBoneNameHash;    // 52
    HitGroup_t        m_nHitGroup;        // 56
    HitBoxShapeType_t m_nShapeType;       // 60
    bool              m_bTranslationOnly; // 61
    uint32_t          m_CRC;              // 64
    uint32_t          m_cRenderColor;     // 68
    uint16_t          m_nHitBoxId;        // 72
    bool              m_bForcedTransform; // 74
    CTransform        m_forcedTransform;  // 80

public:
    [[nodiscard]] uint32_t GetHitGroup() const { return m_nHitGroup; }
    [[nodiscard]] uint32_t GetHitBoxId() const { return m_nHitBoxId; }
};
static_assert(sizeof(CHitBoxData) == 112);
#ifdef PLATFORM_WINDOWS
static_assert(offsetof(CHitBoxData, m_nShapeType) == 60);
static_assert(offsetof(CHitBoxData, m_bTranslationOnly) == 61);
static_assert(offsetof(CHitBoxData, m_CRC) == 64);
static_assert(offsetof(CHitBoxData, m_cRenderColor) == 68);
static_assert(offsetof(CHitBoxData, m_nHitBoxId) == 72);
static_assert(offsetof(CHitBoxData, m_bForcedTransform) == 74);
static_assert(offsetof(CHitBoxData, m_forcedTransform) == 80);
#endif

struct RnCollisionAttr_t
{
private:
    uint64_t m_nInteractsAs;
    uint64_t m_nInteractsWith;
    uint64_t m_nInteractsExclude;
    uint32_t m_nEntityId;
    uint32_t m_nOwnerId;
    uint16_t m_nHierarchyId;
    uint8_t  m_nCollisionGroup;
    uint8_t  m_nCollisionFunctionMask;

public:
    RnCollisionAttr_t()
    {
        m_nInteractsAs           = 0;
        m_nInteractsWith         = 0;
        m_nInteractsExclude      = 0;
        m_nEntityId              = 0;
        m_nOwnerId               = 0xFFFFFFFF;
        m_nHierarchyId           = 0;
        m_nCollisionGroup        = 0;
        m_nCollisionFunctionMask = FCOLLISION_FUNC_DEFAULT;
    }

    bool IsSolidContactEnabled() const { return (m_nCollisionFunctionMask & FCOLLISION_FUNC_ENABLE_SOLID_CONTACT) != 0; }
    bool IsTraceAndQueryEnabled() const { return (m_nCollisionFunctionMask & FCOLLISION_FUNC_ENABLE_TRACE_QUERY) != 0; }
    bool IsTouchEventEnabled() const { return (m_nCollisionFunctionMask & FCOLLISION_FUNC_ENABLE_TOUCH_EVENT) != 0; }
    bool IsSelfCollisionsEnabled() const { return (m_nCollisionFunctionMask & FCOLLISION_FUNC_ENABLE_SELF_COLLISIONS) != 0; }
    bool ShouldIgnoreForHitboxTest() const { return (m_nCollisionFunctionMask & FCOLLISION_FUNC_IGNORE_FOR_HITBOX_TEST) != 0; }
    bool IsTouchPersistsEnabled() const { return (m_nCollisionFunctionMask & FCOLLISION_FUNC_ENABLE_TOUCH_PERSISTS) != 0; }

    bool HasInteractsAsLayer(int nLayerIndex) const { return (m_nInteractsAs & (1ull << nLayerIndex)) != 0; }
    bool HasInteractsWithLayer(int nLayerIndex) const { return (m_nInteractsWith & (1ull << nLayerIndex)) != 0; }
    bool HasInteractsExcludeLayer(int nLayerIndex) const { return (m_nInteractsExclude & (1ull << nLayerIndex)) != 0; }
    void EnableInteractsAsLayer(int nLayer) { m_nInteractsAs |= (1ull << nLayer); }
    void EnableInteractsWithLayer(int nLayer) { m_nInteractsWith |= (1ull << nLayer); }
    void EnableInteractsExcludeLayer(int nLayer) { m_nInteractsExclude |= (1ull << nLayer); }
    void DisableInteractsAsLayer(int nLayer) { m_nInteractsAs &= ~(1ull << nLayer); }
    void DisableInteractsWithLayer(int nLayer) { m_nInteractsWith &= ~(1ull << nLayer); }
    void DisableInteractsExcludeLayer(int nLayer) { m_nInteractsExclude &= ~(1ull << nLayer); }
};
static_assert(sizeof(RnCollisionAttr_t) == 40);

class /*alignas(16)*/ CGameTrace
{
public:
    CPhysSurfaceProp* m_pSurface;         //  0
    CBaseEntity*      m_pHitEntity;       //  8
    CHitBoxData*      m_pHitBoxData;      // 16
    void*             m_pPhysicsBody;     // 24
    void*             m_pPhysicsShape;    // 32
    uint32_t          m_nContents;        // 40
    CTransform        m_BodyTransform;    // 44
    RnCollisionAttr_t m_ShapeAttributes;  // 80
    Vector            m_vStartPos;        // 120
    Vector            m_vEndPos;          // 132
    Vector            m_vPlaneNormal;     // 144
    Vector            m_vHitPoint;        // 156
    float             m_flHitOffset;      // 168
    float             m_flFraction;       // 172
    int32_t           m_nTriangle;        // 176
    int16_t           m_nHitBoxBoneIndex; // 180
    RayType_t         m_eRayType;         // 182
    bool              m_bStartInSolid;    // 184
    bool              m_bExactHitPoint;   // 188

public:
    [[nodiscard]] CBaseEntity*      GetHitEntity() const { return m_pHitEntity; }
    [[nodiscard]] CHitBoxData*      GetHitBoxData() const { return m_pHitBoxData; }
    [[nodiscard]] CPhysSurfaceProp* GetSurfaceProp() const { return m_pSurface; }
    [[nodiscard]] void*             GetPhysicsBody() const { return m_pPhysicsBody; }
    [[nodiscard]] void*             GetPhysicsShape() const { return m_pPhysicsShape; }
    [[nodiscard]] uint32_t          GetContents() const { return m_nContents; }
    [[nodiscard]] const Vector&     GetStartPosition() const { return m_vStartPos; }
    [[nodiscard]] const Vector&     GetEndPosition() const { return m_vEndPos; }
    [[nodiscard]] const Vector&     GetPlaneNormal() const { return m_vPlaneNormal; }
    [[nodiscard]] const Vector&     GetHitPoint() const { return m_vHitPoint; }
    [[nodiscard]] float             GetHitOffset() const { return m_flHitOffset; }
    [[nodiscard]] float             GetFraction() const { return m_flFraction; }
    [[nodiscard]] RayType_t         GetRayType() const { return m_eRayType; }
    [[nodiscard]] int32_t           GetTriangle() const { return m_nTriangle; }
    [[nodiscard]] bool              IsStartInSolid() const { return m_bStartInSolid; }
    [[nodiscard]] bool              DidHit() const { return m_flFraction < 1.0f || m_bStartInSolid; }
    [[nodiscard]] int16_t           GetHitBoxBoneIndex() const { return m_nHitBoxBoneIndex; }

    CGameTrace()
    {
        Init();
    }

    void Init()
    {
        m_pSurface      = nullptr;
        m_pHitEntity    = nullptr;
        m_pHitBoxData   = nullptr;
        m_pPhysicsBody  = nullptr;
        m_pPhysicsShape = nullptr;
        m_nContents     = 0;
        m_BodyTransform.SetToIdentity();
        m_vPlaneNormal.Init();
        m_vHitPoint.Init();
        m_flHitOffset      = 0.0f;
        m_flFraction       = 1.0f;
        m_nTriangle        = -1;
        m_nHitBoxBoneIndex = -1;
        m_eRayType         = RAY_TYPE_LINE;
        m_bStartInSolid    = false;
        m_bExactHitPoint   = false;
    }
};
static_assert(sizeof(CGameTrace) == 192);
#ifdef PLATFORM_WINDOWS
static_assert(offsetof(CGameTrace, m_vStartPos) == 120);
static_assert(offsetof(CGameTrace, m_vEndPos) == 132);
static_assert(offsetof(CGameTrace, m_flFraction) == 172);
static_assert(offsetof(CGameTrace, m_eRayType) == 182);
static_assert(offsetof(CGameTrace, m_bStartInSolid) == 183);
static_assert(offsetof(CGameTrace, m_bExactHitPoint) == 184);
#endif // PLATFORM_WINDOWS

struct RnQueryShapeAttr_t
{
public:
    RnQueryShapeAttr_t()
    {
        m_nInteractsWith    = 0;
        m_nInteractsExclude = 0;
        m_nInteractsAs      = 0;

        m_nEntityIdsToIgnore[0] = CBaseHandle();
        m_nEntityIdsToIgnore[1] = CBaseHandle();

        m_nOwnerIdsToIgnore[0] = CBaseHandle();
        m_nOwnerIdsToIgnore[1] = CBaseHandle();

        m_nHierarchyIds[0] = 0;
        m_nHierarchyIds[1] = 0;

        m_nObjectSetMask  = RNQUERY_OBJECTS_ALL;
        m_nCollisionGroup = 0;

        m_bHitSolid                         = true;
        m_bHitSolidRequiresGenerateContacts = false;
        m_bHitTrigger                       = false;
        m_bShouldIgnoreDisabledPairs        = true;
        m_bIgnoreIfBothInteractWithHitBoxes = false;
        m_bForceHitEverything               = false;
        m_bUnknown                          = true;

        m_nIncludedDetailLayers = 0xFFFF;
    }

    bool HasInteractsAsLayer(int nLayerIndex) const { return (m_nInteractsAs & (1ull << nLayerIndex)) != 0; }
    bool HasInteractsWithLayer(int nLayerIndex) const { return (m_nInteractsWith & (1ull << nLayerIndex)) != 0; }
    bool HasInteractsExcludeLayer(int nLayerIndex) const { return (m_nInteractsExclude & (1ull << nLayerIndex)) != 0; }
    void EnableInteractsAsLayer(int nLayer) { m_nInteractsAs |= (1ull << nLayer); }
    void EnableInteractsWithLayer(int nLayer) { m_nInteractsWith |= (1ull << nLayer); }
    void EnableInteractsExcludeLayer(int nLayer) { m_nInteractsExclude |= (1ull << nLayer); }
    void DisableInteractsAsLayer(int nLayer) { m_nInteractsAs &= ~(1ull << nLayer); }
    void DisableInteractsWithLayer(int nLayer) { m_nInteractsWith &= ~(1ull << nLayer); }
    void DisableInteractsExcludeLayer(int nLayer) { m_nInteractsExclude &= ~(1ull << nLayer); }

public:
    // Which interaction layers do I interact or collide with? (e.g. I collide with LAYER_INDEX_CONTENTS_PASS_BULLETS because I am not a bullet)
    // This is analogous to the "solid mask" or "trace mask" in source 1 (bit mask of CONTENTS_* or 1<<LAYER_INDEX_*)
    uint64_t m_nInteractsWith;

    // Which interaction layers do I _not_ interact or collide with?  If my exclusion layers match m_nInteractsAs on the other object then no interaction happens.
    uint64_t m_nInteractsExclude;

    // Which interaction layers do I represent? (e.g. I am a LAYER_INDEX_CONTENTS_PLAYER_CLIP volume)
    // This is analogous to "contents" in source 1  (bit mask of CONTENTS_* or 1<<LAYER_INDEX_*)
    uint64_t m_nInteractsAs;

    CBaseHandle        m_nEntityIdsToIgnore[2]; // this is the ID of the game entity which should be ignored
    CBaseHandle        m_nOwnerIdsToIgnore[2];  // this is the ID of the owner of the game entity which should be ignored
    uint16_t           m_nHierarchyIds[2];      // this is an ID for the hierarchy of game entities (used to disable collision among objects in a hierarchy)
    uint16_t           m_nIncludedDetailLayers = 0xFFFF;
    uint8_t            m_nTargetDetailLayer;
    RnQueryObjectFlags m_nObjectSetMask;  // set of RnQueryObjectSet bits
    uint8_t            m_nCollisionGroup; // one of the registered collision groups

    bool m_bHitSolid : 1;                         // if true, then query will hit solid
    bool m_bHitSolidRequiresGenerateContacts : 1; // if true, the FCOLLISION_FUNC_ENABLE_SOLID_CONTACT flag will be checked to hit solid
    bool m_bHitTrigger : 1;                       // if true, then query will hit tirgger
    bool m_bShouldIgnoreDisabledPairs : 1;        // if true, then ignores if the query and shape entity IDs are in collision pairs
    bool m_bIgnoreIfBothInteractWithHitBoxes : 1; // if true, then ignores if both query and shape interact with LAYER_INDEX_CONTENTS_HITBOX
    bool m_bForceHitEverything : 1;               // if true, will hit any objects without any conditions
    bool m_bUnknown : 1;                          // haven't found where this is used yet
};

struct RnQueryAttr_t : public RnQueryShapeAttr_t
{
};

#endif