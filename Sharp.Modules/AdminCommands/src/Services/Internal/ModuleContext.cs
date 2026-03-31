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

using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;

namespace Sharp.Modules.AdminCommands.Services.Internal;

/// <summary>
///     Holds optional cross-module services so they can be shared safely across command handlers.
/// </summary>
internal sealed class ModuleContext
{
    /// <summary>
    ///     0 = off, 1 = broadcast (ignore immunity), 2 = broadcast with max immunity gate
    /// </summary>
    private const int DefaultBroadcastType = 1;

    private const int DefaultBroadcastMaxImmunity = byte.MaxValue;

    private readonly IConVar? _broadcastTypeConVar;
    private readonly IConVar? _broadcastMaxImmunityConVar;

    public ModuleContext(InterfaceBridge bridge)
    {
        _broadcastTypeConVar = bridge.ConVarManager.CreateConVar("ms_admin_broadcast_type",
                                                                 DefaultBroadcastType,
                                                                 0,
                                                                 2,
                                                                 "Admin reply broadcast mode: 0=off, 1=everyone, 2=respect max immunity.",
                                                                 ConVarFlags.Release);

        _broadcastMaxImmunityConVar = bridge.ConVarManager.CreateConVar("ms_admin_broadcast_max_immunity",
                                                                        DefaultBroadcastMaxImmunity,
                                                                        0,
                                                                        (int) byte.MaxValue,
                                                                        "When ms_admin_broadcast_type is 2, admins with immunity at or below this value broadcast; higher immunity stays private.",
                                                                        ConVarFlags.Release);
    }

    public ILocalizerManager? LocalizerManager { get; private set; }
    public ITargetingManager? TargetingManager { get; private set; }
    public IAdminManager?     AdminManager     { get; private set; }

    public int BroadcastType => Math.Clamp(_broadcastTypeConVar?.GetInt32() ?? DefaultBroadcastType, 0, 2);

    public int BroadcastMaxImmunity
        => Math.Clamp(_broadcastMaxImmunityConVar?.GetInt32() ?? DefaultBroadcastMaxImmunity, 0, byte.MaxValue);

    public void UpdateLocalizer(ILocalizerManager? manager)
        => LocalizerManager = manager;

    public void UpdateTargeting(ITargetingManager? manager)
        => TargetingManager = manager;

    public void UpdateAdminManager(IAdminManager? manager)
        => AdminManager = manager;
}
