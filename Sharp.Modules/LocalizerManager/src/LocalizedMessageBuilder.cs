using System;
using System.Collections.Generic;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager;

internal sealed class LocalizedMessageBuilder : ILocalizedMessage
{
    private readonly ILocale              _locale;
    private readonly IGameClient?         _client;
    private readonly List<MessageSegment> _segments = new (8);

    private bool   _applyPrefix = true;
    private bool   _colorize;
    private bool   _stripColors;
    private string? _prefix;

    public LocalizedMessageBuilder(ILocale locale, IGameClient? client, string? defaultPrefix)
    {
        _locale = locale;
        _client = client;
        _prefix = defaultPrefix;
    }

    public ILocalizedMessage WithPrefix(string prefix)
    {
        _prefix = prefix;
        _applyPrefix = true;
        return this;
    }

    public ILocalizedMessage WithoutPrefix()
    {
        _applyPrefix = false;
        return this;
    }

    public ILocalizedMessage Colorize(bool enabled = true)
    {
        _colorize = enabled;
        if (enabled)
        {
            _stripColors = false;
        }
        return this;
    }

    public ILocalizedMessage StripColors(bool enabled = true)
    {
        _stripColors = enabled;
        if (enabled)
        {
            _colorize = false;
        }
        return this;
    }

    public ILocalizedMessage Literal(string text)
    {
        _segments.Add(MessageSegment.Literal(text));

        return this;
    }

    public ILocalizedMessage Text(string key, params ReadOnlySpan<object?> args)
    {
        _segments.Add(MessageSegment.FromText(key, args.ToArray()));

        return this;
    }

    public ILocalizedMessage TextOrFallback(string key, string fallback, params ReadOnlySpan<object?> args)
    {
        _segments.Add(MessageSegment.FromTextWithFallback(key, fallback, args.ToArray()));

        return this;
    }

    public ILocalizedMessage Value(object? value)
    {
        _segments.Add(MessageSegment.FromValue(value));

        return this;
    }

    public string Build()
        => MessageRenderHelper.Render(_segments, _locale, _applyPrefix, _prefix, _stripColors, _colorize);

    public void Print(HudPrintChannel channel = HudPrintChannel.Chat)
        => _client?.GetPlayerController()?.Print(channel, Build());
}
