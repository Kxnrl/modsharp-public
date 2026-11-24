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
using System.Collections.Generic;
using System.Globalization;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager.Shared;

public interface ILocalizerManager
{
    const string Identity = nameof(ILocalizerManager);

    /// <summary>
    ///     Load a locale file from <c>{sharp}/locales/{name}.json</c>.
    /// </summary>
    /// <param name="name">Locale file name without extension.</param>
    /// <param name="suppressDuplicationWarnings">Disable duplicate-key warnings when merging.</param>
    /// <exception cref="System.IO.FileNotFoundException">The locale file does not exist.</exception>
    /// <exception cref="System.IO.InvalidDataException">The locale file is empty or cannot be deserialized.</exception>
    /// <exception cref="System.Text.Json.JsonException">The locale file contains invalid JSON.</exception>
    void LoadLocaleFile(string name, bool suppressDuplicationWarnings = false);

    /// <summary>
    ///     Format a localized message for server-side usage (logging, webhooks).
    /// </summary>
    /// <param name="culture">Culture info (for example, <c>en-us</c>).</param>
    /// <param name="key">Key in the locale file.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted string, <paramref name="key" /> if missing, or the raw template string if formatting fails.</returns>
    string Format(CultureInfo culture, string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Format a localized message for server-side usage (logging, webhooks).
    /// </summary>
    /// <param name="cultureName">Culture name (for example, <c>en-us</c>).</param>
    /// <param name="key">Key in the locale file.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted string, <paramref name="key" /> if missing, or the raw template string if formatting fails.</returns>
    /// <exception cref="CultureNotFoundException"><paramref name="cultureName" /> is not a valid culture name.</exception>
    string Format(string cultureName, string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Get a locale bound to a specific client (preferred entrypoint).
    /// </summary>
    ILocale For(IGameClient client);

    /// <summary>
    ///     Get a multi-locale builder for a set of clients (renders per locale).
    /// </summary>
    IMultiLocale ForMany(IEnumerable<IGameClient> clients);

    /// <summary>
    ///     Get a multi-locale builder for a set of clients (renders per locale).
    /// </summary>
    IMultiLocale ForMany(params IGameClient[] clients);
}
