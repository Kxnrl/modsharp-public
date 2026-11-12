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

using System.Collections.Generic;
using Sharp.Shared.Units;

namespace Sharp.Shared.Listeners;

public interface IAddonListener
{
    const int ApiVersion = 1;

    /// <summary>
    ///     Listener version
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     Priority
    /// </summary>
    int ListenerPriority { get; }

    /// <summary>
    ///     Called on every connection attempt to collect extra addons for this client (on top of the server addons). <br />
    ///     Clients reconnect once per addon, so return the same result for the same client while it is downloading. <br />
    ///     Runs while the server answers the connection (before ClientConnect), keep it fast and synchronous.
    /// </summary>
    void OnClientQueryAddons(SteamID steamId, List<ulong> addons)
    {
    }
}
