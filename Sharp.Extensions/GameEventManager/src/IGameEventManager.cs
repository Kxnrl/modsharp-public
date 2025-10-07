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
using Sharp.Shared.Types;

namespace Sharp.Extensions.GameEventManager;

public interface IGameEventManager
{
    delegate void DelegateOnEventFired(IGameEvent e);

    delegate HookReturnValue<bool> DelegateOnHookEvent(in EventHookParams param);

    void HookEvent(string eventName, DelegateOnHookEvent callback);

    void ListenEvent(string eventName, DelegateOnEventFired callback);

    IGameEvent? CreateEvent(string eventName, bool force);

    T? CreateEvent<T>(bool force) where T : class, IGameEvent;

    T? CloneEvent<T>(T e) where T : class, IGameEvent;
}
