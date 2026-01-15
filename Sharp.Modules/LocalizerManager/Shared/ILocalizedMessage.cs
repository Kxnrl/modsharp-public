using System;
using Sharp.Shared.Enums;

namespace Sharp.Modules.LocalizerManager.Shared;

/// <summary>
/// Fluent localized message builder (new ergonomic API).
/// </summary>
public interface ILocalizedMessage
{
    /// <summary>
    ///     Sets the prefix.
    ///     Pass <c>null</c> to disable the prefix entirely.
    /// </summary>
    ILocalizedMessage Prefix(string? prefix);

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
    ///     Register a post-processor for the rendered string (applied before Build/Print return/send).
    ///     Multiple calls compose in order of invocation.
    /// </summary>
    ILocalizedMessage Transform(Func<string, string> processor);

    /// <summary>
    ///     Build the final string (applies prefix/colors and any processors).
    /// </summary>
    string Build();

    /// <summary>
    ///     Print to the bound client (if any) on the specified channel.
    ///     Do not cache builders across unload/reload.
    /// </summary>
    void Print(HudPrintChannel channel = HudPrintChannel.Chat);
}
