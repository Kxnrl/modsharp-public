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
using Sharp.Shared.Enums;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Runtime;
using Sharp.Shared.Units;

namespace Sharp.Core.Bridges.Natives;

public static unsafe partial class Game
{
    public static partial void PrintChannelAll(HudPrintChannel channel, string message);

    public static partial void PrintChannelTeam(HudPrintChannel channel, CStrikeTeam team, string message);

    public static partial void PrintChannelFilter(HudPrintChannel channel, string message, RecipientFilter* filter);

    public static partial void RadioMessageTeam(CStrikeTeam team,
        PlayerSlot                                          slot,
        string                                              name,
        string?                                             param1,
        string?                                             param2,
        string?                                             param3,
        string?                                             param4);

    public static partial void RadioMessageAll(PlayerSlot slot,
        string                                            name,
        string?                                           param1,
        string?                                           param2,
        string?                                           param3,
        string?                                           param4);

    public static partial void TerminateRound(float delay,
        RoundEndReason                              reason,
        bool                                        bypassHook,
        TeamRewardInfo*                             rewardInfo,
        int                                         infoSize);

    public static partial void TraceLine(Vector* start,
        Vector*                                  end,
        InteractionLayers                        mask,
        CollisionGroupType                       group,
        TraceQueryFlag                           flags,
        InteractionLayers                        excludeLayers,
        IntPtr                                   ignoreEntity1,
        IntPtr                                   ignoreEntity2,
        bool                                     ignorePlayers,
        TraceResultStruct*                       result);

    public static partial void TraceLineFilter(Vector* start,
        Vector*                                        end,
        InteractionLayers                              mask,
        CollisionGroupType                             group,
        TraceQueryFlag                                 flags,
        InteractionLayers                              excludeLayers,
        TraceResultStruct*                             result);

    public static partial void TraceShape(TraceShapeRay* ray,
        Vector*                                          start,
        Vector*                                          end,
        InteractionLayers                                mask,
        CollisionGroupType                               group,
        TraceQueryFlag                                   flags,
        InteractionLayers                                excludeLayers,
        IntPtr                                           ignoreEntity1,
        IntPtr                                           ignoreEntity2,
        bool                                             ignorePlayers,
        TraceResultStruct*                               result);

    public static partial void TraceShapeFilter(TraceShapeRay* ray,
        Vector*                                                start,
        Vector*                                                end,
        InteractionLayers                                      mask,
        CollisionGroupType                                     group,
        TraceQueryFlag                                         flags,
        InteractionLayers                                      excludeLayers,
        TraceResultStruct*                                     result);

    public static partial int DispatchParticleEffectPosition(string particle,
        Vector*                                                     origin,
        Vector*                                                     angles,
        RecipientFilter*                                            filter);

    public static partial int DispatchParticleEffectEntityPosition(string particle,
        nint                                                              entity,
        Vector*                                                           origin,
        Vector*                                                           angles,
        bool                                                              resetEntity,
        RecipientFilter*                                                  filter);

    public static partial int DispatchParticleEffectAttachment(string particle,
        ParticleAttachmentType                                        attachType,
        nint                                                          entity,
        byte                                                          attachmentIndex,
        bool                                                          resetEntity,
        RecipientFilter*                                              filter);

    public static partial nint GetMapGroupMapList(string mapGroup);

    public static partial NativeSpan<byte> FindResourceDataBlockInfo(string fileName, string pathId);

    public static partial ResourceStatus GetResourceStatus(string fileName);

    public static partial nint GetAddonName();

    public static partial nint GetMapName();

    public static partial nint FindWeaponVDataByName(string name);

    public static partial void DualAddonPurgeCheck();

    public static partial void DualAddonOverrideCheck(ulong steamId, double time);

    public static partial bool AddWorkshopMap(ulong sharedFileId, string name, string path);

    public static partial bool WorkshopMapExists(ulong sharedFileId);

    public static partial bool RemoveWorkshopMap(ulong sharedFileId);

    public static partial nint ListWorkshopMaps();
}
