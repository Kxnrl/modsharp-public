using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sharp.Modules.LocalizerManager;

internal class Localizer
{
    private readonly Dictionary<string, string> _default;
    private readonly Dictionary<string, string> _local;
    private readonly CultureInfo                _culture;
    private readonly bool                       _isDefault;

    public Localizer(Dictionary<string, string> @default,
                     Dictionary<string, string> local,
                     CultureInfo                culture)
    {
        _default   = @default;
        _local     = local;
        _culture   = culture;
        _isDefault = ReferenceEquals(@default, local);
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
        => _local.TryGetValue(key, out var local) ? local
         : _isDefault ? null
         : _default.GetValueOrDefault(key);

    public void AppendFormat(StringBuilder sb, string key, params ReadOnlySpan<object?> args)
        => sb.AppendFormat(_culture, this[key], args);

    public void AppendFormat(StringBuilder sb, string key, object? arg0)
        => sb.AppendFormat(_culture, this[key], arg0);

    public void AppendFormat(StringBuilder sb, string key, object? arg0, object? arg1)
        => sb.AppendFormat(_culture, this[key], arg0, arg1);

    public void AppendFormat(StringBuilder sb, string key, object? arg0, object? arg1, object? arg2)
        => sb.AppendFormat(_culture, this[key], arg0, arg1, arg2);

    public bool TryAppendFormat(StringBuilder sb, string key, params ReadOnlySpan<object?> args)
    {
        var format = TryGet(key);
        if (format is null) return false;

        try
        {
            sb.AppendFormat(_culture, format, args);
            return true;
        }
        catch (FormatException)
        {
            sb.Append(format);
            return false;
        }
    }

    public bool TryAppendFormat(StringBuilder sb, string key, object? arg0)
    {
        var format = TryGet(key);
        if (format is null) return false;

        try
        {
            sb.AppendFormat(_culture, format, arg0);
            return true;
        }
        catch (FormatException)
        {
            sb.Append(format);
            return false;
        }
    }

    public bool TryAppendFormat(StringBuilder sb, string key, object? arg0, object? arg1)
    {
        var format = TryGet(key);
        if (format is null) return false;

        try
        {
            sb.AppendFormat(_culture, format, arg0, arg1);
            return true;
        }
        catch (FormatException)
        {
            sb.Append(format);
            return false;
        }
    }

    public bool TryAppendFormat(StringBuilder sb, string key, object? arg0, object? arg1, object? arg2)
    {
        var format = TryGet(key);
        if (format is null) return false;

        try
        {
            sb.AppendFormat(_culture, format, arg0, arg1, arg2);
            return true;
        }
        catch (FormatException)
        {
            sb.Append(format);
            return false;
        }
    }

    public CultureInfo Culture => _culture;
}
