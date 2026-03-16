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

    private readonly List<MessageSegment>  _segments = new (8);
    private          Func<string, string>? _processor;

    private bool _applyPrefix = true;

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
        // Track the last rendered culture/message to avoid Dictionary allocation
        // in the common single-language case.
        string?                     lastCulture = null;
        string?                     lastMessage = null;
        Dictionary<string, string>? cache       = null;

        foreach (var client in _clients)
        {
            var    locale  = _localizerManager.For(client);
            var    culture = locale.Culture.Name;
            string message;

            // Fast path: same language as the previous client (most clients hit this).
            if (string.Equals(lastCulture, culture, StringComparison.OrdinalIgnoreCase))
            {
                message = lastMessage!;
            }

            // Cache hit: multiple languages present and this one was rendered before.
            else if (cache is not null && cache.TryGetValue(culture, out var cachedMessage))
            {
                message     = cachedMessage;
                lastCulture = culture;
                lastMessage = message;
            }

            // New language: render and spill previous entries into the dictionary.
            else
            {
                message = MessageRenderHelper.Render(_segments, locale, _applyPrefix, _prefix);

                if (_processor is not null)
                {
                    message = _processor(message);
                }

                if (lastCulture is not null)
                {
                    cache ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    cache.TryAdd(lastCulture, lastMessage!);
                    cache[culture] = message;
                }

                lastCulture = culture;
                lastMessage = message;
            }

            client.Print(channel, message);
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
