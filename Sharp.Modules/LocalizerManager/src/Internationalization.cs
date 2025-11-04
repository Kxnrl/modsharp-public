using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Sharp.Modules.LocalizerManager.Core;

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
}
