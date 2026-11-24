/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
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

using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.Listeners;

public interface IEntityListener
{
    const int ApiVersion = 1;

    /// <summary>
    ///     Listener version
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     Priority (higher values run first)
    /// </summary>
    int ListenerPriority { get; }

    /// <summary>
    ///     Called when an entity is created by the game entity system, before it is spawned.
    /// </summary>
    /// <param name="entity">The created entity.</param>
    void OnEntityCreated(IBaseEntity entity)
    {
    }

    /// <summary>
    ///     Called when an entity is being deleted by the game entity system.
    /// </summary>
    /// <remarks>
    ///     The entity wrapper remains readable during this callback and is invalidated immediately after all listeners return.
    /// </remarks>
    /// <param name="entity">The entity being deleted.</param>
    void OnEntityDeleted(IBaseEntity entity)
    {
    }

    /// <summary>
    ///     Called after an entity has been spawned by the game entity system.
    /// </summary>
    /// <param name="entity">The spawned entity.</param>
    void OnEntitySpawned(IBaseEntity entity)
    {
    }

    /// <summary>
    ///     Called when an entity's parent changes (<c>OnEntityParentChanged</c> in Source 2).
    /// </summary>
    /// <param name="entity">The child entity whose parent changed.</param>
    /// <param name="owner">The new parent entity, or <see langword="null" /> if the entity was detached.</param>
    void OnEntityFollowed(IBaseEntity entity, IBaseEntity? owner)
    {
    }

    /// <summary>
    ///     Called before a hooked entity output fires (<c>CEntityIOOutput::FireOutput</c> pre-hook).
    /// </summary>
    /// <param name="entity">The entity firing the output.</param>
    /// <param name="output">The output name (normalized to lowercase).</param>
    /// <param name="activator">The activator entity, if any.</param>
    /// <param name="delay">The output delay in seconds.</param>
    /// <returns>
    ///     Return <see cref="EHookAction.SkipCallReturnOverride" /> to block the output; all other values allow it to proceed.
    ///     All installed listeners run without short-circuiting and the highest <see cref="EHookAction" /> value is used.
    /// </returns>
    /// <remarks>
    ///     Requires registering the classname/output pair via <see cref="Managers.IEntityManager.HookEntityOutput" />.
    ///     Hooks are global across modules, so listeners receive every hooked output and should filter by both entity and output name.
    /// </remarks>
    EHookAction OnEntityFireOutput(IBaseEntity entity, string output, IBaseEntity? activator, float delay)
        => EHookAction.Ignored;

    /// <summary>
    ///     Called before a hooked entity input is accepted (<c>CEntityIdentity::AcceptInput</c> pre-hook).
    /// </summary>
    /// <param name="entity">The target entity receiving the input.</param>
    /// <param name="input">The input name (preserves original casing; matching is case-insensitive).</param>
    /// <param name="value">Read-only copy of the input variant value.</param>
    /// <param name="activator">The activator entity, if any.</param>
    /// <param name="caller">The caller entity, if any.</param>
    /// <returns>
    ///     Return <see cref="EHookAction.SkipCallReturnOverride" /> to block the input; all other values allow it to proceed.
    ///     All installed listeners run without short-circuiting and the highest <see cref="EHookAction" /> value is used.
    /// </returns>
    /// <remarks>
    ///     Requires registering the classname/input pair via <see cref="Managers.IEntityManager.HookEntityInput" />.
    ///     Hooks are global across modules, so listeners receive every hooked input and should filter by both entity and input name.
    /// </remarks>
    EHookAction OnEntityAcceptInput(IBaseEntity entity,
        string                                  input,
        in EntityVariant                        value,
        IBaseEntity?                            activator,
        IBaseEntity?                            caller)
        => EHookAction.Ignored;
}
