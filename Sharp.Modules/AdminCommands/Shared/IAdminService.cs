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

using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Aggregated admin operation services (ban/mute/gag/silence) exposed to consumers.
///     If your ban/mute records live in SQL or another backend, implement <see cref="IAdminOperationStorageService" />.
/// </summary>
public interface IAdminService
{
    public const string Identity = nameof(IAdminService);

    IBanService     Ban     { get; }
    IMuteService    Mute    { get; }
    IGagService     Gag     { get; }
    ISilenceService Silence { get; }

    /// <summary>
    ///     Registers a new operation handler. It is not recommended to register handlers outside OnAllModuleLoaded or
    ///     PostInit.
    /// </summary>
    void RegisterHandler(string moduleIdentity, IAdminOperationHandler handler);

    /// <summary>
    ///     Applies an admin operation to an online target.
    ///     <para>
    ///         This executes the full pipeline: notifications, game actions (e.g. kick), cache updates, and storage
    ///         persistence.
    ///         For simple data persistence without side effects, use <see cref="IAdminOperationStorageService" />.
    ///     </para>
    /// </summary>
    void Apply(IGameClient? admin,
        IGameClient         target,
        AdminOperationType  type,
        TimeSpan?           duration,
        string              reason,
        bool                silent = false);

    /// <summary>
    ///     Applies an admin operation to an offline target.
    ///     <para>
    ///         This executes the full pipeline: game actions (e.g. kick), cache updates, and storage
    ///         persistence.
    ///         For simple data persistence without side effects, use <see cref="IAdminOperationStorageService" />.
    ///     </para>
    /// </summary>
    void Apply(IGameClient? admin,
        SteamID             target,
        AdminOperationType  type,
        TimeSpan?           duration,
        string              reason);

    /// <summary>
    ///     Removes an admin operation from an online target.
    ///     <para>
    ///         This executes the full pipeline: notifications, cache updates, and storage persistence.
    ///         For simple data persistence without side effects, use <see cref="IAdminOperationStorageService" />.
    ///     </para>
    /// </summary>
    void Remove(IGameClient? admin,
        IGameClient          target,
        AdminOperationType   type,
        string               reason,
        bool                 silent = false);

    /// <summary>
    ///     Removes an admin operation from an offline target.
    ///     <para>
    ///         This executes the full pipeline: cache updates, and storage persistence.
    ///         For simple data persistence without side effects, use <see cref="IAdminOperationStorageService" />.
    ///     </para>
    /// </summary>
    void Remove(IGameClient? admin,
        SteamID              target,
        AdminOperationType   type,
        string               reason);
}
