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

namespace Sharp.Shared.Enums;

/// <summary>
///     Action returned by pre-hooks to control native call execution and return value handling.
/// </summary>
/// <remarks>
///     Each hook in <see cref="Managers.IHookManager" /> supports only a specific subset of actions (often only
///     <see cref="Ignored" /> and <see cref="SkipCallReturnOverride" />). Returning an unsupported action triggers a native
///     <c>FatalError</c> and terminates the server.
/// </remarks>
public enum EHookAction : byte
{
    /// <summary>
    ///     Don't care about return value or parameters
    /// </summary>
    Ignored,

    /// <summary>
    ///     Modify parameters then call original function → Use original return value
    /// </summary>
    ChangeParamReturnDefault,

    /// <summary>
    ///     Modify parameters then call original function → Use our provided return value
    /// </summary>
    ChangeParamReturnOverride,

    /// <summary>
    ///     Ignore parameters then call original function → Use our provided return value
    /// </summary>
    IgnoreParamReturnOverride,

    /// <summary>
    ///     Skip original function → Use our provided return value
    /// </summary>
    SkipCallReturnOverride,
}
