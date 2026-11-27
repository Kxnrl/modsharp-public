/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
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

    /// <summary>
    ///     Listen for entity creation events matching <paramref name="classname" /> (case-insensitive).
    /// </summary>
    /// <param name="classname">Target entity classname.</param>
    /// <param name="callback">Callback invoked when a matching entity is created.</param>
    void ListenEntityCreate(string classname, EventDelegate callback);

    /// <summary>
    ///     Listen for entity deletion events matching <paramref name="classname" /> (case-insensitive).
    /// </summary>
    /// <param name="classname">Target entity classname.</param>
    /// <param name="callback">Callback invoked when a matching entity is deleted.</param>
    void ListenEntityDelete(string classname, EventDelegate callback);

    /// <summary>
    ///     Listen for entity spawn events matching <paramref name="classname" /> (case-insensitive).
    /// </summary>
    /// <param name="classname">Target entity classname.</param>
    /// <param name="callback">Callback invoked when a matching entity is spawned.</param>
    void ListenEntitySpawn(string classname, EventDelegate callback);

    /// <summary>
    ///     Listen for weapon spawn events.
    /// </summary>
    /// <param name="callback">Callback invoked when any weapon entity spawns.</param>
    void ListenWeaponSpawn(WeaponEventDelegate callback);

    /// <summary>
    ///     Hook an entity IO input (case-insensitive matching).
    /// </summary>
    /// <remarks>
    ///     Multiple callbacks for the same pair compose in registration order; <c>result</c> starts as <see cref="EHookAction.Ignored" />.
    ///     Set <c>result = EHookAction.SkipCallReturnOverride</c> to block the input.
    /// </remarks>
    /// <param name="classname">Target entity classname.</param>
    /// <param name="input">Target input name.</param>
    /// <param name="callback">Hook callback.</param>
    void HookEntityInput(string classname, string input, InputDelegate callback);

    /// <summary>
    ///     Hook an entity IO output (case-insensitive matching).
    /// </summary>
    /// <remarks>
    ///     Multiple callbacks for the same pair compose in registration order; <c>result</c> starts as <see cref="EHookAction.Ignored" />.
    ///     Set <c>result = EHookAction.SkipCallReturnOverride</c> to block the output.
    /// </remarks>
    /// <param name="classname">Target entity classname.</param>
    /// <param name="output">Target output name.</param>
    /// <param name="callback">Hook callback.</param>
    void HookEntityOutput(string classname, string output, OutputDelegate callback);
}
