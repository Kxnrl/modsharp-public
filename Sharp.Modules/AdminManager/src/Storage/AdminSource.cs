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

namespace Sharp.Modules.AdminManager.Storage;

internal sealed class AdminSource
{
    public byte            CalculatedImmunity { get; }
    public HashSet<string> ResolvedAllows     { get; set; }
    public HashSet<string> ResolvedDenies     { get; set; }
    public HashSet<string> RawRules           { get; }

    public AdminSource(byte calculatedImmunity,
        HashSet<string>     resolvedAllows,
        HashSet<string>     resolvedDenies,
        HashSet<string>     rawRules)
    {
        CalculatedImmunity = calculatedImmunity;
        ResolvedAllows     = resolvedAllows;
        ResolvedDenies     = resolvedDenies;
        RawRules           = rawRules;
    }
}
