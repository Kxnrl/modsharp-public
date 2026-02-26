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
    private Func<string, string>?         _processor;

    private bool    _applyPrefix = true;
    private string? _prefix;

    public LocalizedMessageBuilder(ILocale locale, IGameClient? client, string? defaultPrefix)
    {
        _locale = locale;
        _client = client;
        _prefix = defaultPrefix;
    }

    public ILocalizedMessage Prefix(string? prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            _applyPrefix = false;
            _prefix      = null;
        }
        else
        {
            _applyPrefix = true;
            _prefix      = prefix;
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
        _segments.Add(MessageSegment.FromText(key, args));

        return this;
    }

    public ILocalizedMessage TextOrFallback(string key, string fallback, params ReadOnlySpan<object?> args)
    {
        _segments.Add(MessageSegment.FromTextWithFallback(key, fallback, args));

        return this;
    }

    public ILocalizedMessage Value(object? value)
    {
        _segments.Add(MessageSegment.FromValue(value));

        return this;
    }

    public ILocalizedMessage Transform(Func<string, string> processor)
    {
        if (processor is null)
        {
            throw new ArgumentNullException(nameof(processor));
        }

        _processor = _processor is null
            ? processor
            : Chain(_processor, processor);

        return this;
    }

    public string Build()
    {
        var rendered = MessageRenderHelper.Render(_segments, _locale, _applyPrefix, _prefix);

        return _processor is null
            ? rendered
            : _processor(rendered);
    }

    public void Print(HudPrintChannel channel = HudPrintChannel.Chat)
        => _client?.Print(channel, Build());

    private static Func<string, string> Chain(Func<string, string> first, Func<string, string> next)
    {
        return s =>
        {
            var intermediate = first(s) ?? string.Empty;

            return next(intermediate) ?? string.Empty;
        };
    }
}
