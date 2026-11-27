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

using System;
using Sharp.Shared.Enums;

namespace Sharp.Modules.LocalizerManager.Shared;

/// <summary>
///     Fluent localized message builder bound to a captured client set.
/// </summary>
public interface ILocalizedMessageMany
{
    /// <summary>
    ///     Sets the prefix (defaults to <c>"[MS]"</c>).
    ///     A leading and trailing space are added around the prefix automatically; pass <see langword="null" /> or an empty string to disable the prefix.
    /// </summary>
    ILocalizedMessageMany Prefix(string? prefix);

    /// <summary>
    ///     Append literal text.
    /// </summary>
    ILocalizedMessageMany Literal(string text);

    /// <summary>
    ///     Append localized text.
    /// </summary>
    ILocalizedMessageMany Text(string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Append localized text or fallback if missing/format fails.
    /// </summary>
    ILocalizedMessageMany TextOrFallback(string key, string fallback, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Append a raw value.
    /// </summary>
    ILocalizedMessageMany Value(object? value);

    /// <summary>
    ///     Register a post-processor for each rendered string (applied before print). Multiple calls compose in order.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="processor" /> is <see langword="null" />.</exception>
    ILocalizedMessageMany Transform(Func<string, string> processor);

    /// <summary>
    ///     Render per locale and print to the captured clients. Do not cache builders across reload/unload.
    /// </summary>
    void Print(HudPrintChannel channel = HudPrintChannel.Chat);
}
