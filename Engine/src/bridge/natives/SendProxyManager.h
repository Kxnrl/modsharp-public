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

#ifndef MS_NATIVE_SENDPROXY_H
#define MS_NATIVE_SENDPROXY_H

#include <cstdint>

// One recipient's override value inside a per-(entity,field) batch. The managed batch callback fills the slots
// it wants; native applies each to the matching client. Layout is blittable and mirrored by the managed struct.
struct SendProxyValue
{
    int32_t kind; // 0 int, 1 float, 2 bool, 3 vector, 4 string
    int64_t i;
    float   f;
    float   x, y, z;
    int32_t strLen;   // UTF8 byte length in str (excludes the null terminator)
    char    str[256]; // inline so it stays alive from the callback through the re-encode on the same frame
};

namespace natives::sendproxy
{
void Init();
}

#endif
