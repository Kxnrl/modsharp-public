using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;

namespace Sharp.Modules.LocalizerManager;

internal static class Internationalization
{
    internal static readonly FrozenDictionary<string, string> SteamLanguageToI18N
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "brazilian", "pt-BR" },
            { "bulgarian", "bg-BG" },
            { "czech", "cs-CZ" },
            { "danish", "da-DK" },
            { "dutch", "nl-NL" },
            { "english", "en-US" },
            { "finnish", "fi-FI" },
            { "french", "fr-FR" },
            { "german", "de-DE" },
            { "greek", "el-GR" },
            { "hungarian", "hu-HU" },
            { "indonesian", "id-ID" },
            { "italian", "it-IT" },
            { "japanese", "ja-JP" },
            { "koreana", "ko-KR" },
            { "latam", "es-419" },
            { "norwegian", "nb-NO" },
            { "polish", "pl-PL" },
            { "portuguese", "pt-PT" },
            { "romanian", "ro-RO" },
            { "russian", "ru-RU" },
            { "schinese", "zh-CN" },
            { "spanish", "es-ES" },
            { "swedish", "sv-SE" },
            { "tchinese", "zh-TW" },
            { "thai", "th-TH" },
            { "turkish", "tr-TR" },
            { "ukrainian", "uk-UA" },
            { "vietnamese", "vi-VN" },
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, CultureInfo> CultureInfoCache
        = BuildCultureInfoCache();

    private static FrozenDictionary<string, CultureInfo> BuildCultureInfoCache()
    {
        var dict = new Dictionary<string, CultureInfo>(SteamLanguageToI18N.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var (_, cultureName) in SteamLanguageToI18N)
        {
            dict.TryAdd(cultureName, new CultureInfo(cultureName));
        }

        return dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    internal static CultureInfo GetCulture(string name)
    {
        // Direct i18n name hit (e.g. "en-us")
        if (CultureInfoCache.TryGetValue(name, out var culture))
        {
            return culture;
        }

        // Steam language name (e.g. "english" → "en-us")
        if (SteamLanguageToI18N.TryGetValue(name, out var i18n) && CultureInfoCache.TryGetValue(i18n, out culture))
        {
            return culture;
        }

        // Unknown — fallback (rare, only for custom/unsupported cultures)
        return new CultureInfo(name);
    }
}
