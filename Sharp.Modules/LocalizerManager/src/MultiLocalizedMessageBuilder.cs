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

    private readonly List<MessageSegment> _segments = new(8);

    private bool    _applyPrefix = true;
    private bool    _colorize;
    private bool    _stripColors;
    private string? _prefix;

    public MultiLocalizedMessageBuilder(IReadOnlyList<IGameClient> clients,
                                        ILocalizerManager          localizerManager,
                                        string?                    defaultPrefix)
    {
        _clients          = clients;
        _localizerManager = localizerManager;
        _prefix           = defaultPrefix;
    }

    public ILocalizedMessageMany WithPrefix(string prefix)
    {
        _prefix      = prefix;
        _applyPrefix = true;
        return this;
    }

    public ILocalizedMessageMany WithoutPrefix()
    {
        _applyPrefix = false;
        return this;
    }

    public ILocalizedMessageMany Colorize(bool enabled = true)
    {
        _colorize = enabled;
        if (enabled)
        {
            _stripColors = false;
        }

        return this;
    }

    public ILocalizedMessageMany StripColors(bool enabled = true)
    {
        _stripColors = enabled;
        if (enabled)
        {
            _colorize = false;
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
        _segments.Add(MessageSegment.FromText(key, args.ToArray()));
        return this;
    }

    public ILocalizedMessageMany TextOrFallback(string key, string fallback, params ReadOnlySpan<object?> args)
    {
        _segments.Add(MessageSegment.FromTextWithFallback(key, fallback, args.ToArray()));
        return this;
    }

    public ILocalizedMessageMany Value(object? value)
    {
        _segments.Add(MessageSegment.FromValue(value));
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
                message        = MessageRenderHelper.Render(_segments, locale, _applyPrefix, _prefix, _stripColors, _colorize);
                cache[culture] = message;
            }

            client.GetPlayerController()?.Print(channel, message);
        }
    }
}
