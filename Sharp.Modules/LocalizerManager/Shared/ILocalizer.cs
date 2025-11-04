using System;
using System.Globalization;

namespace Sharp.Modules.LocalizerManager.Shared;

public interface ILocalizer
{
    /// <summary>
    ///     格式化字符串 (使用Culture属性作为区域性)
    /// </summary>
    string Format(string key, params ReadOnlySpan<object?> param);

    /// <summary>
    ///     格式化字符串 (<b>不</b>使用Culture属性作为区域性)
    /// </summary>
    string FormatRaw(string key, params ReadOnlySpan<object?> param);

    /// <summary>
    ///     获取本地化字符串内容
    /// </summary>
    string this[string key] { get; }

    /// <summary>
    ///     获取本地化字符串内容
    /// </summary>
    string? TryGet(string key);

    /// <summary>
    ///     获取区域属性
    /// </summary>
    CultureInfo Culture { get; }
}
