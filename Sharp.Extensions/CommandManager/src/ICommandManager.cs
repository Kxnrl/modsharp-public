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
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Extensions.CommandManager;

public interface ICommandManager
{
    public delegate void DelegateOnServerCommand(StringCommand command);

    public delegate void DelegateOnClientCommand(IGameClient client, StringCommand command);

    /// <summary>
    ///     注册仅限客户端的<b>控制台</b>和<b>聊天框</b>命令 <br />
    ///     <remarks>控制台会自动加上<c>ms_</c>前缀</remarks>
    /// </summary>
    void RegisterClientCommand(string command, DelegateOnClientCommand callback);

    /// <summary>
    ///     注册仅限客户端且需要管理员权限的<b>控制台</b>和<b>聊天框</b>命令 <br />
    ///     <remarks>控制台会自动加上<c>ms_</c>前缀</remarks>
    /// </summary>
    [Obsolete("This has been merged into AdminManager. Will be removed at 2.2")]
    void RegisterAdminCommand(string command, DelegateOnClientCommand callback, string permission);

    /// <summary>
    ///     注册仅限服务端控制台执行的命令
    /// </summary>
    void RegisterServerCommand(string command, string description, DelegateOnServerCommand callback);
}
