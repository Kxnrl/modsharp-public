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

using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.Listeners;

public interface IEntityListener
{
    const int ApiVersion = 1;

    /// <summary>
    ///     接口实现的版本号
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     优先级
    /// </summary>
    int ListenerPriority { get; }

    void OnEntityCreated(IBaseEntity entity)
    {
    }

    void OnEntityDeleted(IBaseEntity entity)
    {
    }

    void OnEntitySpawned(IBaseEntity entity)
    {
    }

    void OnEntityFollowed(IBaseEntity entity, IBaseEntity? owner)
    {
    }

    EHookAction OnEntityFireOutput(IBaseEntity entity, string output, IBaseEntity? activator, float delay)
        => EHookAction.Ignored;

    EHookAction OnEntityAcceptInput(IBaseEntity entity,
        string                                  input,
        in EntityVariant                        value,
        IBaseEntity?                            activator,
        IBaseEntity?                            caller)
        => EHookAction.Ignored;
}
