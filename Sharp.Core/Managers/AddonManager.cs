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

using System;
using System.Runtime.InteropServices;
using Sharp.Core.Bridges.Natives;
using Sharp.Shared.Managers;
using Sharp.Shared.Units;

namespace Sharp.Core.Managers;

internal interface ICoreAddonManager : IAddonManager;

internal class AddonManager : ICoreAddonManager
{
    private static string[] SplitAddonCsv(nint csvPtr)
    {
        var csv = Marshal.PtrToStringUTF8(csvPtr);
        if (string.IsNullOrEmpty(csv))
            return Array.Empty<string>();
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public void DualAddonPurgeCheck()
        => Game.DualAddonPurgeCheck();

    public void DualAddonOverrideCheck(SteamID steamId, double time)
        => Game.DualAddonOverrideCheck(steamId, time);

    public ulong[] ExtraAddonGetIds()
    {
        var span = Game.ExtraAddonGetIds();
        return span.AsSpan().ToArray();
    }

    public string[] ExtraAddonGetServerAddons()
        => SplitAddonCsv(Game.ExtraAddonGetServerAddons());

    public string[] ExtraAddonGetGlobalClientAddons()
        => SplitAddonCsv(Game.ExtraAddonGetGlobalClientAddons());

    public string[] ExtraAddonGetMountedAddons()
        => SplitAddonCsv(Game.ExtraAddonGetMountedAddons());

    public string[] ExtraAddonGetClientAddons(SteamID steamId)
        => SplitAddonCsv(Game.ExtraAddonGetClientAddons(steamId));

    public string? ExtraAddonGetCurrentWorkshopMap()
        => Marshal.PtrToStringUTF8(Game.ExtraAddonGetCurrentWorkshopMap());

    public bool ExtraAddonAddAddon(string addon, bool refresh = false)
        => Game.ExtraAddonAddAddon(addon, refresh);

    public bool ExtraAddonRemoveAddon(string addon, bool refresh = false)
        => Game.ExtraAddonRemoveAddon(addon, refresh);

    public void ExtraAddonClearAddons()
        => Game.ExtraAddonClearAddons();

    public void ExtraAddonRefreshAddons(bool reloadMap = false)
        => Game.ExtraAddonRefreshAddons(reloadMap);

    public void ExtraAddonReloadMap()
        => Game.ExtraAddonReloadMap();

    public bool ExtraAddonMount(string addon, bool addToTail = false)
        => Game.ExtraAddonMount(addon, addToTail);

    public bool ExtraAddonUnmount(string addon)
        => Game.ExtraAddonUnmount(addon);

    public bool ExtraAddonIsMounted(string addon, bool checkWorkshopMap = false)
        => Game.ExtraAddonIsMounted(addon, checkWorkshopMap);

    public void ExtraAddonAddClientAddon(string addon, SteamID steamId = default, bool refresh = false)
        => Game.ExtraAddonAddClientAddon(addon, steamId, refresh);

    public void ExtraAddonRemoveClientAddon(string addon, SteamID steamId = default)
        => Game.ExtraAddonRemoveClientAddon(addon, steamId);

    public void ExtraAddonClearClientAddons(SteamID steamId = default)
        => Game.ExtraAddonClearClientAddons(steamId);

    public bool ExtraAddonDownload(string addon, bool important = false, bool force = false)
        => Game.ExtraAddonDownload(addon, important, force);

    public bool ExtraAddonHasUGCConnection()
        => Game.ExtraAddonHasUGCConnection();
}
