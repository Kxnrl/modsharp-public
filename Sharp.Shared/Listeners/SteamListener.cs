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

using Sharp.Shared.Enums;
using Sharp.Shared.Units;

namespace Sharp.Shared.Listeners;

public interface ISteamListener
{
    const int ApiVersion = 2;

    /// <summary>
    ///     Listener version
    /// </summary>
    int ListenerVersion { get; }

    /// <summary>
    ///     Priority (higher values run first)
    /// </summary>
    int ListenerPriority { get; }

    /// <summary>
    ///     Triggered after calling <see cref="Objects.ISteamApi.RequestUserGroupStatus" /> (<c>GSClientGroupStatus_t</c>).
    /// </summary>
    /// <param name="steamId">The user's SteamID.</param>
    /// <param name="groupId">The Steam group's SteamID.</param>
    /// <param name="isMember"><see langword="true" /> if the user is a member of the group.</param>
    /// <param name="isOfficer"><see langword="true" /> if the user is an officer of the group.</param>
    void OnGroupStatusResult(SteamID steamId, SteamID groupId, bool isMember, bool isOfficer)
    {
    }

    /// <summary>
    ///     Called when the game server connects to Steam (<c>SteamServersConnected_t</c>).
    /// </summary>
    void OnSteamServersConnected()
    {
    }

    /// <summary>
    ///     Called when the game server disconnects from Steam (<c>SteamServersDisconnected_t</c>).
    /// </summary>
    /// <param name="reason">The Steamworks disconnection result code.</param>
    void OnSteamServersDisconnected(SteamApiResult reason)
    {
    }

    /// <summary>
    ///     Called when the game server fails to connect to Steam (<c>SteamServerConnectFailure_t</c>).
    /// </summary>
    /// <param name="reason">The Steamworks failure result code.</param>
    /// <param name="stillRetrying"><see langword="true" /> if Steam is still retrying the connection.</param>
    void OnSteamServersConnectFailure(SteamApiResult reason, bool stillRetrying)
    {
    }

    /// <summary>
    ///     Called when a Steam Workshop UGC item download completes (<c>DownloadItemResult_t</c>).
    /// </summary>
    /// <param name="sharedFileId">The Workshop item's published file ID.</param>
    /// <param name="result">The download result code.</param>
    void OnDownloadItemResult(ulong sharedFileId, SteamApiResult result)
    {
    }

    /// <summary>
    ///     Called when a Steam Workshop UGC item finishes installing (<c>ItemInstalled_t</c>).
    /// </summary>
    /// <param name="publishedFileId">The Workshop item's published file ID.</param>
    void OnItemInstalled(ulong publishedFileId)
    {
    }
}
