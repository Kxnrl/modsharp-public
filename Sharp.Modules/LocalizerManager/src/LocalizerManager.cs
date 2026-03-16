using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.LocalizerManager;

internal class LocalizerManager : IModSharpModule, ILocalizerManager, IClientListener
{
    private const string ReloadLocalesCommandName = "ms_locales_reload";

    private const string DefaultPrefix = "[MS]";

    public string DisplayName   => "LocalizerManager";
    public string DisplayAuthor => "Kxnrl";

    private readonly ILogger<LocalizerManager> _logger;
    private readonly IModSharp                 _modSharp;
    private readonly ISharpModuleManager       _modules;
    private readonly IClientManager            _clients;
    private readonly IConVarManager            _conVar;

    // <language, <LKey, LValue>>
    private readonly Dictionary<string, Dictionary<string, string>> _locales;
    private readonly Dictionary<SteamID, Localizer>                 _localizers;
    private readonly Dictionary<SteamID, Locale>                    _localeCache;
    private readonly List<LocaleFileEntry>                          _loadedLocaleFiles;
    private readonly CultureInfo                                    _defaultCultureInfo;
    private readonly Localizer                                      _defaultLocalizer;
    private readonly string                                         _localePath;

    private static readonly JsonSerializerOptions Option
        = new () { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    public LocalizerManager(ISharedSystem  sharedSystem,
                            string         dllPath,
                            string         sharpPath,
                            Version        version,
                            IConfiguration configuration,
                            bool           hotReload)
    {
        var loggerFactory = sharedSystem.GetLoggerFactory();

        _logger         = loggerFactory.CreateLogger<LocalizerManager>();
        _modSharp       = sharedSystem.GetModSharp();
        _modules        = sharedSystem.GetSharpModuleManager();
        _clients        = sharedSystem.GetClientManager();
        _conVar         = sharedSystem.GetConVarManager();

        _localizers        = new Dictionary<SteamID, Localizer>(128);
        _localeCache       = new Dictionary<SteamID, Locale>(128);
        _loadedLocaleFiles = [];
        _localePath        = Path.Combine(sharpPath, "locales");

        var locales = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, v) in Internationalization.SteamLanguageToI18N)
        {
            locales[v] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        _locales = locales;

        var selectedKey = "en-US";
        var targetName  = CultureInfo.CurrentUICulture.Name;

        if (_locales.ContainsKey(targetName))
        {
            selectedKey = targetName;
        }
        else
        {
            var prefix = targetName.Length >= 2 ? targetName[..2] : targetName;

            var match = _locales.Keys.FirstOrDefault(k => k.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                selectedKey = match;
            }
        }

        _defaultCultureInfo = Internationalization.GetCulture(selectedKey);
        var defaultLocale = _locales[selectedKey];
        _defaultLocalizer = new Localizer(defaultLocale, defaultLocale, _defaultCultureInfo);
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
        _conVar.CreateServerCommand(ReloadLocalesCommandName, OnCommandReloadLcales);
    }

    public void Shutdown()
    {
        _clients.RemoveClientListener(this);
        _conVar.ReleaseCommand(ReloadLocalesCommandName);
    }

#endregion

#region IClientListener

    int IClientListener.ListenerVersion  => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;

    public void OnClientPutInServer(IGameClient client)
        => _modSharp.PushTimer(() => TryQueryLanguage(client), 1, GameTimerFlags.StopOnMapEnd);

    public void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        _localizers.Remove(client.SteamId);
        _localeCache.Remove(client.SteamId);
    }

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
        _localeCache.Remove(identity);
    }

    private Localizer CreateLocalize(string cultureName)
    {
        var culture = Internationalization.GetCulture(cultureName);

        var def   = _locales[_defaultCultureInfo.Name];
        var local = _locales.GetValueOrDefault(culture.Name, def);

        return new Localizer(def, local, culture);
    }

#endregion

#region ILocalizerManager

    public void LoadLocaleFile(string name, bool suppressDuplicationWarnings = false)
        => LoadLocaleFileInternal(name, suppressDuplicationWarnings, true);

    private void LoadLocaleFileInternal(string name, bool suppressDuplicationWarnings, bool trackLoadedFile)
    {
        var file = $"{name}.json";
        var path = Path.Combine(_localePath, file);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found", file);
        }

        var text = File.ReadAllText(path, Encoding.UTF8);

        var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(text, Option)
                   ?? throw new InvalidDataException($"Invalid locale file: {name}");

        LoadLocaleFile(data, suppressDuplicationWarnings);

        if (trackLoadedFile)
        {
            TrackLoadedLocaleFile(name, suppressDuplicationWarnings);
        }
    }

    public ILocale For(IGameClient client)
    {
        var id = client.SteamId;

        if (_localeCache.TryGetValue(id, out var cached))
            return cached;

        var locale = new Locale(GetLocalizer(client), client, DefaultPrefix);
        _localeCache[id] = locale;

        return locale;
    }

    public IMultiLocale ForMany(IEnumerable<IGameClient> clients)
        => new MultiLocale(clients.ToArray(), this, DefaultPrefix);

    public IMultiLocale ForMany(params IGameClient[] clients)
        => new MultiLocale(clients, this, DefaultPrefix);

#endregion

    private Localizer GetLocalizer(IGameClient client)
        => _localizers.GetValueOrDefault(client.SteamId, _defaultLocalizer);

    private void ClearLocales()
    {
        foreach (var locale in _locales.Values)
        {
            locale.Clear();
        }

        _localeCache.Clear();
    }

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
                    _logger.LogWarning("Duplicate localization key '{key}' in language '{lang}', override to [{value}] from [{old}]",
                                       key,
                                       lang,
                                       value,
                                       old);
                }

                locale[key] = value;
            }
        }
    }

    private void TrackLoadedLocaleFile(string name, bool suppressDuplicationWarnings)
    {
        for (var i = 0; i < _loadedLocaleFiles.Count; i++)
        {
            if (!_loadedLocaleFiles[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _loadedLocaleFiles.RemoveAt(i);

            break;
        }

        _loadedLocaleFiles.Add(new LocaleFileEntry(name, suppressDuplicationWarnings));
    }

#region ILocalizerManager - Server Formatting

    public string Format(CultureInfo? culture, string key, params ReadOnlySpan<object?> args)
    {
        culture ??= _defaultCultureInfo;

        return FormatInternal(culture, culture.Name, key, args);
    }

    public string Format(string cultureName, string key, params ReadOnlySpan<object?> args)
    {
        var culture = Internationalization.GetCulture(cultureName);

        return FormatInternal(culture, culture.Name, key, args);
    }

    private string FormatInternal(CultureInfo culture, string cultureName, string key, ReadOnlySpan<object?> args)
    {
        if (!_locales.TryGetValue(cultureName, out var dict))
        {
            // if requested lang doesn't exist, use default lang dict
            dict = _locales[_defaultCultureInfo.Name];
        }

        if (!dict.TryGetValue(key, out var formatString))
        {
            // try default lang if key is missing in target language 
            var defaultDict = _locales[_defaultCultureInfo.Name];

            if (!defaultDict.TryGetValue(key, out formatString))
            {
                // key is not found in default language, return key instead
                return key;
            }
        }

        try
        {
            return string.Format(culture, formatString, args);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to format localization string '{Key}' for culture '{Culture}'", key, cultureName);

            return formatString;
        }
    }

#endregion

    private ECommandAction OnCommandReloadLcales(StringCommand arg)
    {
        if (_loadedLocaleFiles.Count == 0)
        {
            return ECommandAction.Handled;
        }

        var total  = _loadedLocaleFiles.Count;
        var loaded = 0;
        var failed = 0;

        ClearLocales();

        foreach (var entry in _loadedLocaleFiles)
        {
            try
            {
                LoadLocaleFileInternal(entry.Name, entry.SuppressDuplicationWarnings, false);
                loaded++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Failed to reload locale file '{LocaleFile}'.", entry.Name);
            }
        }

        _modSharp.LogMessage($"Reloaded locale files: {loaded}/{total} (failed: {failed}).");

        return ECommandAction.Handled;
    }

    private readonly record struct LocaleFileEntry(string Name, bool SuppressDuplicationWarnings);
}
