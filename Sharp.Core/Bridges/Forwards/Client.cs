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

using System.Runtime.InteropServices;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;
using Sharp.Shared.Units;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Bridges.Forwards;

internal static class Client
{
    public delegate HookReturnValue<NetworkDisconnectionReason> DelegateOnConnectClient(SteamID steamId, string name, uint ip);

    public delegate HookReturnValue<bool> DelegateOnClientConnect(SteamID steamId, string name, ref string rejectReason);

    public delegate void DelegateOnClientConnected(nint ptr);

    public delegate void DelegateOnClientActive(nint ptr);

    public delegate void DelegateOnClientDisconnecting(nint ptr, NetworkDisconnectionReason reason);

    public delegate void DelegateOnClientDisconnected(nint ptr, NetworkDisconnectionReason reason);

    public delegate void DelegateOnClientSettingChanged(nint ptr);

    public delegate ECommandAction DelegateOnClientStringCommand(nint ptr,
        string                                                        command,
        bool                                                          chatTrigger,
        string?                                                       arguments);

    public delegate ECommandAction DelegateOnClientSayCommand(nint ptr, bool teamOnly, string command, string message);

    public delegate HookReturnValue<bool> DelegateOnClientAllowHear(nint ptr, nint source);

    public delegate ECommandAction DelegateOnClientConsoleCommand(nint ptr, string command, string? arguments);

    public delegate EHookAction DelegateClientSpeakPre(nint ptr, ulong xuid, uint sectionNumber, nint pVoiceData, int size);

    public delegate void DelegateClientSpeakPost(nint ptr,
        ulong                                         xuid,
        uint                                          sectionNumber,
        nint                                          pVoiceData,
        int                                           size,
        EHookAction                                   action);

    public delegate void DelegateOnClientQueryConVar(nint ptr,
        int                                               cookie,
        QueryConVarValueStatus                            status,
        string                                            name,
        string                                            value);

    public static event DelegateOnConnectClient?        OnConnectClient;
    public static event DelegateOnClientConnect?        OnClientConnect;
    public static event DelegateOnClientConnected?      OnClientConnected;
    public static event DelegateOnClientActive?         OnClientActive;
    public static event DelegateOnClientDisconnecting?  OnClientDisconnecting;
    public static event DelegateOnClientDisconnected?   OnClientDisconnected;
    public static event DelegateOnClientSettingChanged? OnClientSettingChanged;
    public static event DelegateOnClientStringCommand?  OnClientStringCommand;
    public static event DelegateOnClientSayCommand?     OnClientSayCommand;
    public static event DelegateOnClientAllowHear?      OnClientAllowHear;
    public static event DelegateOnClientConsoleCommand? OnClientConsoleCommand;
    public static event DelegateClientSpeakPre?         OnClientSpeakPre;
    public static event DelegateClientSpeakPost?        OnClientSpeakPost;
    public static event DelegateOnClientQueryConVar?    OnClientQueryConVar;

    [UnmanagedCallersOnly]
    public static unsafe EHookAction OnConnectClientExport(SteamID steamId, sbyte* pName, nint pNetInfo, uint ip)
    {
        if (OnConnectClient is null)
        {
            return EHookAction.Ignored;
        }

        var name   = Utils.ReadString(pName);
        var action = OnConnectClient.Invoke(steamId, name, ip);

        if (action.Action is EHookAction.SkipCallReturnOverride)
        {
            Natives.Core.RejectConnection(pNetInfo, action.ReturnValue);
        }

        return action.Action;
    }

    [UnmanagedCallersOnly]
    public static unsafe EHookAction OnClientConnectExport(SteamID steamId,
        sbyte*                                                     pName,
        sbyte*                                                     pRejectReason,
        int                                                        len,
        bool*                                                      returnValue)
    {
        if (OnClientConnect is null)
        {
            return EHookAction.Ignored;
        }

        var name   = Utils.ReadString(pName);
        var reason = string.Empty;
        var action = OnClientConnect.Invoke(steamId, name, ref reason);

        *returnValue = action.ReturnValue;
        Utils.WriteString(pRejectReason, len, reason);

        return action.Action;
    }

    [UnmanagedCallersOnly]
    public static void OnClientConnectedExport(nint ptr)
        => OnClientConnected?.Invoke(ptr);

    [UnmanagedCallersOnly]
    public static void OnClientActiveExport(nint ptr)
        => OnClientActive?.Invoke(ptr);

    [UnmanagedCallersOnly]
    public static void OnClientDisconnectingExport(nint ptr, NetworkDisconnectionReason reason)
        => OnClientDisconnecting?.Invoke(ptr, reason);

    [UnmanagedCallersOnly]
    public static void OnClientDisconnectedExport(nint ptr, NetworkDisconnectionReason reason)
        => OnClientDisconnected?.Invoke(ptr, reason);

    [UnmanagedCallersOnly]
    public static void OnClientSettingChangedExport(nint ptr)
        => OnClientSettingChanged?.Invoke(ptr);

    [UnmanagedCallersOnly]
    public static unsafe ECommandAction OnClientStringCommandExport(nint ptr,
        sbyte*                                                           pCommand,
        byte                                                             bChatTrigger,
        sbyte*                                                           pArguments)
    {
        if (OnClientStringCommand is null || pCommand == null)
        {
            return bChatTrigger != 0 ? ECommandAction.Stopped : ECommandAction.Skipped;
        }

        var command   = Utils.ReadString(pCommand);
        var arguments = pArguments == null ? null : Utils.ReadString(pArguments);

        return OnClientStringCommand.Invoke(ptr, command, bChatTrigger != 0, arguments);
    }

    [UnmanagedCallersOnly]
    public static unsafe ECommandAction OnClientSayCommandExport(nint ptr, bool bTeamOnly, sbyte* pCommand, sbyte* pMessage)
    {
        if (pCommand == null || pMessage == null || OnClientSayCommand is null)
        {
            return ECommandAction.Skipped;
        }

        var command = Utils.ReadString(pCommand);
        var message = Utils.ReadString(pMessage);

        return OnClientSayCommand.Invoke(ptr, bTeamOnly, command, message);
    }

    [UnmanagedCallersOnly]
    public static unsafe EHookAction OnClientAllowHearExport(nint ptr, nint ptrSource, bool* returnValue)
    {
        if (OnClientAllowHear is null)
        {
            return EHookAction.Ignored;
        }

        var action = OnClientAllowHear.Invoke(ptr, ptrSource);

        *returnValue = action.ReturnValue;

        return action.Action;
    }

    [UnmanagedCallersOnly]
    public static unsafe ECommandAction OnClientConsoleCommandExport(nint ptr, sbyte* pCommand, sbyte* pArguments)
    {
        if (OnClientConsoleCommand is null || pCommand == null)
        {
            return ECommandAction.Skipped;
        }

        var command   = Utils.ReadString(pCommand);
        var arguments = pArguments == null ? null : Utils.ReadString(pArguments);

        return OnClientConsoleCommand.Invoke(ptr, command, arguments);
    }

    [UnmanagedCallersOnly]
    public static EHookAction OnClientSpeakPreExport(nint ptr, ulong xuid, uint sectionNumber, nint pVoiceData, int size)
        => OnClientSpeakPre?.Invoke(ptr, xuid, sectionNumber, pVoiceData, size) ?? EHookAction.Ignored;

    [UnmanagedCallersOnly]
    public static void OnClientSpeakPostExport(nint ptr,
        ulong                                       xuid,
        uint                                        sectionNumber,
        nint                                        pVoiceData,
        int                                         size,
        EHookAction                                 action)
        => OnClientSpeakPost?.Invoke(ptr, xuid, sectionNumber, pVoiceData, size, action);

    [UnmanagedCallersOnly]
    public static unsafe void OnClientQueryConVarExport(nint ptr,
        int                                                  cookie,
        QueryConVarValueStatus                               status,
        sbyte*                                               pName,
        sbyte*                                               pValue)
    {
        if (OnClientQueryConVar is null)
        {
            return;
        }

        var name  = Utils.ReadString(pName);
        var value = Utils.ReadString(pValue);

        OnClientQueryConVar.Invoke(ptr, cookie, status, name, value);
    }
}
