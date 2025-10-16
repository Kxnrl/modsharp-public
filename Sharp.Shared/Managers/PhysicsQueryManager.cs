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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.Managers;

public interface IPhysicsQueryManager
{
    /// <summary>
    ///     TraceLine
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="mask">看<seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">基本上都用7 (1+2+4)</param>
    /// <param name="excludeLayers">排除的Layers</param>
    /// <param name="ignore1">忽略的实体1</param>
    /// <param name="ignore2">忽略的实体2</param>
    TraceResult TraceLine(Vector start,
        Vector                   end,
        InteractionLayers        mask,
        CollisionGroupType       group,
        TraceQueryFlag           flags,
        InteractionLayers        excludeLayers = InteractionLayers.None,
        IBaseEntity?             ignore1       = null,
        IBaseEntity?             ignore2       = null);

    /// <summary>
    ///     TraceLine 但 忽略玩家
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="mask">看<seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">基本上都用7 (1+2+4)</param>
    /// <param name="excludeLayers">排除的Layers</param>
    /// <param name="ignore1">忽略的实体1</param>
    /// <param name="ignore2">忽略的实体2</param>
    TraceResult TraceLineNoPlayers(Vector start,
        Vector                            end,
        InteractionLayers                 mask,
        CollisionGroupType                group,
        TraceQueryFlag                    flags,
        InteractionLayers                 excludeLayers = InteractionLayers.None,
        IBaseEntity?                      ignore1       = null,
        IBaseEntity?                      ignore2       = null);

    /// <summary>
    ///     TraceLine, 使用自定义Filter
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="mask">看<seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">基本上都用7 (1+2+4)</param>
    /// <param name="excludeLayers">排除的Layers</param>
    /// <param name="filter">Filter Callback</param>
    TraceResult TraceLineFilter(Vector start,
        Vector                         end,
        InteractionLayers              mask,
        CollisionGroupType             group,
        TraceQueryFlag                 flags,
        InteractionLayers              excludeLayers,
        Func<IBaseEntity, bool>        filter);

    /// <summary>
    ///     TraceShape
    /// </summary>
    /// <param name="ray">定义Ray_t</param>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="mask">看<seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">基本上都用7 (1+2+4)</param>
    /// <param name="excludeLayers">排除的Layers</param>
    /// <param name="ignore1">忽略的实体1</param>
    /// <param name="ignore2">忽略的实体2</param>
    TraceResult TraceShape(TraceShapeRay ray,
        Vector                           start,
        Vector                           end,
        InteractionLayers                mask,
        CollisionGroupType               group,
        TraceQueryFlag                   flags,
        InteractionLayers                excludeLayers = InteractionLayers.None,
        IBaseEntity?                     ignore1       = null,
        IBaseEntity?                     ignore2       = null);

    /// <summary>
    ///     TraceShape 但 忽略玩家
    /// </summary>
    /// <param name="ray">定义Ray_t</param>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="mask">看<seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">基本上都用7 (1+2+4)</param>
    /// <param name="excludeLayers">排除的Layers</param>
    /// <param name="ignore1">忽略的实体1</param>
    /// <param name="ignore2">忽略的实体2</param>
    TraceResult TraceShapeNoPlayers(TraceShapeRay ray,
        Vector                                    start,
        Vector                                    end,
        InteractionLayers                         mask,
        CollisionGroupType                        group,
        TraceQueryFlag                            flags,
        InteractionLayers                         excludeLayers = InteractionLayers.None,
        IBaseEntity?                              ignore1       = null,
        IBaseEntity?                              ignore2       = null);

    /// <summary>
    ///     TraceShape, 使用自定义Filter
    /// </summary>
    /// <param name="ray">定义Ray_t</param>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="mask">看<seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">基本上都用7 (1+2+4)</param>
    /// <param name="excludeLayers">排除的Layers</param>
    /// <param name="filter">Filter Callback</param>
    TraceResult TraceShapeFilter(TraceShapeRay ray,
        Vector                                 start,
        Vector                                 end,
        InteractionLayers                      mask,
        CollisionGroupType                     group,
        TraceQueryFlag                         flags,
        InteractionLayers                      excludeLayers,
        Func<IBaseEntity, bool>                filter);

    /// <summary>
    ///     TraceShape, 用游戏的MovementFilter
    /// </summary>
    /// <param name="ray">定义Ray_t</param>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="interactsWith"> 一般情况下用 CollisionProperty 里的 InteractsWith 就可以了</param>
    /// <param name="pawn">玩家pawn</param>
    TraceResult TraceShapePlayerMovement(TraceShapeRay ray,
        Vector                                         start,
        Vector                                         end,
        InteractionLayers                              interactsWith,
        IPlayerPawn                                    pawn);

    /// <summary>
    ///     TraceShape
    /// </summary>
    /// <param name="ray">定义Ray_t</param>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="query">Trace参数</param>
    /// <param name="filterCallback">filter回调, 需要自行转换<b>unmanaged</b>到<b>nint</b></param>
    GameTrace TraceShape(TraceShapeRay ray, Vector start, Vector end, in RnQueryShapeAttr query, nint? filterCallback = null);

    /// <summary>
    ///     TraceLine
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="query">Trace参数</param>
    /// <param name="filterCallback">filter回调, 需要自行转换<b>unmanaged</b>到<b>nint</b></param>
    GameTrace TraceLine(Vector start, Vector end, in RnQueryShapeAttr query, nint? filterCallback = null);

    /// <summary>
    ///     TracePlayerMovement
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="query">Trace参数</param>
    GameTrace TraceShapePlayerMovement(TraceShapeRay ray, Vector start, Vector end, in RnQueryShapeAttr query);

    /// <summary>
    ///     TraceShape (自动过滤玩家)
    /// </summary>
    /// <param name="ray">定义Ray_t</param>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="query">Trace参数</param>
    GameTrace TraceShapeNoPlayers(TraceShapeRay ray, Vector start, Vector end, in RnQueryShapeAttr query);

    /// <summary>
    ///     TraceLine (自动过滤玩家)
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="query">Trace参数</param>
    GameTrace TraceLineNoPlayers(Vector start, Vector end, in RnQueryShapeAttr query);

    /// <summary>
    ///     遍历Ray上的实体
    /// </summary>
    /// <param name="ray">定义Ray_t</param>
    /// <param name="origin">起始位置</param>
    /// <param name="query">Trace参数</param>
    /// <param name="unique">唯一</param>
    /// <param name="entities">buffer</param>
    /// <param name="test">不知道干什么的, 游戏都用<b>1</b></param>
    /// <param name="filterCallback">filter回调, 需要自行转换<b>unmanaged</b>到<b>nint</b></param>
    /// <returns>包含的实体个数</returns>
    int EntitiesAlongRay(TraceShapeRay ray,
        Vector                         origin,
        in RnQueryShapeAttr            query,
        bool                           unique,
        Span<uint>                     entities,
        uint                           test           = 1,
        nint?                          filterCallback = null);
}
