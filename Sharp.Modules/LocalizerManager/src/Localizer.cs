using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sharp.Modules.LocalizerManager;

internal class Localizer
{
    private readonly Dictionary<string, string> _default;
    private readonly Dictionary<string, string> _local;
    private readonly CultureInfo                _culture;

    public Localizer(Dictionary<string, string> @default,
                     Dictionary<string, string> local,
                     CultureInfo                culture)
    {
        _default = @default;
        _local   = local;
        _culture = culture;
    }

    public string Format(string key, params ReadOnlySpan<object?> param)
        => string.Format(_culture, this[key], param);

    public string Format(string key, object? arg0)
        => string.Format(_culture, this[key], arg0);

    public string Format(string key, object? arg0, object? arg1)
        => string.Format(_culture, this[key], arg0, arg1);

    public string Format(string key, object? arg0, object? arg1, object? arg2)
        => string.Format(_culture, this[key], arg0, arg1, arg2);

    public string FormatRaw(string key, params ReadOnlySpan<object?> param)
        => string.Format(this[key], param);

    public string FormatRaw(string key, object? arg0)
        => string.Format(this[key], arg0);

    public string FormatRaw(string key, object? arg0, object? arg1)
        => string.Format(this[key], arg0, arg1);

    public string FormatRaw(string key, object? arg0, object? arg1, object? arg2)
        => string.Format(this[key], arg0, arg1, arg2);

    public string this[string key] => TryGet(key) ?? throw new KeyNotFoundException($"Missing '{key}' in locale file");

    public string? TryGet(string key)
        => _local.TryGetValue(key, out var local) ? local : _default.GetValueOrDefault(key);

    public CultureInfo Culture => _culture;
}
