using System;
using Sharp.Shared.Enums;

namespace Sharp.Modules.LocalizerManager.Shared;

/// <summary>
/// Fluent localized message builder bound to a captured client set.
/// </summary>
public interface ILocalizedMessageMany
{
    /// <summary>
    ///     Sets the prefix.
    ///     Pass <c>null</c> to disable the prefix entirely.
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
    ILocalizedMessageMany Transform(Func<string, string> processor);

    /// <summary>
    ///     Render per locale and print to the captured clients. Do not cache builders across reload/unload.
    /// </summary>
    void Print(HudPrintChannel channel = HudPrintChannel.Chat);
}
