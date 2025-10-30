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
    ///     打印日志 (Info)
    /// </summary>
    void LogMessage(string message);

    /// <summary>
    ///     打印日志 (Warning)
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    ///     打印日志 (自定义彩色)
    /// </summary>
    void LogColorText(Color color, string text);

#endregion

#region Core

    /// <summary>
    ///     直接Engine Error
    /// </summary>
    void FatalError(string message);

    /// <summary>
    ///     获取 GameData
    /// </summary>
    /// <returns></returns>
    IGameData GetGameData();

    /// <summary>
    ///     获取gpGlobals
    /// </summary>
    IGlobalVars GetGlobals();

    /// <summary>
    ///     获取NetworkGameServer (sv)
    /// </summary>
    INetworkServer GetIServer();

    /// <summary>
    ///     获取Engine2Server (engine)
    /// </summary>
    nint GetIEngine();

    /// <summary>
    ///     获取GameRules
    /// </summary>
    IGameRules GetGameRules();

    /// <summary>
    ///     获取SteamApi SteamGameServer
    /// </summary>
    ISteamApi GetSteamGameServer();

    /// <summary>
    ///     获取NativeFunction
    /// </summary>
    /// <returns>Func 指针, 可以直接Invoke unmanaged</returns>
    nint GetNativeFunctionPointer(string name);

    /// <summary>
    ///     Plat_FloatTime 也是Source 1的EngineTime
    /// </summary>
    double EngineTime();

    /// <summary>
    ///     在主线程调用Action <br />
    ///     <remarks>如果当前在主线程则立即调用, 否则会在当前帧末尾调用</remarks>
    /// </summary>
    void InvokeAction(Action action);

    /// <summary>
    ///     在当前帧末尾调用Action
    /// </summary>
    void InvokeFrameAction(Action action);

    /// <summary>
    ///     在当前帧末尾调用Action并等待
    /// </summary>
    Task<T> InvokeFrameActionAsync<T>(Func<T> action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     在当前帧末尾调用Action并等待
    /// </summary>
    Task InvokeFrameActionAsync(Action action, CancellationToken cancellationToken = default);

    /// <summary>
    ///     把Timer加入队列<br />
    /// </summary>
    Guid PushTimer(Action action, double interval, GameTimerFlags flags = GameTimerFlags.None);

    /// <summary>
    ///     把Timer加入队列<br />
    /// </summary>
    Guid PushTimer(Func<TimerAction> action, double interval, GameTimerFlags flags = GameTimerFlags.None);

    /// <summary>
    ///     把Timer移除队列
    /// </summary>
    void StopTimer(Guid uniqueId);

    /// <summary>
    ///     判断Timer是否在队列中
    /// </summary>
    bool IsValidTimer(Guid uniqueId);

    /// <summary>
    ///     获取游戏绝对路径
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
    ///     返回给的字符串的MurmurHash2值,不区分大小写
    /// </summary>
    uint MakeStringToken(string str);

#endregion

#region UserMessage

    /// <summary>
    ///     打印到所有玩家聊天框 (Chat)
    /// </summary>
    void PrintToChatAll(string message);

    /// <summary>
    ///     打印到对应队伍玩家聊天框 (Chat)
    /// </summary>
    void PrintToChat(CStrikeTeam team, string message);

    /// <summary>
    ///     打印到所有玩家的对应频道
    /// </summary>
    void PrintChannelAll(HudPrintChannel channel, string message);

    /// <summary>
    ///     打印到对应玩家的对应频道
    /// </summary>
    void PrintChannelFilter(HudPrintChannel channel, string message, RecipientFilter receiver);

    /// <summary>
    ///     打印到对应队伍玩家的对应频道
    /// </summary>
    void PrintChannelTeam(HudPrintChannel channel, CStrikeTeam team, string message);

    /// <summary>
    ///     无线电消息 (Team)
    /// </summary>
    void RadioTextTeam(CStrikeTeam team,
        PlayerSlot                 slot,
        string                     name,
        string?                    params1,
        string?                    params2,
        string?                    params3,
        string?                    params4);

    /// <summary>
    ///     无线电消息
    /// </summary>
    void RadioTextAll(PlayerSlot slot, string name, string? params1, string? params2, string? params3, string? params4);

    /// <summary>
    ///     发送NetMessage
    /// </summary>
    [Obsolete("Use SendNetMessage<T> instead, will remove in 2.1")]
    bool SendNetMessage(RecipientFilter filter, string name, IMessage data);

    /// <summary>
    ///     发送NetMessage
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
    ///     是否存在某个启动参数
    /// </summary>
    bool HasCommandLine(string key);

    /// <summary>
    ///     获取某个启动参数的值
    /// </summary>
    T? GetCommandLine<T>(string key) where T : IParsable<T>;

    /// <summary>
    ///     获取某个启动参数的值 (String)
    /// </summary>
    string? GetCommandLine(string key);

    /// <summary>
    ///     预加载资源 <br />
    /// </summary>
    void PrecacheResource(string resource);

    /// <summary>
    ///     更换地图
    /// </summary>
    void ChangeLevel(string map);

    /// <summary>
    ///     地图是否是有效的地图
    /// </summary>
    bool IsMapValid(string map);

    /// <summary>
    ///     读取地图列表 <br />
    ///     <remarks>返回null时为该地图组无效</remarks>
    /// </summary>
    /// <param name="mapGroup">例如 'workshop'</param>
    List<string>? GetMapGroupMapList(string mapGroup);

    /// <summary>
    ///     读取游戏资源buffer <br />
    ///     <remarks>如果文件无效或者加载失败时返回null</remarks>
    /// </summary>
    /// <param name="filePath">文件名,比如 'scripts/weapons.vdata_c'. 如果不带 '_c' 将会自动加上</param>
    /// <param name="pathId">路径ID,比如 'GAME'</param>
    byte[]? FindResourceDataBlockInfo(string filePath, string pathId);

    /// <summary>
    ///     获取指定资源状态 <br />
    ///     <remarks>如果返回Resident则说明资源已加载</remarks>
    /// </summary>
    /// <param name="filePath">文件名,比如 'scripts/weapons.vdata'. 末尾不能带_c</param>
    ResourceStatus GetResourceStatus(string filePath);

    /// <summary>
    ///     调用ServerCommand
    /// </summary>
    void ServerCommand(string command);

    /// <summary>
    ///     获取当前加载的AddonId
    ///     <remarks>获取失败或者没有加载则返回null, 加载上插件后返回为 addonId1,addonId2 </remarks>
    /// </summary>
    string? GetAddonName();

    /// <summary>
    ///     获取地图bsp名字, 如 de_mirage, surf_beginner
    ///     <remarks>获取失败或者没有加载则返回null</remarks>
    /// </summary>
    string? GetMapName();

#endregion

#region Effects

    /// <summary>
    ///     发送粒子特效
    /// </summary>
    int DispatchParticleEffect(string particle, Vector origin, Vector angles, RecipientFilter filter = default);

    /// <summary>
    ///     发送粒子特效
    /// </summary>
    int DispatchParticleEffect(string particle,
        IBaseEntity                   entity,
        Vector                        origin,
        Vector                        angles,
        bool                          resetEntity = false,
        RecipientFilter               filter      = default);

    /// <summary>
    ///     发送粒子特效
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
    ///     添加GameListener以监听事件
    /// </summary>
    void InstallGameListener(IGameListener listener);

    /// <summary>
    ///     移除GameListener
    /// </summary>
    void RemoveGameListener(IGameListener listener);

    /// <summary>
    ///     添加SteamListener以监听事件
    /// </summary>
    void InstallSteamListener(ISteamListener listener);

    /// <summary>
    ///     移除SteamListener
    /// </summary>
    void RemoveSteamListener(ISteamListener listener);

#endregion

#region Native

    /// <summary>
    ///     从指针创建NativeObject
    /// </summary>
    /// <typeparam name="T">Object类型</typeparam>
    /// <param name="ptr">Object指针</param>
    T? CreateNativeObject<T>(nint ptr) where T : class, INativeObject;

    /// <summary>
    ///     FindPattern, 无效的module会直接FatalError
    /// </summary>
    /// <param name="module">dll/so名字</param>
    /// <param name="pattern">IDA Pattern</param>
    nint FindPattern(string module, string pattern);

    /// <summary>
    ///     查找指定C++ Class的虚表
    /// </summary>
    /// <param name="module">dll/so名字</param>
    /// <param name="className">C++ Class名</param>
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
    ///     获取DLL/SO
    /// </summary>
    ILibraryModule GetLibraryModule(string module);

    /// <summary>
    ///     通过名称查找GameSystem
    /// </summary>
    IGameSystem? FindGameSystemByName(string name);

    /// <summary>
    ///     查找Valve Interface
    /// </summary>
    nint FindValveInterface(string module, string name);

    /// <summary>
    ///     Set protected memory access
    /// </summary>
    bool SetMemoryAccess(nint pMemory, long size, MemoryAccess access);

    /// <summary>
    ///     获取MemAlloc
    /// </summary>
    /// <returns></returns>
    IMemAlloc GetMemAlloc();

    /// <summary>
    ///     通过武器名字获取VData <br />
    ///     <remarks>
    ///         这里找到的VData只能读不能写。
    ///     </remarks>
    /// </summary>
    /// <returns>IWeaponData, 如果未加载或者找不到则返回null</returns>
    IWeaponData? FindWeaponVDataByName(string name);

#endregion

#region Dual Addon

    /// <summary>
    ///     清空缓存
    /// </summary>
    void DualAddonPurgeCheck();

    /// <summary>
    ///     复写Dual Addon缓存
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
