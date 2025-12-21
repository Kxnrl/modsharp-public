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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Shared;

public interface IModSharp
{
#region Logs

    /// <summary>
    ///     Print log (Info)
    /// </summary>
    void LogMessage(string message);

    /// <summary>
    ///     Print log (Warning)
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    ///     Print log (custom color)
    /// </summary>
    void LogColorText(Color color, string text);

#endregion

#region Core

    /// <summary>
    ///     Engine error
    /// </summary>
    void FatalError(string message);

    /// <summary>
    ///     Get GameData
    /// </summary>
    /// <returns></returns>
    IGameData GetGameData();

    /// <summary>
    ///     Get gpGlobals
    /// </summary>
    IGlobalVars GetGlobals();

    /// <summary>
    ///     Get NetworkGameServer (sv)
    /// </summary>
    INetworkServer GetIServer();

    /// <summary>
    ///     Get Engine2Server (engine)
    /// </summary>
    nint GetIEngine();

    /// <summary>
    ///     Get GameRules (not available in <see cref="GameListener.OnGameShutdown"/>)
    /// </summary>
    IGameRules GetGameRules();

    /// <summary>
    ///     Get SteamApi SteamGameServer
    /// </summary>
    ISteamApi GetSteamGameServer();

    /// <summary>
    ///     Get NativeFunction
    /// </summary>
    /// <returns>Function pointer, can directly invoke unmanaged</returns>
    nint GetNativeFunctionPointer(string name);

    /// <summary>
    ///     Plat_FloatTime, also Source 1's EngineTime
    /// </summary>
    double EngineTime();

    /// <summary>
    ///     Invoke action on main thread <br />
    ///     <remarks>If currently on main thread, calls immediately, otherwise calls at end of current frame</remarks>
    /// </summary>
    void InvokeAction(Action action);

    /// <summary>
    ///     Invoke action at the end of current frame
    /// </summary>
    void InvokeFrameAction(Action action);

    /// <summary>
    ///     Invoke action at the end of current frame and wait
    /// </summary>
    Task<T> InvokeFrameActionAsync<T>(Func<T> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invoke action at the end of current frame and wait
    /// </summary>
    Task InvokeFrameActionAsync(Action action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Add timer to queue<br />
    /// </summary>
    Guid PushTimer(Action action, double interval, GameTimerFlags flags = GameTimerFlags.None);

    /// <summary>
    ///     Add timer to queue<br />
    /// </summary>
    Guid PushTimer(Func<TimerAction> action, double interval, GameTimerFlags flags = GameTimerFlags.None);

    /// <summary>
    ///     Remove timer from queue
    /// </summary>
    void StopTimer(Guid uniqueId);

    /// <summary>
    ///     Check if timer is in queue
    /// </summary>
    bool IsValidTimer(Guid uniqueId);

    /// <summary>
    ///     Get game absolute path
    /// </summary>
    string GetGamePath();

    /// <summary>
    ///     Install GameFrame Hook
    /// </summary>
    void InstallGameFrameHook(Action<bool, bool, bool>? pre,
        Action<bool, bool, bool>?                       post,
        int                                             prePriority  = 0,
        int                                             postPriority = 0);

    /// <summary>
    ///     Install EntityThink Hook
    /// </summary>
    void InstallEntityThinkHook(Action? pre, Action? post, int prePriority = 0, int postPriority = 0);

    /// <summary>
    ///     Install ServerGameSimulate Hook
    /// </summary>
    void InstallServerGameSimulateHook(Action callback, int priority = 0);

    /// <summary>
    ///     Remove GameFrame Hook
    /// </summary>
    void RemoveGameFrameHook(Action<bool, bool, bool>? pre, Action<bool, bool, bool>? post);

    /// <summary>
    ///     Remove EntityThink Hook
    /// </summary>
    void RemoveEntityThinkHook(Action? pre, Action? post);

    /// <summary>
    ///     Remove ServerGameSimulate Hook
    /// </summary>
    void RemoveServerGameSimulateHook(Action callback);

    /// <summary>
    ///     Returns the MurmurHash2 value of the given string, case-insensitive
    /// </summary>
    uint MakeStringToken(string str);

#endregion

#region UserMessage

    /// <summary>
    ///     Send chat message to all players
    /// </summary>
    void PrintToChatAll(string message);

    /// <summary>
    ///     Send chat message to specific team
    /// </summary>
    void PrintToChat(CStrikeTeam team, string message);

    /// <summary>
    ///     Send message to all players on specified channel
    /// </summary>
    void PrintChannelAll(HudPrintChannel channel, string message);

    /// <summary>
    ///     Send message to filtered players on specified channel
    /// </summary>
    void PrintChannelFilter(HudPrintChannel channel, string message, RecipientFilter receiver);

    /// <summary>
    ///     Send message to team players on specified channel
    /// </summary>
    void PrintChannelTeam(HudPrintChannel channel, CStrikeTeam team, string message);

    /// <summary>
    ///     Radio message (Team)
    /// </summary>
    void RadioTextTeam(CStrikeTeam team,
        PlayerSlot                 slot,
        string                     name,
        string?                    params1,
        string?                    params2,
        string?                    params3,
        string?                    params4);

    /// <summary>
    ///     Radio message
    /// </summary>
    void RadioTextAll(PlayerSlot slot, string name, string? params1, string? params2, string? params3, string? params4);

    /// <summary>
    ///     Send NetMessage
    /// </summary>
    bool SendNetMessage<T>(RecipientFilter filter, T data) where T : IMessage;

    /// <summary>
    ///     Install HookNetMessage
    /// </summary>
    void HookNetMessage(ProtobufNetMessageType msgId);

    /// <summary>
    ///     Remove HookNetMessage
    /// </summary>
    void UnhookNetMessage(ProtobufNetMessageType msgId);

#endregion

#region Engine

    /// <summary>
    ///     Check if the command line argument exists
    /// </summary>
    bool HasCommandLine(string key);

    /// <summary>
    ///     Get value for a specific command line argument
    /// </summary>
    T? GetCommandLine<T>(string key) where T : IParsable<T>;

    /// <summary>
    ///     Get command line argument value as string
    /// </summary>
    string? GetCommandLine(string key);

    /// <summary>
    ///     Precache resource
    /// </summary>
    void PrecacheResource(string resource);

    /// <summary>
    ///     Change map
    /// </summary>
    void ChangeLevel(string map);

    /// <summary>
    ///     Check if map is valid
    /// </summary>
    bool IsMapValid(string map);

    /// <summary>
    ///     Get map list for given map group
    ///     <remarks>Returns null if map group is invalid</remarks>
    /// </summary>
    /// <param name="mapGroup">e.g. 'workshop'</param>
    List<string>? GetMapGroupMapList(string mapGroup);

    /// <summary>
    ///     Read game resource data buffer
    ///     <remarks>Returns null if file is invalid or fails to load</remarks>
    /// </summary>
    /// <param name="filePath">File name, e.g. 'scripts/weapons.vdata_c'. '_c' will be added automatically if missing</param>
    /// <param name="pathId">Path ID, e.g. 'GAME'</param>
    byte[]? FindResourceDataBlockInfo(string filePath, string pathId);

    /// <summary>
    ///     Get resource status
    ///     <remarks>Returns Resident if resource is loaded</remarks>
    /// </summary>
    /// <param name="filePath">File name, e.g. 'scripts/weapons.vdata'. Must not end with '_c'</param>
    ResourceStatus GetResourceStatus(string filePath);

    /// <summary>
    ///     Execute server command
    ///     <remarks>Commands are buffered and not executed immediately. Too many commands can stop the execution</remarks>
    /// </summary>
    void ServerCommand(string command);

    /// <summary>
    ///     Get currently loaded addon IDs
    ///     <remarks>Returns null if failed or no addons loaded, otherwise returns format: addonId1,addonId2</remarks>
    /// </summary>
    string? GetAddonName();

    /// <summary>
    ///     Get map BSP name, e.g. de_mirage, surf_beginner
    ///     <remarks>Returns null if failed or no map loaded</remarks>
    /// </summary>
    string? GetMapName();

#endregion

#region Effects

    /// <summary>
    ///     Dispatches a particle effect at a specific world location
    /// </summary>
    int DispatchParticleEffect(string particle, Vector origin, Vector angles, RecipientFilter filter = default);

    /// <summary>
    ///     Dispatches a particle effect relative to an entity
    /// </summary>
    int DispatchParticleEffect(string particle,
        IBaseEntity                   entity,
        Vector                        origin,
        Vector                        angles,
        bool                          resetEntity = false,
        RecipientFilter               filter      = default);

    /// <summary>
    ///     Dispatches a particle effect attached to a specific point on an entity
    /// </summary>
    int DispatchParticleEffect(string particle,
        ParticleAttachmentType        attachType,
        IBaseEntity                   entity,
        byte                          attachmentIndex = 0,
        bool                          resetEntity     = false,
        RecipientFilter               filter          = default);

#endregion

#region Listener

    /// <summary>
    ///     Install <see cref="IGameListener"/> to listen for game events
    /// </summary>
    void InstallGameListener(IGameListener listener);

    /// <summary>
    ///     Remove <see cref="IGameListener"/>
    /// </summary>
    void RemoveGameListener(IGameListener listener);

    /// <summary>
    ///     Install <see cref="ISteamListener"/> to listen for Steam events
    /// </summary>
    void InstallSteamListener(ISteamListener listener);

    /// <summary>
    ///     Remove <see cref="ISteamListener"/>
    /// </summary>
    void RemoveSteamListener(ISteamListener listener);

#endregion

#region Native

    /// <summary>
    ///     Create native object from pointer
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="ptr">Object pointer</param>
    T? CreateNativeObject<T>(nint ptr) where T : class, INativeObject;

    /// <summary>
    ///     Find pattern in module, invalid module will cause FatalError
    /// </summary>
    /// <param name="module">DLL/SO name</param>
    /// <param name="pattern">IDA pattern</param>
    nint FindPattern(string module, string pattern);

    /// <summary>
    ///     Get virtual table for specified C++ class
    /// </summary>
    /// <param name="module">DLL/SO name</param>
    /// <param name="className">C++ class name</param>
    /// <returns></returns>
    nint GetVTableByClass(string module, string className);

    /// <summary>
    ///     Create KeyValues
    /// </summary>
    IKeyValues CreateKeyValues(string name);

    /// <summary>
    ///     Create KeyValues3
    /// </summary>
    IKeyValues3 CreateKeyValues3(KeyValues3Type type, KeyValues3SubType subType);

    /// <summary>
    ///     Get DLL/SO library module
    /// </summary>
    ILibraryModule GetLibraryModule(string module);

    /// <summary>
    ///     Find GameSystem by name
    /// </summary>
    IGameSystem? FindGameSystemByName(string name);

    /// <summary>
    ///     Find Valve interface
    /// </summary>
    nint FindValveInterface(string module, string name);

    /// <summary>
    ///     Set protected memory access
    /// </summary>
    bool SetMemoryAccess(nint pMemory, long size, MemoryAccess access);

    /// <summary>
    ///     Get memory allocator
    /// </summary>
    /// <returns></returns>
    IMemAlloc GetMemAlloc();

    /// <summary>
    ///     Find weapon VData by name
    ///     <remarks>
    ///         The VData found here is read-only and cannot be modified.
    ///     </remarks>
    /// </summary>
    /// <returns>IWeaponData, returns null if not loaded or not found</returns>
    IWeaponData? FindWeaponVDataByName(string name);

#endregion

#region Dual Addon

    /// <summary>
    ///     Clear dual addon cache
    /// </summary>
    void DualAddonPurgeCheck();

    /// <summary>
    ///     Override cache for a player
    /// </summary>
    void DualAddonOverrideCheck(SteamID steamId, double time);

#endregion

#region Workshop

    /// <summary>
    ///     Add a workshop map to the server's map list
    /// </summary>
    /// <param name="sharedFileId">Workshop FileId</param>
    /// <param name="mapName">Map (case sensitive)</param>
    /// <param name="path">Absolute path of vpk file</param>
    bool AddWorkshopMap(ulong sharedFileId, string mapName, string path);

    /// <summary>
    ///     Is map loaded by dedicate server workshop manager
    /// </summary>
    bool WorkshopMapExists(ulong sharedFileId);

    /// <summary>
    ///     Remove map from dedicate server workshop manager
    /// </summary>
    /// <param name="sharedFileId">Workshop FileId</param>
    bool RemoveWorkshopMap(ulong sharedFileId);

    /// <summary>
    ///     List all workshop maps added to the server's map list
    /// </summary>
    List<(ulong PublishFileId, string Name)> ListWorkshopMaps();

#endregion
}
