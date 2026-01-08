using System;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager.Shared;

/// <summary>
/// Fluent localized message builder (new ergonomic API).
/// </summary>
public interface ILocalizedMessage
{
    /// <summary>
    ///     Set or override prefix (applied if not empty).
    /// </summary>
    ILocalizedMessage WithPrefix(string prefix);

    /// <summary>
    ///     Disable prefix.
    /// </summary>
    ILocalizedMessage WithoutPrefix();

    /// <summary>
    ///     Enable chat color placeholder processing.
    /// </summary>
    ILocalizedMessage Colorize(bool enabled = true);

    /// <summary>
    ///     Strip chat color placeholders/control codes.
    /// </summary>
    ILocalizedMessage StripColors(bool enabled = true);

    /// <summary>
    ///     Append literal text.
    /// </summary>
    ILocalizedMessage Literal(string text);

    /// <summary>
    ///     Append localized text.
    /// </summary>
    ILocalizedMessage Text(string key, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Append localized text or fallback if missing/format fails.
    /// </summary>
    ILocalizedMessage TextOrFallback(string key, string fallback, params ReadOnlySpan<object?> args);

    /// <summary>
    ///     Append a raw value.
    /// </summary>
    ILocalizedMessage Value(object? value);

    /// <summary>
    ///     Build the final string (applies prefix/colors).
    /// </summary>
    string Build();

    /// <summary>
    ///     Print to the bound client (if any) on the specified channel.
    ///     Do not cache builders across unload/reload.
    /// </summary>
    void Print(HudPrintChannel channel = HudPrintChannel.Chat);
}
