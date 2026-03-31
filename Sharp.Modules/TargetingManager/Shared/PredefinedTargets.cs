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

namespace Sharp.Modules.TargetingManager.Shared;

public static class PredefinedTargets
{
    public const string All  = "@all";
    public const string None = "@!all";

    public const string Aim = "@aim";

    public const string Ct   = "@ct";
    public const string T    = "@t";
    public const string Spec = "@spec";

    public const string Alive = "@alive";
    public const string Dead  = "@dead";

    public const string Me    = "@me";
    public const string NotMe = "@!me";

    public const string Bots = "@bots";
}
