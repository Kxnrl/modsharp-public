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

using System.Buffers;
using Sharp.Modules.AdminManager.Shared;

namespace Sharp.Modules.AdminManager.Permissions;

internal static class PermissionMatcher
{
    private static readonly SearchValues<char> WildcardChars = SearchValues.Create(IAdminManager.WildCardOperator);

    public static bool HasWildcard(string rule)
        => !string.IsNullOrEmpty(rule) && rule.AsSpan().ContainsAny(WildcardChars);

    public static bool IsPureWildcardSegment(ReadOnlySpan<char> segment)
    {
        const char wildcard = IAdminManager.WildCardOperator;

        return !segment.IsEmpty && segment.Trim(wildcard).IsEmpty;
    }

    /// <summary>
    ///     Validates if a concrete permission matches a pattern using Zero-Allocation Spans.
    /// </summary>
    /// <param name="permission">The concrete permission (e.g., "admin:money:give")</param>
    /// <param name="pattern">The pattern (e.g., "admin:*:give")</param>
    public static bool IsWildcardMatch(ReadOnlySpan<char> permission, ReadOnlySpan<char> pattern)
    {
        const char separator = IAdminManager.SeparatorOperator;

        // Optimization: identical strings always match
        if (permission.SequenceEqual(pattern))
        {
            return true;
        }

        while (true)
        {
            // If the pattern is exhausted, the permission must also be exhausted.
            if (pattern.IsEmpty)
            {
                return permission.IsEmpty;
            }

            var patSepIdx  = pattern.IndexOf(separator);
            var permSepIdx = permission.IndexOf(separator);

            // If no separator is found (-1), take the rest of the string.
            var currPatSeg = patSepIdx == -1 ? pattern : pattern.Slice(0, patSepIdx);

            // Check if the current pattern segment is a pure wildcard (e.g. "*" or "**")
            var isWildcardSegment = IsPureWildcardSegment(currPatSeg);

            if (isWildcardSegment)
            {
                // LOGIC: Trailing Wildcard (e.g. "admin:*")
                // If this is the LAST segment in the pattern, it matches EVERYTHING remaining.
                if (patSepIdx == -1)
                {
                    return true;
                }

                // LOGIC: Mid-Wildcard (e.g. "admin:*:give")
                // The wildcard requires "something" to be here. 
                // If the permission string runs out early, it's a fail.
                if (permission.IsEmpty)
                {
                    return false;
                }
            }
            else
            {
                // If permission runs out but pattern expects more -> Fail
                if (permission.IsEmpty)
                {
                    return false;
                }

                // Get the permission segment to compare
                var currPermSeg = permSepIdx == -1 ? permission : permission.Slice(0, permSepIdx);

                // Compare ignoring case
                if (!currPermSeg.Equals(currPatSeg, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Move the span forward past the separator.
            pattern    = patSepIdx  == -1 ? ReadOnlySpan<char>.Empty : pattern.Slice(patSepIdx     + 1);
            permission = permSepIdx == -1 ? ReadOnlySpan<char>.Empty : permission.Slice(permSepIdx + 1);
        }
    }
}
