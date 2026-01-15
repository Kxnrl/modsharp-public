using System;
using System.Globalization;

namespace Sharp.Modules.LocalizerManager.Shared;

/// <summary>
/// Client/culture-scoped locale view (new ergonomic API).
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
    string Text(string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Localize a key using the locale's culture.
    /// </summary>
    string Text(string key, object? arg0);

    /// <summary>
    ///     Localize a key using the locale's culture.
    /// </summary>
    string Text(string key, object? arg0, object? arg1);

    /// <summary>
    ///     Localize a key using the locale's culture.
    /// </summary>
    string Text(string key, object? arg0, object? arg1, object? arg2);

    /// <summary>
    ///     Localize a key ignoring culture (raw string.Format).
    /// </summary>
    string Raw(string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Try to localize a key; returns false and key if missing or format fails.
    /// </summary>
    bool TryText(string key, out string value, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Try to localize a key; returns false and key if missing or format fails.
    /// </summary>
    bool TryText(string key, out string value, object? arg0);

    /// <summary>
    ///     Try to localize a key; returns false and key if missing or format fails.
    /// </summary>
    bool TryText(string key, out string value, object? arg0, object? arg1);

    /// <summary>
    ///     Try to localize a key; returns false and key if missing or format fails.
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
