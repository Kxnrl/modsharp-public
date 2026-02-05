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

using System;
using System.Buffers;
using System.Text.Unicode;

namespace Sharp.Shared.Utilities;

public static class SpanExtensions
{
    public static int WriteStringUtf8(this Span<byte> span, string value, out bool truncated)
    {
        if (span.IsEmpty)
        {
            truncated = true;

            return 0;
        }

        var destination = span[..^1];

        var status = Utf8.FromUtf16(value,
                                    destination,
                                    out _,
                                    out var bytesWritten);

        truncated          = status == OperationStatus.DestinationTooSmall;
        span[bytesWritten] = 0;

        return bytesWritten + 1;
    }

    public static int WriteStringUtf8(this Span<byte> span, string value)
        => WriteStringUtf8(span, value, out _);
}
