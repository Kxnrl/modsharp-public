using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.LocalizerManager.Core;

internal class LocalizerManager : IModSharpModule, ILocalizerManager, IClientListener
{
    public string DisplayName   => "LocalizerManager";
    public string DisplayAuthor => "Kxnrl";

    private readonly ILogger<LocalizerManager> _logger;
    private readonly IModSharp                 _modSharp;
    private readonly ISharpModuleManager       _modules;
    private readonly IClientManager            _clients;

    // <language, <LKey, LValue>>
    private readonly Dictionary<string, Dictionary<string, string>> _locales;
    private readonly Dictionary<SteamID, Localizer>                 _localizers;
    private readonly CultureInfo                                    _defaultCultureInfo;
    private readonly Localizer                                      _defaultLocalizer;
    private readonly string                                         _localePath;

    public LocalizerManager(ISharedSystem sharedSystem,
        string                            dllPath,
        string                            sharpPath,
        Version                           version,
        IConfiguration                    configuration,
        bool                              hotReload)
    {
        var loggerFactory = sharedSystem.GetLoggerFactory();

        _logger   = loggerFactory.CreateLogger<LocalizerManager>();
        _modSharp = sharedSystem.GetModSharp();
        _modules  = sharedSystem.GetSharpModuleManager();
        _clients  = sharedSystem.GetClientManager();

        _localizers         = new Dictionary<SteamID, Localizer>(128);
        _defaultCultureInfo = CultureInfo.CurrentUICulture;
        _localePath         = Path.Combine(sharpPath, "locales");

        var locales = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, v) in Internationalization.SteamLanguageToI18N)
        {
            locales[v] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        _locales = locales;
        var defaultLocale = _locales.GetValueOrDefault(CultureInfo.CurrentUICulture.Name, _locales["en-us"]);
        _defaultLocalizer = new Localizer(defaultLocale, defaultLocale, new CultureInfo(CultureInfo.CurrentUICulture.Name));
    }

#region IModSharpModule

    public bool Init()
    {
        _clients.InstallClientListener(this);

        return true;
    }

    public void PostInit()
    {
        _modules.RegisterSharpModuleInterface<ILocalizerManager>(this, ILocalizerManager.Identity, this);
    }

    public void Shutdown()
    {
        _clients.RemoveClientListener(this);
    }

#endregion

#region IClientListener

    int IClientListener.ListenerVersion  => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;

    public void OnClientPutInServer(IGameClient client)
        => _modSharp.PushTimer(() => TryQueryLanguage(client), 1, GameTimerFlags.StopOnMapEnd);

    public void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
        => _localizers.Remove(client.SteamId);

    private void TryQueryLanguage(IGameClient client)
    {
        if (!client.IsInGame)
        {
            return;
        }

        if (client.IsFakeClient)
        {
            _localizers[client.SteamId] = CreateLocalize(_defaultCultureInfo.Name);

            return;
        }

        _clients.QueryConVar(client, "cl_language", OnQueryLanguage);
    }

    private void OnQueryLanguage(IGameClient client, QueryConVarValueStatus status, string name, string value)
    {
        if (status != QueryConVarValueStatus.ValueIntact)
        {
            return;
        }

        var identity = client.SteamId;

        if (!identity.IsValidUserId())
        {
            return;
        }

        var i18n = Internationalization.SteamLanguageToI18N.GetValueOrDefault(value, _defaultCultureInfo.Name);

        _localizers[identity] = CreateLocalize(i18n);
    }

    private Localizer CreateLocalize(string i18n)
    {
        var info = new CultureInfo(i18n);

        var def   = _locales[_defaultCultureInfo.Name];
        var local = _locales[info.Name];

        return new Localizer(def, local, info);
    }

#endregion

#region ILocalizerManager

    public void LoadLocaleFile(string name, bool suppressDuplicationWarnings = false)
    {
        var file = $"{name}.json";
        var path = Path.Combine(_localePath, file);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found", file);
        }

        var text = File.ReadAllText(path, Encoding.UTF8);

        var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(text)
                   ?? throw new InvalidDataException($"Invalid locale file: {name}");

        LoadLocaleFile(data, suppressDuplicationWarnings);
    }

    public ILocalizer GetLocalizer(IGameClient client)
        => _localizers.GetValueOrDefault(client.SteamId, _defaultLocalizer);

    public ILocalizer this[IGameClient client] => GetLocalizer(client);

    public bool TryGetLocalizer(IGameClient client, [NotNullWhen(true)] out ILocalizer? localizer)
    {
        if (_localizers.TryGetValue(client.SteamId, out var value))
        {
            localizer = value;

            return true;
        }

        localizer = null;

        return false;
    }

#endregion

    private void LoadLocaleFile(Dictionary<string, Dictionary<string, string>> data, bool suppressDuplicationWarnings)
    {
        foreach (var (key, kv) in data)
        {
            foreach (var (lang, value) in kv)
            {
                if (!_locales.TryGetValue(lang, out var locale))
                {
                    _logger.LogWarning("Invalid language '{lang}' in section '{key}'", lang, key);

                    continue;
                }

                if (locale.TryGetValue(key, out var old) && !suppressDuplicationWarnings)
                {
                    _logger.LogWarning(
                        "Duplicate localization key '{key}' in language '{lang}', override to [{value}] from [{old}]",
                        key,
                        lang,
                        value,
                        old);
                }

                locale[key] = value;
            }
        }
    }
}
