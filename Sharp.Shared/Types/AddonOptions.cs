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

namespace Sharp.Shared.Types;

/// <summary>
///     Tuning of the MultiAddon flow
/// </summary>
/// <param name="ClientTimeout">Seconds allowed between reconnects while a client downloads the next addon</param>
/// <param name="ConnectionTimeout">Seconds allowed to accept the first addon before the client is kicked, 0 disables</param>
/// <param name="CacheDuration">Seconds to remember the addons a client downloaded, 0 forever, negative disables the cache</param>
/// <param name="Debug">Print verbose information about the download flow</param>
public readonly record struct AddonOptions(
    float ClientTimeout     = 10,
    float ConnectionTimeout = 0,
    float CacheDuration     = 600,
    bool  Debug             = false);
