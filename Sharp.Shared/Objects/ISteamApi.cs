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

namespace Sharp.Shared.Objects;

public interface ISteamApi
{
    /// <summary>
    ///     Check if Steam service is available
    /// </summary>
    bool IsAvailable();

    /// <summary>
    ///     Description of the game.  This is a required field and is displayed in the steam server browser....for now. This is
    ///     a required field, but it will go away eventually, as the data should be determined from the AppID.
    /// </summary>
    void SetGameDescription(string description);

    /// <summary>
    ///     Check if logged into Steam
    /// </summary>
    bool BLoggedOn();

    /// <summary>
    ///     Check if server is VAC secured
    /// </summary>
    bool BSecure();

    /// <summary>
    ///     Get server Steam ID
    /// </summary>
    SteamID GetSteamId();

    /// <summary>
    ///     Returns true if the master server has requested a restart.
    /// </summary>
    bool WasRestartRequested();

    /// <summary>
    ///     Max player count that will be reported to server browser and client queries
    /// </summary>
    void SetMaxPlayerCount(int count);

    /// <summary>
    ///     Number of bots.  Default value is zero
    /// </summary>
    void SetBotPlayerCount(int count);

    /// <summary>
    ///     Set the name of server as it will appear in the server browser
    /// </summary>
    void SetServerName(string name);

    /// <summary>
    ///     Set name of map to report in the server browser
    /// </summary>
    void SetMapName(string name);

    /// <summary>
    ///     Let people know if your server will require a password
    /// </summary>
    void SetPasswordProtected(bool protect);

    /// <summary>
    ///     Sets a string defining the "gametags" for this server, this is optional, but if it is set
    ///     it allows users to filter in the matchmaking/server-browser interfaces based on the value
    /// </summary>
    void SetGameTags(string tags);

    /// <summary>
    ///     Ask if a user in the specified group, results returns async by GSUserGroupStatus_t
    /// </summary>
    bool RequestUserGroupStatus(SteamID steamId, SteamID groupId);

    /// <summary>
    ///     After receiving a user's authentication data, and passing it to SendUserConnectAndAuthenticate, use this function
    ///     <br />
    ///     to determine if the user owns downloadable content specified by the provided AppID.
    /// </summary>
    UserHasLicenseForApp UserHasLicenseForApp(SteamID steamId, uint appId);

    /// <summary>
    ///     Get Steam Workshop item state
    /// </summary>
    WorkshopItemState GetItemState(ulong sharedFileId);

    /// <summary>
    ///     Get download progress information
    /// </summary>
    bool GetItemDownloadInfo(ulong sharedFileId, out ulong bytesDownloaded, out ulong bytesTotal);

    /// <summary>
    ///     Download Steam Workshop item
    /// </summary>
    /// <returns></returns>
    bool DownloadItem(ulong sharedFileId, bool highPriority);

    /// <summary>
    ///     Suspend or resume downloads
    /// </summary>
    void SuspendDownloads(bool suspend);

    /// <summary>
    ///     Get public IP address from master server
    /// </summary>
    /// <returns></returns>
    uint GetPublicIP();
}
