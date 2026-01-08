using System;
using System.Collections.Generic;
using System.Globalization;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager.Shared;

public interface ILocalizerManager
{
    const string Identity = nameof(ILocalizerManager);

    /// <summary>
    ///     Load a locale file from {sharp}/locales/{name}.json.
    /// </summary>
    /// <param name="name">Locale file name without extension.</param>
    /// <param name="suppressDuplicationWarnings">Disable duplicate-key warnings when merging.</param>
    void LoadLocaleFile(string name, bool suppressDuplicationWarnings = false);

    /// <summary>
    ///     Format a localized message for server-side usage (logging, webhooks).
    /// </summary>
    /// <param name="culture">culture info. en-us for example</param>
    /// <param name="key">Key in the locale file</param>
    /// <param name="args">arguments</param>
    /// <returns></returns>
    string Format(CultureInfo culture, string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Format a localized message for server-side usage (logging, webhooks).
    /// </summary>
    /// <param name="cultureName">culture name. en-us for example</param>
    /// <param name="key">Key in the locale file</param>
    /// <param name="args">arguments</param>
    /// <returns></returns>
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
