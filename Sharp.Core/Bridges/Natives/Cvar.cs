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
using Sharp.Shared.Types;

namespace Sharp.Core.Bridges.Natives;

public static unsafe partial class Cvar
{
    public static partial bool InstallChangeHook(nint cvar);

    public static partial bool RemoveChangeHook(nint ptr);

    public static partial nint FindConVar(string name, bool useIterator);

    public static partial nint CreateConVar(string name,
        ConVarVariantValue*                        defaultValue,
        string                                     helpString,
        ConVarType                                 type,
        ConVarFlags                                flags,
        ConVarVariantValue?*                       min,
        ConVarVariantValue?*                       max);

    public static partial void SetValue(nint cvar, ConVarVariantValue* value);

    public static partial bool SetMinBound(nint cvar, ConVarVariantValue* value);

    public static partial bool SetMaxBound(nint cvar, ConVarVariantValue* value);

    public static partial long CreateCommand(string name,
        string                                      helpString,
        ConVarFlags                                 flags);

    public static partial bool ReleaseCommand(string name);

    public static partial long FindGameCommandHandle(string name);

    public static partial long FindSharpCommandHandle(string name);

    public static partial void ReplicateToClient(nint cvar, string value, nint client);
}
