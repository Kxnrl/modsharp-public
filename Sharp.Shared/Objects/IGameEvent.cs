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
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Sharp.Shared.Objects;

public interface IGameEvent : INativeObject
{
    /// <summary>
    ///     设置事件字段的值
    /// </summary>
    void SetString(string key, string value);

    /// <summary>
    ///     设置事件字段的值
    /// </summary>
    void SetFloat(string key, float value);

    /// <summary>
    ///     设置事件字段的值
    /// </summary>
    void SetInt(string key, int value);

    /// <summary>
    ///     设置事件字段的值
    /// </summary>
    void SetUInt64(string key, ulong value);

    /// <summary>
    ///     设置事件字段的值
    /// </summary>
    void SetPlayer(string key, IPlayerController controller);

    /// <summary>
    ///     设置事件字段的值
    /// </summary>
    void SetPlayer(string key, IPlayerPawn pawn);

    /// <summary>
    ///     设置事件字段的值
    /// </summary>
    void SetPlayer(string key, int slot);

    /// <summary>
    ///     设置事件字段的值
    /// </summary>
    void SetBool(string key, bool value);

    /// <summary>
    ///     获取事件字段的值
    /// </summary>
    bool GetBool(string key);

    /// <summary>
    ///     获取事件字段的值
    /// </summary>
    string GetString(string key, string defaultValue = "");

    /// <summary>
    ///     获取事件字段的值
    /// </summary>
    float GetFloat(string key, float defaultValue = 0.0f);

    /// <summary>
    ///     获取事件字段的值
    /// </summary>
    int GetInt(string key, int defaultValue = 0);

    /// <summary>
    ///     获取事件字段的值
    /// </summary>
    ulong GetUInt64(string key, ulong defaultValue = 0);

    /// <summary>
    ///     获取事件字段的PlayerController
    /// </summary>
    IPlayerController? GetPlayerController(string key);

    /// <summary>
    ///     获取事件字段的PlayerPawn
    ///     <remarks>
    ///         <br />若实体本身为Observer这里也会返回null
    ///     </remarks>
    /// </summary>
    IPlayerPawn? GetPlayerPawn(string key);

    /// <summary>
    ///     与游戏原本行为相符, 取BasePlayerPawn
    /// </summary>
    IBasePlayerPawn? GetBasePlayerPawn(string key);

    /// <summary>
    ///     获取事件的名称
    /// </summary>
    /// <returns></returns>
    string GetName();

    /// <summary>
    ///     发射事件 <br />
    ///     <remarks>在非自主创建的Event上调用时会直接抛出异常</remarks>
    /// </summary>
    /// <param name="serverOnly">不发送给客户端</param>
    void Fire(bool serverOnly);

    /// <summary>
    ///     发射事件给客户端 <br />
    ///     <remarks>在非自主创建的Event上调用时会直接抛出异常</remarks>
    /// </summary>
    /// <param name="slot">IGameClient index</param>
    void FireToClient(int slot);

    /// <summary>
    ///     发射事件给客户端 <br />
    ///     <remarks>在非自主创建的Event上调用时会直接抛出异常</remarks>
    /// </summary>
    /// <param name="client">IGameClient</param>
    void FireToClient(IGameClient client);

    /// <summary>
    ///     发射事件给全体客户端<br />
    ///     <remarks>在非自主创建的Event上调用时会直接抛出异常</remarks>
    /// </summary>
    void FireToClients();

    /// <summary>
    ///     销毁事件 <br />
    ///     <remarks>在非自主创建的Event上调用时会直接抛出异常</remarks>
    ///     <br />
    ///     <remarks>在已经被发射的事件上调用时会立即崩溃</remarks>
    /// </summary>
    void Dispose();

    /// <summary>
    ///     事件名
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     是否允许修改值/发射
    /// </summary>
    bool Editable { get; }

    /// <summary>
    ///     使用索引获取值
    /// </summary>
    object this[string key] { set; }

    /// <summary>
    ///     获取事件的值并转换为Enum
    /// </summary>
    T Get<T>(string key, int defaultValue = 0) where T : Enum;
}
