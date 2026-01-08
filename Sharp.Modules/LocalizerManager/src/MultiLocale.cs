using System;
using System.Collections.Generic;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared.Objects;

namespace Sharp.Modules.LocalizerManager;

internal sealed class MultiLocale : IMultiLocale
{
    private readonly IReadOnlyList<IGameClient> _clients;
    private readonly ILocalizerManager          _localizerManager;
    private readonly string?                    _defaultPrefix;

    public MultiLocale(IReadOnlyList<IGameClient> clients, ILocalizerManager localizerManager, string? defaultPrefix)
    {
        _clients          = clients;
        _localizerManager = localizerManager;
        _defaultPrefix    = defaultPrefix;
    }

    public ILocalizedMessageMany Message()
        => new MultiLocalizedMessageBuilder(_clients, _localizerManager, _defaultPrefix);

    public ILocalizedMessageMany Localized(string key, params ReadOnlySpan<object?> args)
        => Message().Text(key, args);

    public ILocalizedMessageMany Literal(string text)
        => Message().Literal(text);
}
