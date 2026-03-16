using System;
using System.Globalization;
using System.Text;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager;

internal sealed class Locale : ILocale
{
    private readonly Localizer    _localizer;
    private readonly IGameClient? _client;
    private readonly string?      _defaultPrefix;

    public Locale(Localizer localizer, IGameClient? client, string? defaultPrefix)
    {
        _localizer     = localizer;
        _client        = client;
        _defaultPrefix = defaultPrefix;
    }

    public CultureInfo Culture => _localizer.Culture;

    public string Text(string key, params ReadOnlySpan<object?> args)
        => _localizer.Format(key, args);

    public string Text(string key, object? arg0)
        => _localizer.Format(key, arg0);

    public string Text(string key, object? arg0, object? arg1)
        => _localizer.Format(key, arg0, arg1);

    public string Text(string key, object? arg0, object? arg1, object? arg2)
        => _localizer.Format(key, arg0, arg1, arg2);

    public string Raw(string key, params ReadOnlySpan<object?> args)
        => _localizer.FormatRaw(key, args);

    public bool TryText(string key, out string value, params ReadOnlySpan<object?> args)
    {
        var format = _localizer.TryGet(key);

        if (format is null)
        {
            value = key;

            return false;
        }

        try
        {
            value = string.Format(_localizer.Culture, format, args);

            return true;
        }
        catch (FormatException)
        {
            value = format;

            return false;
        }
    }

    public bool TryText(string key, out string value, object? arg0)
    {
        var format = _localizer.TryGet(key);

        if (format is null)
        {
            value = key;

            return false;
        }

        try
        {
            value = string.Format(_localizer.Culture, format, arg0);

            return true;
        }
        catch (FormatException)
        {
            value = format;

            return false;
        }
    }

    public bool TryText(string key, out string value, object? arg0, object? arg1)
    {
        var format = _localizer.TryGet(key);

        if (format is null)
        {
            value = key;

            return false;
        }

        try
        {
            value = string.Format(_localizer.Culture, format, arg0, arg1);

            return true;
        }
        catch (FormatException)
        {
            value = format;

            return false;
        }
    }

    public bool TryText(string key, out string value, object? arg0, object? arg1, object? arg2)
    {
        var format = _localizer.TryGet(key);

        if (format is null)
        {
            value = key;

            return false;
        }

        try
        {
            value = string.Format(_localizer.Culture, format, arg0, arg1, arg2);

            return true;
        }
        catch (FormatException)
        {
            value = format;

            return false;
        }
    }

    public ILocalizedMessage Message()
        => new LocalizedMessageBuilder(this, _client, _defaultPrefix);

    public ILocalizedMessage Localized(string key, params ReadOnlySpan<object?> args)
        => Message().Text(key, args);

    public ILocalizedMessage Literal(string text)
        => Message().Literal(text);

    internal void FormatTo(StringBuilder sb, string key, params ReadOnlySpan<object?> args)
        => _localizer.AppendFormat(sb, key, args);

    internal void FormatTo(StringBuilder sb, string key, object? arg0)
        => _localizer.AppendFormat(sb, key, arg0);

    internal void FormatTo(StringBuilder sb, string key, object? arg0, object? arg1)
        => _localizer.AppendFormat(sb, key, arg0, arg1);

    internal void FormatTo(StringBuilder sb, string key, object? arg0, object? arg1, object? arg2)
        => _localizer.AppendFormat(sb, key, arg0, arg1, arg2);

    internal bool TryFormatTo(StringBuilder sb, string key, params ReadOnlySpan<object?> args)
        => _localizer.TryAppendFormat(sb, key, args);

    internal bool TryFormatTo(StringBuilder sb, string key, object? arg0)
        => _localizer.TryAppendFormat(sb, key, arg0);

    internal bool TryFormatTo(StringBuilder sb, string key, object? arg0, object? arg1)
        => _localizer.TryAppendFormat(sb, key, arg0, arg1);

    internal bool TryFormatTo(StringBuilder sb, string key, object? arg0, object? arg1, object? arg2)
        => _localizer.TryAppendFormat(sb, key, arg0, arg1, arg2);
}
