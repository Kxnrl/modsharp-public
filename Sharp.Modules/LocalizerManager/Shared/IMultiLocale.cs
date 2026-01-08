using System;
using System.Collections.Generic;

namespace Sharp.Modules.LocalizerManager.Shared;

/// <summary>
/// Locale view for a captured set of clients; intended only for multi-send builders.
/// </summary>
public interface IMultiLocale
{
    /// <summary>
    ///     Create a fluent message builder for the captured clients.
    /// </summary>
    ILocalizedMessageMany Message();

    /// <summary>
    ///     Convenience for Message().Text(key, args).
    /// </summary>
    ILocalizedMessageMany Localized(string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Convenience for Message().Literal(text).
    /// </summary>
    ILocalizedMessageMany Literal(string text);
}
