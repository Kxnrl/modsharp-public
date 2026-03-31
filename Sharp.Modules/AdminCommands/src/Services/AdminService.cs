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

using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services;

/// <summary>
///     Aggregates feature services for external callers.
/// </summary>
internal sealed class AdminService : IAdminService
{
    private readonly AdminOperationEngine _engine;

    public AdminService(IBanService ban,
        IMuteService                mute,
        IGagService                 gag,
        ISilenceService             silence,
        AdminOperationEngine        engine)
    {
        Ban     = ban;
        Mute    = mute;
        Gag     = gag;
        Silence = silence;
        _engine = engine;
    }

    public IBanService     Ban     { get; }
    public IMuteService    Mute    { get; }
    public IGagService     Gag     { get; }
    public ISilenceService Silence { get; }

    public void RegisterHandler(string moduleIdentity, IAdminOperationHandler handler)
        => _engine.RegisterHandler(moduleIdentity, handler);

    public void Apply(IGameClient? admin,
        IGameClient                target,
        AdminOperationType         type,
        TimeSpan?                  duration,
        string                     reason,
        bool                       silent = false)
        => _engine.ApplyOnline(admin, target, type, duration, reason, silent);

    public void Apply(IGameClient? admin, SteamID target, AdminOperationType type, TimeSpan? duration, string reason)
        => _engine.ApplyOffline(admin, target, target.ToString(), type, duration, reason);

    public void Remove(IGameClient? admin,
        IGameClient                 target,
        AdminOperationType          type,
        string                      reason,
        bool                        silent = false)
        => _engine.RemoveOnline(admin, target, type, reason, silent);

    public void Remove(IGameClient? admin, SteamID target, AdminOperationType type, string reason)
        => _engine.RemoveOffline(admin, target, target.ToString(), type, reason);
}
