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

using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Units;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Sharp.Shared.Objects;

public interface IGameClient : INativeObject
{
    /// <summary>
    ///     输出到客户端控制台
    /// </summary>
    void ConsolePrint(string message);

    /// <summary>
    ///     覆盖游戏内名字
    /// </summary>
    void SetName(string name);

    /// <summary>
    ///     发生聊天消息
    /// </summary>
    void SayChatMessage(bool teamOnly, string message);

    /// <summary>
    ///     读取客户端IP地址
    /// </summary>
    /// <returns></returns>
    string? GetAddress(bool withPort);

    /// <summary>
    ///     获取客户端建立链接的时间
    /// </summary>
    float GetTimeConnected();

    /// <summary>
    ///     执行命令 (via Client/NetChannel)
    /// </summary>
    void Command(string command);

    /// <summary>
    ///     执行命令 (via GameClients)
    /// </summary>
    void FakeCommand(string command);

    /// <summary>
    ///     执行命令 (via Engine)
    /// </summary>
    void ExecuteStringCommand(string command);

    /// <summary>
    ///     强制发送FullUpdate
    /// </summary>
    void ForceFullUpdate();

    /// <summary>
    ///     获取客户端info类ConVar的值
    /// </summary>
    string? GetConVarValue(string cvarName);

    /// <summary>
    ///     获取客户端ConVars KeyValues对象
    /// </summary>
    IKeyValues? GetConVars();

    /// <summary>
    ///     获取<see cref="IPlayerController" /> <br />
    ///     <remarks>
    ///         如果玩家不在服务器内, 哪怕是实体存在, 也会返回<c>null</c>
    ///     </remarks>
    /// </summary>
    IPlayerController? GetPlayerController();

    /// <summary>
    ///     客户端SignOn状态
    /// </summary>
    SignOnState SignOnState { get; }

    /// <summary>
    ///     是否为FakeClient <br />
    ///     <remarks>Hltv / Bot</remarks>
    /// </summary>
    bool IsFakeClient { get; }

    /// <summary>
    ///     是否为Hltv
    /// </summary>
    bool IsHltv { get; }

    /// <summary>
    ///     UserId
    /// </summary>
    UserID UserId { get; }

    /// <summary>
    ///     SteamId (64位)
    /// </summary>
    SteamID SteamId { get; }

    /// <summary>
    ///     IClient的Engine Slot (PlayerSlot)
    /// </summary>
    PlayerSlot Slot { get; }

    /// <summary>
    ///     Controller的Index
    /// </summary>
    EntityIndex ControllerIndex { get; }

    /// <summary>
    ///     名字 <br />
    ///     <remarks>目前游戏里无法改名</remarks>
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     是否是完美世界用户
    /// </summary>
    bool PerfectWorld { get; }

    /// <summary>
    ///     是否已经被Steam服务器验证过SteamId
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    ///     当前指针的IClient是否有效
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    ///     当前指针的IClient是否已连线
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     当前指针的IClient是否已在游戏内
    /// </summary>
    bool IsInGame { get; }

    /// <summary>
    ///     网络存活时间 <br />
    ///     <remarks>对于玩家, 该值为进服到现在的时间 (换图不会重置)</remarks>
    ///     <br />
    ///     <remarks>对于Bot/Hltv, 该值永远为服务器启动到现在的时间</remarks>
    /// </summary>
    float TimeConnected { get; }

    /// <summary>
    ///     客户端的IP和端口 <br />
    ///     <remarks>对于Bot/Hltv, 该值永远为空</remarks>
    /// </summary>
    string? Address { get; }

    new int GetHashCode();

    new bool Equals(object? obj);
}
