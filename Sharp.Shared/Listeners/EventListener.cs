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

using Sharp.Shared.Objects;

namespace Sharp.Shared.Listeners;

public interface IEventListener
{
    const int ApiVersion = 1;

    /// <summary>
    ///     Listenver version
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     Priority
    /// </summary>
    int ListenerPriority { get; }

    /// <summary>
    ///     Hook and modify event, if you only need to listen, do not implement this function
    /// </summary>
    /// <returns>False = Block event from firing</returns>
    bool HookFireEvent(IGameEvent @event, ref bool serverOnly)
        => true;

    /// <summary>
    ///     Event listener, do not modify event here
    /// </summary>
    /// <param name="event"></param>
    void FireGameEvent(IGameEvent @event);
}
