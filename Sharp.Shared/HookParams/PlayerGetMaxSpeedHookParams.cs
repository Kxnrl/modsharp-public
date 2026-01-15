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

namespace Sharp.Shared.HookParams;

public interface IPlayerGetMaxSpeedHookParams : IFunctionParams, IPlayerPawnFunctionParams
{
    /// <summary>
    ///     Gets the original max run speed calculated by the game.
    ///     <br />
    ///     To override this value, your hook must return <see cref="EHookAction.SkipCallReturnOverride" />
    ///     paired with the new speed value.
    ///     <br />
    ///     <example>
    ///         Example:
    ///         <code>
    /// return new (EHookAction.SkipCallReturnOverride, 300.0f);
    /// </code>
    ///     </example>
    /// </summary>
    float OriginalSpeed { get; }
}
