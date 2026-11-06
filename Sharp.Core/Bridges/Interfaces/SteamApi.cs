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

using Sharp.Core.CStrike;
using Sharp.Generator;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Core.Bridges.Interfaces;

[NativeVirtualObject(HasDestructors = true)]
internal partial class SteamApi : NativeObject, ISteamApi
{
    public partial bool IsAvailable();

    public partial void SetGameDescription(string description);

    public partial bool BLoggedOn();

    public partial bool BSecure();

    public partial SteamID GetSteamId();

    public partial bool WasRestartRequested();

    public partial void SetMaxPlayerCount(int count);

    public partial void SetBotPlayerCount(int count);

    public partial void SetServerName(string name);

    public partial void SetMapName(string name);

    public partial void SetPasswordProtected(bool protect);

    public partial void SetGameTags(string tags);

    public partial bool RequestUserGroupStatus(SteamID steamId, SteamID groupId);

    public partial void EndAuthSession(SteamID steamId);

    public partial void BUpdateUserData(SteamID steamId, string playerName, uint score);

    public partial UserHasLicenseForApp UserHasLicenseForApp(SteamID steamId, uint appId);

    public partial WorkshopItemState GetItemState(ulong nPublishedFileId);

    public partial bool GetItemDownloadInfo(ulong nPublishedFileId, out ulong punBytesDownloaded, out ulong punBytesTotal);

    public partial bool DownloadItem(ulong nPublishedFileId, bool bHighPriority);

    public partial bool DeleteItem(ulong nPublishedFileId);

    public partial void SuspendDownloads(bool state);

    public partial uint GetPublicIP();

    public partial nint GetSteamGameServer();

    public partial nint GetSteamUGC();
}
