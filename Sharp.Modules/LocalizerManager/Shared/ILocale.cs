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
using System.Globalization;

namespace Sharp.Modules.LocalizerManager.Shared;

/// <summary>
///     Client/culture-scoped locale view.
/// </summary>
public interface ILocale
{
    /// <summary>
    ///     Effective culture for this locale.
    /// </summary>
    CultureInfo Culture { get; }

    /// <summary>
    ///     Localize a key using the locale's culture.
    /// </summary>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException"><paramref name="key" /> does not exist in the loaded locales.</exception>
    /// <exception cref="FormatException">The localized format template is invalid for <paramref name="args" />.</exception>
    string Text(string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Localize a key using the locale's culture.
    /// </summary>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException"><paramref name="key" /> does not exist in the loaded locales.</exception>
    /// <exception cref="FormatException">The localized format template is invalid for the argument.</exception>
    string Text(string key, object? arg0);

    /// <summary>
    ///     Localize a key using the locale's culture.
    /// </summary>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException"><paramref name="key" /> does not exist in the loaded locales.</exception>
    /// <exception cref="FormatException">The localized format template is invalid for the arguments.</exception>
    string Text(string key, object? arg0, object? arg1);

    /// <summary>
    ///     Localize a key using the locale's culture.
    /// </summary>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException"><paramref name="key" /> does not exist in the loaded locales.</exception>
    /// <exception cref="FormatException">The localized format template is invalid for the arguments.</exception>
    string Text(string key, object? arg0, object? arg1, object? arg2);

    /// <summary>
    ///     Localize a key ignoring culture (raw <c>string.Format</c>).
    /// </summary>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException"><paramref name="key" /> does not exist in the loaded locales.</exception>
    /// <exception cref="FormatException">The localized format template is invalid for <paramref name="args" />.</exception>
    string Raw(string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Try to localize a key; returns <see langword="false" /> with <paramref name="key" /> if missing,
    ///     or <see langword="false" /> with the raw unformatted template string if formatting fails.
    /// </summary>
    bool TryText(string key, out string value, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Try to localize a key; returns <see langword="false" /> with <paramref name="key" /> if missing,
    ///     or <see langword="false" /> with the raw unformatted template string if formatting fails.
    /// </summary>
    bool TryText(string key, out string value, object? arg0);

    /// <summary>
    ///     Try to localize a key; returns <see langword="false" /> with <paramref name="key" /> if missing,
    ///     or <see langword="false" /> with the raw unformatted template string if formatting fails.
    /// </summary>
    bool TryText(string key, out string value, object? arg0, object? arg1);

    /// <summary>
    ///     Try to localize a key; returns <see langword="false" /> with <paramref name="key" /> if missing,
    ///     or <see langword="false" /> with the raw unformatted template string if formatting fails.
    /// </summary>
    bool TryText(string key, out string value, object? arg0, object? arg1, object? arg2);

    /// <summary>
    ///     Create a fluent message builder bound to this locale/client.
    /// </summary>
    ILocalizedMessage Message();

    /// <summary>
    ///     Convenience for Message().Text(key, args).
    /// </summary>
    ILocalizedMessage Localized(string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Convenience for Message().Literal(text).
    /// </summary>
    ILocalizedMessage Literal(string text);
}
