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

using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Core.GameObjects;

internal partial class UseService : PlayerPawnComponent, IUseService
{
    public override string GetSchemaClassname()
        => "CPlayer_UseServices";

    public IPlayerUseService? AsPlayerUseService(bool reinterpret = false)
    {
        if (GetPlayerUseService() is { } us)
        {
            return us;
        }

        return reinterpret ? PlayerUseService.Create(_this) : null;
    }

    protected virtual IPlayerUseService? GetPlayerUseService()
        => null;
}

internal partial class PlayerUseService : UseService, IPlayerUseService
{
#region Schemas

    [NativeSchemaField("CCSPlayer_UseServices", "m_hLastKnownUseEntity", typeof(CEntityHandle<IBaseEntity>))]
    private partial SchemaField GetLastKnownUseEntityHandleField();

    [NativeSchemaField("CCSPlayer_UseServices", "m_flLastUseTimeStamp", typeof(float))]
    private partial SchemaField GetLastUseTimeStampField();

    [NativeSchemaField("CCSPlayer_UseServices", "m_flTimeLastUsedWindow", typeof(float))]
    private partial SchemaField GetTimeLastUsedWindowField();

#endregion

    public override string GetSchemaClassname()
        => "CCSPlayer_UseServices";

    protected override IPlayerUseService? GetPlayerUseService()
        => this;
}