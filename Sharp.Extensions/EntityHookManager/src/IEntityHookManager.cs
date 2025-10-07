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

using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Sharp.Extensions.EntityHookManager;

public interface IEntityHookManager
{
    delegate void EventDelegate(string classname, IBaseEntity entity);

    delegate void WeaponEventDelegate(string classname, IBaseWeapon weapon);

    delegate void OutputDelegate(string classname,
        string                          output,
        IBaseEntity                     entity,
        IBaseEntity?                    activator,
        float                           delay,
        ref EHookAction                 result);

    delegate void InputDelegate(string classname,
        string                         input,
        in EntityVariant               value,
        IBaseEntity                    entity,
        IBaseEntity?                   activator,
        IBaseEntity?                   caller,
        ref EHookAction                result);

    void ListenEntityCreate(string classname, EventDelegate callback);

    void ListenEntityDelete(string classname, EventDelegate callback);

    void ListenEntitySpawn(string classname, EventDelegate callback);

    void ListenWeaponSpawn(WeaponEventDelegate callback);

    void HookEntityInput(string classname, string input, InputDelegate callback);

    void HookEntityOutput(string classname, string output, OutputDelegate callback);
}
