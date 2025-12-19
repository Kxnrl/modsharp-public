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
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="mask">See <seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">Typically use 7 (1+2+4)</param>
    /// <param name="excludeLayers">Layers to exclude</param>
    /// <param name="ignore1">Entity 1 to ignore</param>
    /// <param name="ignore2">Entity 2 to ignore</param>
    TraceResult TraceLine(Vector start,
        Vector                   end,
        InteractionLayers        mask,
        CollisionGroupType       group,
        TraceQueryFlag           flags,
        InteractionLayers        excludeLayers = InteractionLayers.None,
        IBaseEntity?             ignore1       = null,
        IBaseEntity?             ignore2       = null);

    /// <summary>
    ///     TraceLine but ignores players
    /// </summary>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="mask">See <seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">Typically use 7 (1+2+4)</param>
    /// <param name="excludeLayers">Layers to exclude</param>
    /// <param name="ignore1">Entity 1 to ignore</param>
    /// <param name="ignore2">Entity 2 to ignore</param>
    TraceResult TraceLineNoPlayers(Vector start,
        Vector                            end,
        InteractionLayers                 mask,
        CollisionGroupType                group,
        TraceQueryFlag                    flags,
        InteractionLayers                 excludeLayers = InteractionLayers.None,
        IBaseEntity?                      ignore1       = null,
        IBaseEntity?                      ignore2       = null);

    /// <summary>
    ///     TraceLine with custom filter
    /// </summary>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="mask">See <seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">Typically use 7 (1+2+4)</param>
    /// <param name="excludeLayers">Layers to exclude</param>
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
    /// <param name="ray">Ray shape</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="mask">See <seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">Typically use 7 (1+2+4)</param>
    /// <param name="excludeLayers">Layers to exclude</param>
    /// <param name="ignore1">Entity 1 to ignore</param>
    /// <param name="ignore2">Entity 2 to ignore</param>
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
    ///     TraceShape but ignores players
    /// </summary>
    /// <param name="ray">Ray shape</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="mask">See <seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">Typically use 7 (1+2+4)</param>
    /// <param name="excludeLayers">Layers to exclude</param>
    /// <param name="ignore1">Entity 1 to ignore</param>
    /// <param name="ignore2">Entity 2 to ignore</param>
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
    ///     TraceShape with custom filter
    /// </summary>
    /// <param name="ray">Ray shape</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="mask">See <seealso cref="Sharp.Shared.Definition.UsefulInteractionLayers" /></param>
    /// <param name="group"><br />4 = SwingOrStab<br />3 = FireBullets<br />4 = OpenSource2</param>
    /// <param name="flags">Typically use 7 (1+2+4)</param>
    /// <param name="excludeLayers">Layers to exclude</param>
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
    ///     TraceShape using the game's MovementFilter
    /// </summary>
    /// <param name="ray">Ray shape</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="interactsWith">Generally, using InteractsWith from CollisionProperty is sufficient</param>
    /// <param name="pawn">Player pawn</param>
    [Obsolete("Does not work, will be removed", true)]
    TraceResult TraceShapePlayerMovement(TraceShapeRay ray,
        Vector                                         start,
        Vector                                         end,
        InteractionLayers                              interactsWith,
        IPlayerPawn                                    pawn);

    /// <summary>
    ///     TraceShape
    /// </summary>
    /// <param name="ray">Defines Ray_t</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="query">Trace parameters</param>
    /// <param name="filterCallback">Filter callback, requires manual conversion from <b>unmanaged</b> to <b>nint</b></param>
    GameTrace TraceShape(TraceShapeRay ray, Vector start, Vector end, in RnQueryShapeAttr query, nint? filterCallback = null);

    /// <summary>
    ///     TraceLine
    /// </summary>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="query">Trace parameters</param>
    /// <param name="filterCallback">Filter callback, requires manual conversion from <b>unmanaged</b> to <b>nint</b></param>
    GameTrace TraceLine(Vector start, Vector end, in RnQueryShapeAttr query, nint? filterCallback = null);

    /// <summary>
    ///     TraceShapePlayerMovement
    /// </summary>
    /// <param name="ray">Ray shape</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="query">Trace parameters</param>
    GameTrace TraceShapePlayerMovement(TraceShapeRay ray, Vector start, Vector end, in RnQueryShapeAttr query);

    /// <summary>
    ///     TraceShape (filters players)
    /// </summary>
    /// <param name="ray">Ray shape</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="query">Trace parameters</param>
    GameTrace TraceShapeNoPlayers(TraceShapeRay ray, Vector start, Vector end, in RnQueryShapeAttr query);

    /// <summary>
    ///     TraceLine (filters players)
    /// </summary>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <param name="query">Trace parameters</param>
    GameTrace TraceLineNoPlayers(Vector start, Vector end, in RnQueryShapeAttr query);

    /// <summary>
    ///     Enumerate entities along a ray
    /// </summary>
    /// <param name="ray">Ray shape</param>
    /// <param name="origin">Origin position</param>
    /// <param name="query">Trace parameters</param>
    /// <param name="unique">Unique entities only</param>
    /// <param name="entities">Buffer for entities</param>
    /// <param name="test">Unknown purpose, game always uses <b>1</b></param>
    /// <param name="filterCallback">Filter callback, requires manual conversion from <b>unmanaged</b> to <b>nint</b></param>
    /// <returns>Number of entities found</returns>
    int EntitiesAlongRay(TraceShapeRay ray,
        Vector                         origin,
        in RnQueryShapeAttr            query,
        bool                           unique,
        Span<uint>                     entities,
        uint                           test           = 1,
        nint?                          filterCallback = null);
}
