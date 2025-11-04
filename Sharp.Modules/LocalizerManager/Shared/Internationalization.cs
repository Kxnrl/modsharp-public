using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Sharp.Modules.LocalizerManager.Shared;

internal static class Internationalization
{
    internal static readonly FrozenDictionary<string, string> SteamLanguageToI18N
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "brazilian", "pt-br" },
            { "bulgarian", "bg-bg" },
            { "czech", "cs-cz" },
            { "danish", "da-dk" },
            { "dutch", "nl-nl" },
            { "english", "en-us" },
            { "finnish", "fi-fi" },
            { "french", "fr-fr" },
            { "german", "de-de" },
            { "greek", "el-gr" },
            { "hungarian", "hu-hu" },
            { "indonesian", "id-id" },
            { "italian", "it-it" },
            { "japanese", "ja-jp" },
            { "koreana", "ko-kr" },
            { "latam", "es-419" },
            { "norwegian", "nb-no" },
            { "polish", "pl-pl" },
            { "portuguese", "pt-pt" },
            { "romanian", "ro-ro" },
            { "russian", "ru-ru" },
            { "schinese", "zh-cn" },
            { "spanish", "es-es" },
            { "swedish", "sv-se" },
            { "tchinese", "zh-tw" },
            { "thai", "th-th" },
            { "turkish", "tr-tr" },
            { "ukrainian", "uk-ua" },
            { "vietnamese", "vi-vn" },
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}
