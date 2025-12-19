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

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sharp.Core.Bridges;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.GameEntities;
using Sharp.Core.Helpers;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;

namespace Sharp.Core.Managers;

internal interface ICorePhysicsQueryManager : IPhysicsQueryManager;

internal unsafe partial class PhysicsQueryManager : ICorePhysicsQueryManager
{
    private readonly ILogger<PhysicsQueryManager> _logger;

    public PhysicsQueryManager(ILogger<PhysicsQueryManager> logger)
    {
        _logger = logger;

        Bridges.Forwards.Game.OnRuntimeTraceFilterCallback += OnRuntimeTraceFilterCallback;
    }

#region Warppers

    public TraceResult TraceLine(Vector start,
        Vector                          end,
        InteractionLayers               mask,
        CollisionGroupType              group,
        TraceQueryFlag                  flags,
        InteractionLayers               excludeLayers = InteractionLayers.None,
        IBaseEntity?                    ignore1       = null,
        IBaseEntity?                    ignore2       = null)
    {
        var result = stackalloc TraceResultStruct[1];

        Game.TraceLine(&start,
                       &end,
                       mask,
                       group,
                       flags,
                       excludeLayers,
                       ignore1?.GetAbsPtr() ?? nint.Zero,
                       ignore2?.GetAbsPtr() ?? nint.Zero,
                       false,
                       result);

        return new TraceResult(BaseEntity.Create(result->HitEntity),
                               result->Fraction,
                               result->RayType,
                               result->StartInSolid,
                               result->Triangle,
                               result->StartPosition,
                               result->EndPosition,
                               result->PlaneNormal,
                               result->SurfaceProp,
                               result->HitBoxData,
                               result->PhysicsBody,
                               result->PhysicsShape,
                               result->HitPoint,
                               result->HitOffset,
                               result->Contents,
                               result->HitGroupHitBoxBoneIndex);
    }

    public TraceResult TraceLineNoPlayers(Vector start,
        Vector                                   end,
        InteractionLayers                        mask,
        CollisionGroupType                       group,
        TraceQueryFlag                           flags,
        InteractionLayers                        excludeLayers = InteractionLayers.None,
        IBaseEntity?                             ignore1       = null,
        IBaseEntity?                             ignore2       = null)
    {
        var result = stackalloc TraceResultStruct[1];

        Game.TraceLine(&start,
                       &end,
                       mask,
                       group,
                       flags,
                       excludeLayers,
                       ignore1?.GetAbsPtr() ?? nint.Zero,
                       ignore2?.GetAbsPtr() ?? nint.Zero,
                       true,
                       result);

        return new TraceResult(BaseEntity.Create(result->HitEntity),
                               result->Fraction,
                               result->RayType,
                               result->StartInSolid,
                               result->Triangle,
                               result->StartPosition,
                               result->EndPosition,
                               result->PlaneNormal,
                               result->SurfaceProp,
                               result->HitBoxData,
                               result->PhysicsBody,
                               result->PhysicsShape,
                               result->HitPoint,
                               result->HitOffset,
                               result->Contents,
                               result->HitGroupHitBoxBoneIndex);
    }

    public TraceResult TraceLineFilter(Vector start,
        Vector                                end,
        InteractionLayers                     mask,
        CollisionGroupType                    group,
        TraceQueryFlag                        flags,
        InteractionLayers                     excludeLayers,
        Func<IBaseEntity, bool>               filter)
    {
        _traceFilterCallback = filter;

        var result = stackalloc TraceResultStruct[1];

        Game.TraceLineFilter(&start,
                             &end,
                             mask,
                             group,
                             flags,
                             excludeLayers,
                             result);

        _traceFilterCallback = null;

        return new TraceResult(BaseEntity.Create(result->HitEntity),
                               result->Fraction,
                               result->RayType,
                               result->StartInSolid,
                               result->Triangle,
                               result->StartPosition,
                               result->EndPosition,
                               result->PlaneNormal,
                               result->SurfaceProp,
                               result->HitBoxData,
                               result->PhysicsBody,
                               result->PhysicsShape,
                               result->HitPoint,
                               result->HitOffset,
                               result->Contents,
                               result->HitGroupHitBoxBoneIndex);
    }

    public TraceResult TraceShape(TraceShapeRay ray,
        Vector                                  start,
        Vector                                  end,
        InteractionLayers                       mask,
        CollisionGroupType                      group,
        TraceQueryFlag                          flags,
        InteractionLayers                       excludeLayers = InteractionLayers.None,
        IBaseEntity?                            ignore1       = null,
        IBaseEntity?                            ignore2       = null)
    {
        var result = stackalloc TraceResultStruct[1];

        Game.TraceShape(&ray,
                        &start,
                        &end,
                        mask,
                        group,
                        flags,
                        excludeLayers,
                        ignore1?.GetAbsPtr() ?? nint.Zero,
                        ignore2?.GetAbsPtr() ?? nint.Zero,
                        false,
                        result);

        return new TraceResult(BaseEntity.Create(result->HitEntity),
                               result->Fraction,
                               result->RayType,
                               result->StartInSolid,
                               result->Triangle,
                               result->StartPosition,
                               result->EndPosition,
                               result->PlaneNormal,
                               result->SurfaceProp,
                               result->HitBoxData,
                               result->PhysicsBody,
                               result->PhysicsShape,
                               result->HitPoint,
                               result->HitOffset,
                               result->Contents,
                               result->HitGroupHitBoxBoneIndex);
    }

    public TraceResult TraceShapeNoPlayers(TraceShapeRay ray,
        Vector                                           start,
        Vector                                           end,
        InteractionLayers                                mask,
        CollisionGroupType                               group,
        TraceQueryFlag                                   flags,
        InteractionLayers                                excludeLayers = InteractionLayers.None,
        IBaseEntity?                                     ignore1       = null,
        IBaseEntity?                                     ignore2       = null)
    {
        var result = stackalloc TraceResultStruct[1];

        Game.TraceShape(&ray,
                        &start,
                        &end,
                        mask,
                        group,
                        flags,
                        excludeLayers,
                        ignore1?.GetAbsPtr() ?? nint.Zero,
                        ignore2?.GetAbsPtr() ?? nint.Zero,
                        true,
                        result);

        return new TraceResult(BaseEntity.Create(result->HitEntity),
                               result->Fraction,
                               result->RayType,
                               result->StartInSolid,
                               result->Triangle,
                               result->StartPosition,
                               result->EndPosition,
                               result->PlaneNormal,
                               result->SurfaceProp,
                               result->HitBoxData,
                               result->PhysicsBody,
                               result->PhysicsShape,
                               result->HitPoint,
                               result->HitOffset,
                               result->Contents,
                               result->HitGroupHitBoxBoneIndex);
    }

    public TraceResult TraceShapeFilter(TraceShapeRay ray,
        Vector                                        start,
        Vector                                        end,
        InteractionLayers                             mask,
        CollisionGroupType                            group,
        TraceQueryFlag                                flags,
        InteractionLayers                             excludeLayers,
        Func<IBaseEntity, bool>                       filter)
    {
        _traceFilterCallback = filter;

        var result = stackalloc TraceResultStruct[1];

        Game.TraceShapeFilter(&ray,
                              &start,
                              &end,
                              mask,
                              group,
                              flags,
                              excludeLayers,
                              result);

        _traceFilterCallback = null;

        return new TraceResult(BaseEntity.Create(result->HitEntity),
                               result->Fraction,
                               result->RayType,
                               result->StartInSolid,
                               result->Triangle,
                               result->StartPosition,
                               result->EndPosition,
                               result->PlaneNormal,
                               result->SurfaceProp,
                               result->HitBoxData,
                               result->PhysicsBody,
                               result->PhysicsShape,
                               result->HitPoint,
                               result->HitOffset,
                               result->Contents,
                               result->HitGroupHitBoxBoneIndex);
    }

    public TraceResult TraceShapePlayerMovement(TraceShapeRay ray,
        Vector                                                start,
        Vector                                                end,
        InteractionLayers                                     interactsWith,
        IPlayerPawn                                           pawn)
        => throw new NotImplementedException();

    private Func<IBaseEntity, bool>? _traceFilterCallback;

    private bool OnRuntimeTraceFilterCallback(nint ptr)
    {
        if (_traceFilterCallback is null || BaseEntity.Create(ptr) is not { } entity)
        {
            return true;
        }

        return _traceFilterCallback.Invoke(entity);
    }

#endregion

    public GameTrace TraceShape(TraceShapeRay ray, Vector start, Vector end, in RnQueryShapeAttr query, nint? callback = null)
        => TraceInternal(&ray, start, end, in query, callback);

    public GameTrace TraceLine(Vector start, Vector end, in RnQueryShapeAttr query, nint? callback = null)
    {
        var ray = stackalloc TraceShapeRay[1];

        return TraceInternal(ray, start, end, in query, callback);
    }

    public GameTrace TraceShapePlayerMovement(TraceShapeRay ray, Vector start, Vector end, in RnQueryShapeAttr query)
        => TraceInternal(&ray, start, end, in query, _vtable);

    public GameTrace TraceShapeNoPlayers(TraceShapeRay ray, Vector start, Vector end, in RnQueryShapeAttr query)
    {
        var callback = (nint) (delegate* unmanaged<CTraceFilter*, nint, bool>) (&NoPlayer_ShouldHitEntity);

        return TraceShape(ray, start, end, in query, callback);
    }

    public GameTrace TraceLineNoPlayers(Vector start, Vector end, in RnQueryShapeAttr query)
    {
        var callback = (nint) (delegate* unmanaged<CTraceFilter*, nint, bool>) (&NoPlayer_ShouldHitEntity);

        return TraceLine(start, end, in query, callback);
    }

    public int EntitiesAlongRay(TraceShapeRay ray,
        Vector                                origin,
        in RnQueryShapeAttr                   query,
        bool                                  unique,
        Span<uint>                            entities,
        uint                                  test     = 1,
        IntPtr?                               callback = null)
        => EntitiesAlongRayInternal(&ray, origin, in query, callback, unique, test, entities);
}

internal unsafe partial class PhysicsQueryManager
{
    // ReSharper disable InconsistentNaming
    private static readonly CTraceFilterVTableDescriptor* _vtable;
    private static readonly nint                          _interface;

    private static readonly delegate* unmanaged<nint, TraceShapeRay*, Vector*, Vector*, CTraceFilter*, GameTrace*, bool>
        _traceShape;

    private static readonly delegate* unmanaged<nint, TraceShapeRay*, Vector*, CTraceFilter*, uint, COverlapHitVector*,
        bool /*unique*/, int>
        _entitiesAlongRay;

    // ReSharper restore InconsistentNaming

    static PhysicsQueryManager()
    {
        _vtable = (CTraceFilterVTableDescriptor*) CoreGameData.Core.GetRequiredVTable("server", "CTraceFilterPlayerMovementCS");
        _interface = CoreGameData.Core.GetRequiredAddress("g_pPhysicsQuery");

        _traceShape
            = (delegate* unmanaged<IntPtr, TraceShapeRay*, Vector*, Vector*, CTraceFilter*, GameTrace*, bool>)
            CoreGameData.Core.GetRequiredAddress("CGamePhysicsQueryInterface::TraceShape");

        _entitiesAlongRay
            = (delegate* unmanaged<IntPtr, TraceShapeRay*, Vector*, CTraceFilter*, uint, COverlapHitVector*, bool, int>)
            CoreGameData.Core.GetRequiredAddress("CGamePhysicsQueryInterface::EntitiesAlongRay");
    }

    private static GameTrace TraceInternal(TraceShapeRay* pRay,
        Vector                                            start,
        Vector                                            end,
        in RnQueryShapeAttr                               query,
        nint?                                             callback)
    {
        if (callback is null)
        {
            return TraceInternal(pRay, start, end, in query, vtable: null);
        }

        var vtable = stackalloc CTraceFilterVTableDescriptor[1];
        vtable->ShouldHitEntity = (delegate* unmanaged<CTraceFilter*, nint, bool>) callback;

        return TraceInternal(pRay, start, end, in query, vtable);
    }

    private static GameTrace TraceInternal(TraceShapeRay* pRay,
        Vector                                            start,
        Vector                                            end,
        in RnQueryShapeAttr                               query,
        CTraceFilterVTableDescriptor*                     vtable)
    {
        var traceFilter = stackalloc CTraceFilter[1];

        if (vtable is not null)
        {
            traceFilter->Vtable             = vtable;
            traceFilter->m_bIterateEntities = true;
        }

        traceFilter->QueryAttribute = query;

        var trace = stackalloc GameTrace[1];
        _traceShape(_interface, pRay, &start, &end, traceFilter, trace);

        return *trace;
    }

    private static int EntitiesAlongRayInternal(TraceShapeRay* ray,
        Vector                                                 origin,
        in RnQueryShapeAttr                                    query,
        nint?                                                  callback,
        bool                                                   unique,
        uint                                                   nTest,
        Span<uint>                                             entities)
    {
        var traceFilter = stackalloc CTraceFilter[1];

        if (callback is not null)
        {
            var vtable = stackalloc CTraceFilterVTableDescriptor[1];
            vtable->ShouldHitEntity = (delegate* unmanaged<CTraceFilter*, nint, bool>) callback;
            traceFilter->Vtable     = vtable;
        }

        traceFilter->QueryAttribute = query;

        var results = stackalloc COverlapHitVector[1];

        var count = _entitiesAlongRay(_interface,
                                      ray,
                                      &origin,
                                      traceFilter,
                                      nTest,
                                      results,
                                      unique);

        if (count == 0)
        {
            return 0;
        }

        count = Math.Min(count, entities.Length);
        var ptr = results[0].m_pMemory;

        for (var i = 0; i < count; i++)
        {
            ref var enumerateResult = ref ptr[i];
            entities[i] = enumerateResult.m_ShapeAttributes.EntityId;
        }

        CoreBridge.MemAlloc.Free((nint) results[0].m_pMemory);

        return count;
    }

    [UnmanagedCallersOnly]
    private static bool NoPlayer_ShouldHitEntity(CTraceFilter* filter, nint entityPtr)
    {
        var entity = BaseEntity.Create(entityPtr);

        if (entity == null || entity.IsValidEntity)
        {
            return false;
        }

        return !entity.IsPlayerPawn;
    }

    [StructLayout(LayoutKind.Explicit, Size = 112)]
    private ref struct COverlapHit
    {
        [FieldOffset(0)]
        public RnCollisionAttr m_ShapeAttributes;

        [FieldOffset(0x50)]
        public PhysicsSurfaceProperties* m_pSurfaceProperty;

        [FieldOffset(0x58)]
        private void* m_hBody;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 8)]
    private ref struct COverlapHitVector
    {
        [FieldOffset(0)]
        public int m_nSize;

        [FieldOffset(16)]
        public COverlapHit* m_pMemory;

        [FieldOffset(24)]
        public int AllocationCount;

        [FieldOffset(28)]
        public int GrowSize;
    }
}
