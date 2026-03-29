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
using Sharp.Shared.Units;

namespace Sharp.Shared.HookParams;

public interface IClientConnectHookParams : IFunctionParams
{
    SteamID SteamId { get; }
    string  Name    { get; }

    /// <summary>
    ///     Set the reason to reject this client's connection
    ///     <br />
    ///     <b>Note:</b> You must also return <see cref="EHookAction.SkipCallReturnOverride" /> for this to take effect.
    ///     <br />If rejected, the reason is logged to the server console.
    /// </summary>
    /// <param name="reason">The message to show in the server console.</param>
    void SetBlockReason(string reason);
}
