using System;
using System.Collections.Generic;
using System.Globalization;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager;

internal class Localizer
{
    private readonly Dictionary<string, string> _default;
    private readonly Dictionary<string, string> _local;
    private readonly CultureInfo                _culture;
    private readonly IGameClient?               _client;

    public Localizer(Dictionary<string, string> @default,
                     Dictionary<string, string> local,
                     CultureInfo                culture,
                     IGameClient?               client = null)
    {
        _default = @default;
        _local   = local;
        _culture = culture;
        _client  = client;
    }

    public string Format(string key, params ReadOnlySpan<object?> param)
        => string.Format(_culture, this[key], param);

    public string FormatRaw(string key, params ReadOnlySpan<object?> param)
        => string.Format(this[key], param);

    public string this[string key] => TryGet(key) ?? throw new KeyNotFoundException($"Missing '{key}' in locale file");

    public string? TryGet(string key)
        => _local.TryGetValue(key, out var local) ? local : _default.GetValueOrDefault(key);

    public CultureInfo Culture => _culture;
}
