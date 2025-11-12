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

#ifndef MS_ROOT_ADDRESS_H
#define MS_ROOT_ADDRESS_H

#include "definitions.h"

#include <cstdint>

struct CSosFieldData;
struct CNetworkStateChangedInfo;
class CModule;

namespace modules
{

inline CModule* engine;
inline CModule* tier0;
inline CModule* server;
inline CModule* schemas;
inline CModule* resource;
inline CModule* vscript;
inline CModule* vphysics2;
inline CModule* sound;
inline CModule* network;
inline CModule* worldrenderer;
inline CModule* matchmaking;
inline CModule* filesystem;
inline CModule* steamsockets;
inline CModule* materialsystem2;
inline CModule* animationsystem;

} // namespace modules

class CBaseEntity;
class CBaseAnimGraph;
class CEntityIdentity;
class CEntityInstance;
class CAttributeList;
class CBasePlayerController;
class CCSPlayer_ItemServices;
class CCSPlayer_WeaponServices;
class CEconItemView;
class CBaseWeapon;
enum class GearSlot_t;
class CGameEntitySystem;
class IEntityListener;
class BaseConVar;
class CBasePlayerPawn;
class CCSPlayerController;
class IGameRules;
enum HudPrint_t : uint32_t;
enum CStrikeTeam_t : uint8_t;
class Vector;
class IRecipientFilter;
class CSingleRecipientFilter;
class QAngle;
class CBaseModelEntity;
class Variant_t;
class CRay_t;
class CTraceFilter;
class CGameTrace;
class CGamePhysicsQueryInterface;
class CConVarBaseData;
class CCSPlayerPawn;
class ISoundOpSystem;
class SoundOpGameSystem;
class CCollisionProperty;
class CTakeDamageInfo;
struct SoundEventGuid_t;
struct SndOpEventGuid_t;
struct matrix3x4_t;
class CEntityKeyValues;
class HSCRIPT;
class CEffectData;
struct bbox_t;
class CCSWeaponBaseVData;
struct HSequence;
struct CResourceNameTyped;
struct LoadResourceOutput;
class IGameEventListener2;
struct ResourceFileHeader_t;
struct CResourceBlockInfo;
class CUtlSymbolLarge;

namespace address
{

bool Initialize();

namespace engine
{
inline void* Source2_Init;
} // namespace engine

namespace server
{

// 一般来说，最好是函数先定义好类型然后再声明
// 不然的话东西多起来就是噩梦
using NetworkStateChanged_t                         = void (*)(void* chainEntity, CNetworkStateChangedInfo& info);
using StateChanged_t                                = void (*)(void* networkTransmitComponent, CEntityInstance* ent, int64_t offset, int16_t a4, int32_t a5);
using CAttributeList_SetOrAddAttributeValueByName_t = void (*)(CAttributeList*, const char* pszAttribDefName, float flValue);
using CEntityIdentity_SetEntityName_t               = void (*)(CEntityIdentity*, const char* pszName);
using CreateEntityByName_t                          = CBaseEntity* (*)(const char* className, int iForceIndex);
using CBaseEntity_SetMoveType_t                     = void (*)(CBaseEntity*, uint8_t, uint8_t);
using CBaseEntity_SetGravityScale_t                 = void (*)(CBaseEntity*, float scale);
using CBaseEntity_ApplyAbsVelocityImpulse_t         = void (*)(CBaseEntity*, Vector*);
using CBaseEntity_AbsAngles_t                       = const QAngle& (*)(CBaseEntity*);
using CBaseEntity_SetAbsAngles_t                    = void (*)(CBaseEntity*, float pitch, float yaw, float roll);
using CBaseEntity_AbsOrigin_t                       = const Vector& (*)(CBaseEntity*);
using CBaseEntity_SetAbsOrigin_t                    = void (*)(CBaseEntity*, Vector*);
#ifdef PLATFORM_WINDOWS
using CBaseEntity_LocalVelocity_t = void (*)(CBaseEntity*, Vector*);
using CBaseEntity_AbsVelocity_t   = void (*)(CBaseEntity*, Vector*);
#else
using CBaseEntity_AbsVelocity_t   = Vector (*)(CBaseEntity*);
using CBaseEntity_LocalVelocity_t = Vector (*)(CBaseEntity*);
#endif
using CBaseEntity_SetAbsVelocity_t                        = void (*)(CBaseEntity*, Vector*);
using CBaseEntity_AcceptInput_t                           = bool (*)(CBaseEntity*, const char*, CBaseEntity*, CBaseEntity*, Variant_t&, int, void*);
using CBaseEntity_DispatchSpawn_t                         = void (*)(CBaseEntity*, CEntityKeyValues*);
using CBaseEntity_SetGroundEntity_t                       = void (*)(CBaseEntity*, CBaseEntity*, void*);
using CBaseEntity_EmitSoundFilter_t                       = void (*)(CBaseEntity*, SndOpEventGuid_t*, IRecipientFilter* filter, const char*, float, Vector*);
using CBaseEntity_StopSound_t                             = void (*)(CBaseEntity*, const char*);
using CBaseEntity_DispatchTraceAttack_t                   = int64_t (*)(CBaseEntity*, const CTakeDamageInfo*, void*);
using CBasePlayerController_SwitchSteam_t                 = void (*)(CBasePlayerController*, CStrikeTeam_t team);
using CBasePlayerController_CheckPawn_t                   = void (*)(CBasePlayerController*);
using CBasePlayerController_SetPawn_t                     = void (*)(CBasePlayerController*, CCSPlayerPawn* pawn, bool unknown1, bool unknown2, bool unknown3, bool unknown4);
using PlayerPawnItemServices_GiveNamedItem_t              = CBaseWeapon* (*)(CCSPlayer_ItemServices*, const char* classname, int64_t iSubType, CEconItemView* pScriptItem, bool bForce, void* vector);
using PlayerPawnWeaponServices_RemovePlayerItem_t         = void (*)(CCSPlayer_WeaponServices*, CBaseWeapon* weapon);
using PlayerPawnWeaponServices_GetWeaponBySlot_t          = CBaseWeapon* (*)(CCSPlayer_WeaponServices*, GearSlot_t slot, int position);
using PlayerPawnWeaponServices_DetachWeapon_t             = bool (*)(CCSPlayer_WeaponServices*, CBaseWeapon* weapon);
using CBasePlayerPawn_FindMatchingWeaponsForTeamLoadout_t = void (*)(void* loadout, void* team, void* result);
using ServiceGiveGlove_t                                  = void (*)(void* service, void* player, void* item, void* result);
using UTIL_SetModel_t                                     = void (*)(CBaseEntity*, const char*);
using UTIL_RadioMessage_t                                 = void (*)(IRecipientFilter*, int, int, const char*, const char*, const char*, const char*, const char*);
using UTIL_DispatchEffect_t                               = void (*)(const char*, CEffectData*);
#ifdef PLATFORM_WINDOWS
using UTIL_DispatchEffectFilter_t = void (*)(IRecipientFilter*, float, const char*, CEffectData*);
#else
using UTIL_DispatchEffectFilter_t = void (*)(IRecipientFilter*, const char*, CEffectData*);
#endif
#ifdef PLATFORM_WINDOWS
using UTIL_DispatchParticleEffectFilterPosition_t = int32_t (*)(const char*, Vector*, Vector*, CBaseEntity*, bool, int, IRecipientFilter*, bool);
#else
using UTIL_DispatchParticleEffectFilterPosition_t = int32_t (*)(const char*,
                                                                CBaseEntity*,
                                                                bool,
                                                                int,
                                                                IRecipientFilter*,
                                                                bool,
                                                                double /*origin_xy*/,
                                                                float /*origin_z*/,
                                                                double /*angle_xy*/,
                                                                float /*angle_z*/);
#endif
using UTIL_DispatchParticleEffectFilterAttachment_t    = int32_t (*)(const char*, uint32_t, CBaseEntity*, uint8_t, uint32_t, bool, int, IRecipientFilter*, bool);
using CCSGameRules_TerminateRound_t                    = void (*)(IGameRules*, float, uint32_t, void*, uint32_t);
using CCSGameRules_PlayerCanHearChat_t                 = bool (*)(IGameRules*, CCSPlayerController*, CCSPlayerController*, bool);
using CGameEntitySystem_FindEntityByIndex_t            = CBaseEntity* (*)(CGameEntitySystem*, int32_t index);
using CGameEntitySystem_AllocPooledString_t            = void* (*)(CUtlSymbolLarge*, const char*);
using CGameEntitySystem_FindByClassname_t              = CBaseEntity* (*)(CGameEntitySystem*, CBaseEntity* startEntity, const char* classname);
using CGameEntitySystem_FindByName_t                   = CBaseEntity* (*)(CGameEntitySystem*, CBaseEntity* startEntity, const char* name, void*, void*, void*, void*);
using CGameEntitySystem_FindInSphere_t                 = CBaseEntity* (*)(CGameEntitySystem*, CBaseEntity* startEntity, Vector* vector, float radius);
using CGameEntitySystem_SpawnEntityFromKeyValuesSync_t = int32_t (*)(const char* classname, void* kv);
using CGameEntitySystem_AddListenerEntity_t            = void (*)(CGameEntitySystem*, IEntityListener* listener);
using CGameEntitySystem_RemoveListenerEntity_t         = void (*)(CGameEntitySystem*, IEntityListener* listener);
using CGameEntitySystem_AddEntityIOEvent_t             = void (*)(CGameEntitySystem*, CBaseEntity*, const char*, CBaseEntity*, CBaseEntity*, Variant_t*, float, int, void*, void*);
using ScriptRegisterConVar_t                           = void* (*)(void*, const char*, const char*, const char*, int);
using ScriptRegisterConCommand_t                       = void (*)(void*, const char*, void*, const char*, int);
using ScriptSetConVarString_t                          = void (*)(BaseConVar* convar, int64_t, const char*);
using ScriptSetConVarNumber_t                          = void (*)(BaseConVar* convar, int64_t value, int64_t value2);
#ifdef PLATFORM_WINDOWS
using ScriptSetConVarDouble_t = void (*)(BaseConVar* convar, float value);
#else
using ScriptSetConVarDouble_t = void (*)(BaseConVar* convar, int64_t unknown, float value);
#endif
using CEntityInstance_Kill_t                          = void (*)(CEntityInstance*);
using CEntityInstance_GetEntityIndex_t                = int64_t (*)(CEntityInstance*, int32_t*);
using CEntityInstance_GetRefEHandle_t                 = int64_t (*)(CEntityInstance*, uint32_t*);
using CEntityInstance_GetOrCreatePublicScriptScope_t  = HSCRIPT* (*)(CEntityInstance*);
using CEntityInstance_GetOrCreatePrivateScriptScope_t = HSCRIPT* (*)(CEntityInstance*);
using GetEconItemSchema_t                             = void* (*)();
using CBaseModelEntity_SetBodyGroupByName_t           = void (*)(CBaseModelEntity*, const char*, int32_t);
using CBaseModelEntity_SetMaterialGroupMask_t         = void (*)(CBaseModelEntity*, uint64_t);
using CBaseModelEntity_LookupBone_t                   = int32_t (*)(CBaseModelEntity*, const char*);
using CBaseModelEntity_GetBoneTransform_t             = void (*)(CBaseModelEntity*, int32_t, matrix3x4_t*);
using CBaseModelEntity_SetModelScale_t                = void (*)(CBaseModelEntity*, float);
using CBaseModelEntity_SetCollisionBounds_t           = void (*)(CBaseModelEntity*, const Vector*, const Vector*);
using CPaintKit_UsesLegacyModel_t                     = bool (*)(const char*);
using CGamePhysicsQueryInterface_TraceShape_t         = bool (*)(CGamePhysicsQueryInterface*, void* ray, Vector* start, Vector* end, CTraceFilter* filter, CGameTrace* trace);
#ifdef PLATFORM_WINDOWS
using StudioModel_LookupAttachment_t = int32_t (*)(void*, int32_t*, const char*);
#else
using StudioModel_LookupAttachment_t = int32_t (*)(void*, const char*);
#endif
using StudioModel_GetAttachment_t = void (*)(void*, int32_t, Vector*, Vector*);
#ifdef PLATFORM_WINDOWS
using SoundOpGameSystem_SetSoundEventParamString_t = void (*)(SoundOpGameSystem*, const IRecipientFilter*, const SndOpEventGuid_t&, const char*, CSosFieldData*, int16_t, bool);
#else
using SoundOpGameSystem_SetSoundEventParamString_t = void (*)(SoundOpGameSystem*, const IRecipientFilter*, const char*, CSosFieldData*, int16_t, bool, SndOpEventGuid_t, int32_t);
#endif
using SoundOpGameSystem_StopSoundEvent_t       = void (*)(SoundOpGameSystem*, SndOpEventGuid_t*);
using SoundOpGameSystem_StopSoundEventFilter_t = void (*)(SoundOpGameSystem*, IRecipientFilter*, SndOpEventGuid_t*);
using CCollisionProperty_SetSolid_t            = void (*)(CCollisionProperty*, uint8_t);

using CCSPlayer_MovementServices_TracePlayerBBox_t = void (*)(const Vector& start, const Vector& end, const bbox_t& bounds, CTraceFilter* filter, CGameTrace& trace);
using CTraceFilterPlayerMovementCS_ctor_t          = void (*)(CTraceFilter& filter, CEntityInstance* handleEntity, uint64_t interactWith, uint8_t collisionGroup);
using FindWeaponVDataByName_t                      = CCSWeaponBaseVData* (*)(int state, const char* weaponName);
using GetLegacyGameEventListener_t                 = IGameEventListener2* (*)(int32_t);
using CGameEntitySystem_GetSpawnOriginOffset_t     = matrix3x4_t& (*)(const CGameEntitySystem*);

inline NetworkStateChanged_t                               NetworkStateChanged;
inline StateChanged_t                                      StateChanged;
inline CAttributeList_SetOrAddAttributeValueByName_t       CAttributeList_SetOrAddAttributeValueByName;
inline CEntityIdentity_SetEntityName_t                     CEntityIdentity_SetEntityName;
inline CreateEntityByName_t                                CreateEntityByName;
inline CBaseEntity_SetMoveType_t                           CBaseEntity_SetMoveType;
inline CBaseEntity_SetGravityScale_t                       CBaseEntity_SetGravityScale;
inline CBaseEntity_ApplyAbsVelocityImpulse_t               CBaseEntity_ApplyAbsVelocityImpulse;
inline CBaseEntity_AbsAngles_t                             CBaseEntity_AbsAngles;
inline CBaseEntity_SetAbsAngles_t                          CBaseEntity_SetAbsAngles;
inline CBaseEntity_AbsOrigin_t                             CBaseEntity_AbsOrigin;
inline CBaseEntity_SetAbsOrigin_t                          CBaseEntity_SetAbsOrigin;
inline CBaseEntity_LocalVelocity_t                         CBaseEntity_LocalVelocity;
inline CBaseEntity_AbsVelocity_t                           CBaseEntity_AbsVelocity;
inline CBaseEntity_SetAbsVelocity_t                        CBaseEntity_SetAbsVelocity;
inline CBaseEntity_AcceptInput_t                           CBaseEntity_AcceptInput;
inline CBaseEntity_DispatchSpawn_t                         CBaseEntity_DispatchSpawn;
inline CBaseEntity_EmitSoundFilter_t                       CBaseEntity_EmitSoundFilter;
inline CBaseEntity_SetGroundEntity_t                       CBaseEntity_SetGroundEntity;
inline CBaseEntity_StopSound_t                             CBaseEntity_StopSound;
inline CBaseEntity_DispatchTraceAttack_t                   CBaseEntity_DispatchTraceAttack;
inline CBasePlayerController_SwitchSteam_t                 CBasePlayerController_SwitchSteam;
inline CBasePlayerController_SetPawn_t                     CBasePlayerController_SetPawn;
inline CBasePlayerController_CheckPawn_t                   CBasePlayerController_CheckPawn;
inline PlayerPawnItemServices_GiveNamedItem_t              PlayerPawnItemServices_GiveNamedItem;
inline PlayerPawnWeaponServices_RemovePlayerItem_t         PlayerPawnWeaponServices_RemovePlayerItem;
inline PlayerPawnWeaponServices_GetWeaponBySlot_t          PlayerPawnWeaponServices_GetWeaponBySlot;
inline PlayerPawnWeaponServices_DetachWeapon_t             PlayerPawnWeaponServices_DetachWeapon;
inline CBasePlayerPawn_FindMatchingWeaponsForTeamLoadout_t CBasePlayerPawn_FindMatchingWeaponsForTeamLoadout;
inline ServiceGiveGlove_t                                  ServiceGiveGlove;
inline UTIL_SetModel_t                                     UTIL_SetModel;
inline UTIL_RadioMessage_t                                 UTIL_RadioMessage;
inline UTIL_DispatchEffect_t                               UTIL_DispatchEffect;
inline UTIL_DispatchEffectFilter_t                         UTIL_DispatchEffectFilter;
inline UTIL_DispatchParticleEffectFilterPosition_t         UTIL_DispatchParticleEffectFilterPosition;
inline UTIL_DispatchParticleEffectFilterAttachment_t       UTIL_DispatchParticleEffectFilterAttachment;
inline CCSGameRules_TerminateRound_t                       CCSGameRules_TerminateRound;
inline CCSGameRules_PlayerCanHearChat_t                    CCSGameRules_PlayerCanHearChat;
inline CGameEntitySystem_FindEntityByIndex_t               CGameEntitySystem_FindEntityByIndex;
inline CGameEntitySystem_AllocPooledString_t               CGameEntitySystem_AllocPooledString;
inline CGameEntitySystem_FindByClassname_t                 CGameEntitySystem_FindByClassname;
inline CGameEntitySystem_FindByName_t                      CGameEntitySystem_FindByName;
inline CGameEntitySystem_FindInSphere_t                    CGameEntitySystem_FindInSphere;
inline CGameEntitySystem_SpawnEntityFromKeyValuesSync_t    CGameEntitySystem_SpawnEntityFromKeyValuesSync;
inline CGameEntitySystem_AddListenerEntity_t               CGameEntitySystem_AddListenerEntity;
inline CGameEntitySystem_RemoveListenerEntity_t            CGameEntitySystem_RemoveListenerEntity;
inline CGameEntitySystem_AddEntityIOEvent_t                CGameEntitySystem_AddEntityIOEvent;
inline void*                                               gConvarAccessorInclude;
inline ScriptRegisterConCommand_t                          ScriptRegisterConCommand;
inline ScriptSetConVarString_t                             ScriptSetConVarString;
inline ScriptSetConVarNumber_t                             ScriptSetConVarNumber;
inline ScriptSetConVarDouble_t                             ScriptSetConVarDouble;
inline CEntityInstance_Kill_t                              CEntityInstance_Kill;
inline CEntityInstance_GetEntityIndex_t                    CEntityInstance_GetEntityIndex;
inline CEntityInstance_GetRefEHandle_t                     CEntityInstance_GetRefEHandle;
inline CEntityInstance_GetOrCreatePublicScriptScope_t      CEntityInstance_GetOrCreatePublicScriptScope;
inline CEntityInstance_GetOrCreatePrivateScriptScope_t     CEntityInstance_GetOrCreatePrivateScriptScope;
inline GetEconItemSchema_t                                 GetEconItemSchema;
inline CBaseModelEntity_SetBodyGroupByName_t               CBaseModelEntity_SetBodyGroupByName;
inline CBaseModelEntity_SetMaterialGroupMask_t             CBaseModelEntity_SetMaterialGroupMask;
inline CBaseModelEntity_LookupBone_t                       CBaseModelEntity_LookupBone;
inline CBaseModelEntity_GetBoneTransform_t                 CBaseModelEntity_GetBoneTransform;
inline CBaseModelEntity_SetModelScale_t                    CBaseModelEntity_SetModelScale;
inline CBaseModelEntity_SetCollisionBounds_t               CBaseModelEntity_SetCollisionBounds;
inline CPaintKit_UsesLegacyModel_t                         CPaintKit_UsesLegacyModel;
inline CGamePhysicsQueryInterface_TraceShape_t             CGamePhysicsQueryInterface_TraceShape;
inline StudioModel_LookupAttachment_t                      StudioModel_LookupAttachment;
inline StudioModel_GetAttachment_t                         StudioModel_GetAttachment;
inline SoundOpGameSystem_SetSoundEventParamString_t        SoundOpGameSystem_SetSoundEventParamString;
inline SoundOpGameSystem_StopSoundEvent_t                  SoundOpGameSystem_StopSoundEvent;
inline SoundOpGameSystem_StopSoundEventFilter_t            SoundOpGameSystem_StopSoundEventFilter;
inline CCollisionProperty_SetSolid_t                       CCollisionProperty_SetSolid;
inline CCSPlayer_MovementServices_TracePlayerBBox_t        CCSPlayer_MovementServices_TracePlayerBBox;
inline CTraceFilterPlayerMovementCS_ctor_t                 CTraceFilterPlayerMovementCS_ctor;
inline FindWeaponVDataByName_t                             FindWeaponVDataByName;
inline GetLegacyGameEventListener_t                        GetLegacyGameEventListener;
inline CGameEntitySystem_GetSpawnOriginOffset_t            CGameEntitySystem_GetSpawnOriginOffset;
} // namespace server

namespace resource
{
using CResourceNameTyped_ResolveResourceName_t = bool (*)(CResourceNameTyped& info, const char* fileName);
inline CResourceNameTyped_ResolveResourceName_t CResourceNameTyped_ResolveResourceName;
} // namespace resource

} // namespace address

#endif