using System;
using System.Collections.Generic;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager;

internal sealed class MultiLocalizedMessageBuilder : ILocalizedMessageMany
{
    private readonly IReadOnlyList<IGameClient> _clients;
    private readonly ILocalizerManager          _localizerManager;

    private readonly List<MessageSegment>  _segments = new(8);
    private          Func<string, string>? _processor;

    private bool    _applyPrefix = true;

    private string? _prefix;

    public MultiLocalizedMessageBuilder(IReadOnlyList<IGameClient> clients,
                                        ILocalizerManager          localizerManager,
                                        string?                    defaultPrefix)
    {
        _clients          = clients;
        _localizerManager = localizerManager;
        _prefix           = defaultPrefix;
    }

    public ILocalizedMessageMany Prefix(string? prefix)
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

    public ILocalizedMessageMany Literal(string text)
    {
        _segments.Add(MessageSegment.Literal(text));
        return this;
    }

    public ILocalizedMessageMany Text(string key, params ReadOnlySpan<object?> args)
    {
        _segments.Add(MessageSegment.FromText(key, args));
        return this;
    }

    public ILocalizedMessageMany TextOrFallback(string key, string fallback, params ReadOnlySpan<object?> args)
    {
        _segments.Add(MessageSegment.FromTextWithFallback(key, fallback, args));
        return this;
    }

    public ILocalizedMessageMany Value(object? value)
    {
        _segments.Add(MessageSegment.FromValue(value));
        return this;
    }

    public ILocalizedMessageMany Transform(Func<string, string> processor)
    {
        ArgumentNullException.ThrowIfNull(processor);

        _processor = _processor is null
            ? processor
            : Chain(_processor, processor);

        return this;
    }

    public void Print(HudPrintChannel channel = HudPrintChannel.Chat)
    {
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var client in _clients)
        {
            var locale  = _localizerManager.For(client);
            var culture = locale.Culture.Name;

            if (!cache.TryGetValue(culture, out var message))
            {
                message        = MessageRenderHelper.Render(_segments, locale, _applyPrefix, _prefix);
                message        = _processor is null ? message : _processor(message);
                cache[culture] = message;
            }

            client.GetPlayerController()?.Print(channel, message);
        }
    }

    private static Func<string, string> Chain(Func<string, string> first, Func<string, string> next)
    {
        return s =>
        {
            var intermediate = first(s) ?? string.Empty;

            return next(intermediate) ?? string.Empty;
        };
    }
}
