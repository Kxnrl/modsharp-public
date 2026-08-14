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

#ifndef MS_ROOT_STEAMWORKS_H
#define MS_ROOT_STEAMWORKS_H
#include <cstdint>

void InitApiContext();

void DestroyApiContext();

uint64_t GetDualAddonId();

// Resolves the dual-addon workshop id from the command line, falling back to
// <gamedir>/../../sharp/dual_addon.txt. Safe to call at any point during init — it reads a plain
// file and does not depend on the filesystem or convar systems being up yet. Cached after the
// first call.
uint64_t ResolveConfiguredDualAddonId();

#endif