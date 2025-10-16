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
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Shared.Managers;

public interface IConVarManager
{
    /// <summary>
    ///     ConVar值变化时调用的回调 <br />
    ///     <remarks>在当前Callback内调用值的修改会立即StackOverflow</remarks>
    /// </summary>
    public delegate void DelegateConVarChange(IConVar conVar);

    /// <summary>
    ///     搜索某个已存在的ConVar
    /// </summary>
    /// <param name="name">参数名字 (匹配大小写)</param>
    /// <param name="useIterator">是否使用迭代器模式, 会比直接查找更慢, 但是可以找到隐藏的ConVar</param>
    IConVar? FindConVar(string name, bool useIterator = false);

    /// <summary>
    ///     监听某个ConVar的值发生变化 <br />
    ///     <remarks>在Callback内调用值的修改会立即StackOverflow</remarks>
    /// </summary>
    void InstallChangeHook(IConVar conVar, DelegateConVarChange callback);

    /// <summary>
    ///     移除ConVar监听
    /// </summary>
    void RemoveChangeHook(IConVar conVar, DelegateConVarChange callback);

    IConVar? CreateConVar(string name,
        bool                     defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        short                    defaultValue,
        short                    min,
        short                    max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        short                    defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        ushort                   defaultValue,
        ushort                   min,
        ushort                   max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        ushort                   defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        int                      defaultValue,
        int                      min,
        int                      max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        int                      defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        uint                     defaultValue,
        uint                     min,
        uint                     max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        uint                     defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        long                     defaultValue,
        long                     min,
        long                     max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        long                     defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        ulong                    defaultValue,
        ulong                    min,
        ulong                    max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        ulong                    defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        float                    defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        float                    defaultValue,
        float                    min,
        float                    max,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    IConVar? CreateConVar(string name,
        string                   defaultValue,
        string?                  helpString = null,
        ConVarFlags?             flags      = null);

    /// <summary>
    ///     创建一个控制台命令, 可以被客户端和服务器调用, 如果你想创建 <br />
    ///     <remarks>
    ///         高性能的, <br />
    ///         只允许客户端执行的, <br />
    ///         同时支持控制台和聊天窗口触发的<br />
    ///         命令, 那么请使用 <see cref="IClientManager.InstallCommandCallback" />
    ///     </remarks>
    /// </summary>
    void CreateConsoleCommand(string                      name,
        Func<IGameClient?, StringCommand, ECommandAction> fn,
        string?                                           description = null,
        ConVarFlags?                                      flags       = null);

    /// <summary>
    ///     创建一个仅服务器控制台可用的命令
    /// </summary>
    void CreateServerCommand(string         name,
        Func<StringCommand, ECommandAction> fn,
        string?                             description = null,
        ConVarFlags?                        flags       = null);

    /// <summary>
    ///     卸载时需要调用, 否则发生泄漏
    /// </summary>
    bool ReleaseCommand(string name);
}
