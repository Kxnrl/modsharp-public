using System;
using Sharp.Shared.Enums;

namespace Sharp.Modules.LocalizerManager.Shared;

/// <summary>
/// Fluent localized message builder bound to a captured client set.
/// </summary>
public interface ILocalizedMessageMany
{
    /// <summary>
    ///     Set or override prefix (applied if not empty).
    /// </summary>
    ILocalizedMessageMany WithPrefix(string prefix);

    /// <summary>
    ///     Disable prefix.
    /// </summary>
    ILocalizedMessageMany WithoutPrefix();

    /// <summary>
    ///     Enable chat color placeholder processing.
    /// </summary>
    ILocalizedMessageMany Colorize(bool enabled = true);

    /// <summary>
    ///     Strip chat color placeholders/control codes.
    /// </summary>
    ILocalizedMessageMany StripColors(bool enabled = true);

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
    ///     Render per locale and print to the captured clients. Do not cache builders across reload/unload.
    /// </summary>
    void Print(HudPrintChannel channel = HudPrintChannel.Chat);
}
