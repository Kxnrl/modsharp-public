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

using Sharp.Shared.Attributes;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameObjects;

[NetClass("CPlayer_UseServices")]
public interface IUseService : IPlayerPawnComponent
{
    /// <summary>
    ///     转换为CCSPlayer_UseServices
    /// </summary>
    /// <param name="reinterpret">
    ///     False: 如果当前UseService不是CCSPlayer_UseService则返回null<br />True:
    ///     使用指针重新解析为CCSPlayer_UseServices
    /// </param>
    IPlayerUseService? AsPlayerUseService(bool reinterpret = false);
}

[NetClass("CCSPlayer_UseServices")]
public interface IPlayerUseService : IUseService
{
    /// <summary>
    ///     m_hLastKnownUseEntity
    /// </summary>
    CEntityHandle<IBaseEntity> LastKnownUseEntityHandle { get; set; }

    /// <summary>
    ///     LastUseTimeStamp
    /// </summary>
    float LastUseTimeStamp { get; set; }

    /// <summary>
    ///     m_flTimeLastUsedWindow
    /// </summary>
    float TimeLastUsedWindow { get; set; }
}
