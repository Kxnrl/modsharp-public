/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Sharp.Modules.LocalizerManager.Shared;

namespace Sharp.Modules.LocalizerManager;

internal static class MessageRenderHelper
{
    internal static string Render(List<MessageSegment> segments,
        ILocale              locale,
        bool                 applyPrefix,
        string?              prefix)
    {
        var span      = CollectionsMarshal.AsSpan(segments);
        var hasPrefix = applyPrefix && !string.IsNullOrEmpty(prefix);

        if (span.IsEmpty)
        {
            return string.Empty;
        }

        // Fast path: single segment — try to avoid StringBuilder entirely.
        if (span.Length == 1)
        {
            ref readonly var only = ref span[0];

            if (!hasPrefix)
            {
                // No prefix: always short-circuit, no Concat overhead possible.
                return only.Kind switch
                {
                    SegmentKind.Literal          => only.Text ?? string.Empty,
                    SegmentKind.Value            => only.Args.Arg0?.ToString() ?? string.Empty,
                    SegmentKind.Text             => RenderText(only, locale),
                    SegmentKind.TextWithFallback => RenderTextWithFallback(only, locale),
                    _                            => string.Empty,
                };
            }

            // With prefix: only short-circuit when the content itself doesn't allocate
            // an intermediate string, so Concat is the sole allocation.
            var content = only.Kind switch
            {
                SegmentKind.Literal                              => only.Text ?? string.Empty,
                SegmentKind.Value when only.Args.Arg0 is string s => s,
                SegmentKind.Text             when only.Args.Count == 0 => RenderText(only, locale),
                SegmentKind.TextWithFallback when only.Args.Count == 0 => RenderTextWithFallback(only, locale),
                _ => (string?)null,
            };

            if (content is not null)
            {
                return string.Concat(" ", prefix, " ", content);
            }
        }

        var sb = StringBuilderCache.Acquire(256);

        if (hasPrefix)
        {
            sb.Append(' ');
            sb.Append(prefix);
            sb.Append(' ');
        }

        foreach (ref readonly var segment in span)
        {
            switch (segment.Kind)
            {
                case SegmentKind.Literal:
                    sb.Append(segment.Text);

                    break;
                case SegmentKind.Text:
                    FormatTextTo(sb, segment, locale);

                    break;
                case SegmentKind.TextWithFallback:
                    FormatTextWithFallbackTo(sb, segment, locale);

                    break;
                case SegmentKind.Value:
                    AppendValue(sb, segment.Args.Arg0);

                    break;
            }
        }

        return StringBuilderCache.GetStringAndRelease(sb);
    }

    /// <summary>
    ///     Returns a localized string — used by the single-segment fast path where no StringBuilder is involved.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string RenderText(in MessageSegment segment, ILocale locale)
    {
        if (segment.Args.Array is { } argsArray)
        {
            return locale.Text(segment.Text!, argsArray.AsSpan());
        }

        return segment.Args.Count switch
        {
            0 => locale.Text(segment.Text!),
            1 => locale.Text(segment.Text!, segment.Args.Arg0),
            2 => locale.Text(segment.Text!, segment.Args.Arg0, segment.Args.Arg1),
            3 => locale.Text(segment.Text!, segment.Args.Arg0, segment.Args.Arg1, segment.Args.Arg2),
            _ => locale.Text(segment.Text!),
        };
    }

    /// <summary>
    ///     Returns a localized string with fallback — used by the single-segment fast path.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string RenderTextWithFallback(in MessageSegment segment, ILocale locale)
    {
        if (segment.Args.Array is { } argsArray)
        {
            return locale.TryText(segment.Text!, out var value, argsArray.AsSpan()) ? value : segment.Fallback!;
        }

        return segment.Args.Count switch
        {
            0 => locale.TryText(segment.Text!, out var v0) ? v0 : segment.Fallback!,
            1 => locale.TryText(segment.Text!, out var v1, segment.Args.Arg0) ? v1 : segment.Fallback!,
            2 => locale.TryText(segment.Text!, out var v2, segment.Args.Arg0, segment.Args.Arg1) ? v2 : segment.Fallback!,
            3 => locale.TryText(segment.Text!, out var v3, segment.Args.Arg0, segment.Args.Arg1, segment.Args.Arg2)
                ? v3
                : segment.Fallback!,
            _ => locale.TryText(segment.Text!, out var vN) ? vN : segment.Fallback!,
        };
    }

    /// <summary>
    ///     Formats a Text segment directly into the StringBuilder, avoiding intermediate string allocation
    ///     when the locale is the internal <see cref="Locale" /> implementation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FormatTextTo(StringBuilder sb, in MessageSegment segment, ILocale locale)
    {
        if (locale is Locale fast)
        {
            if (segment.Args.Array is { } argsArray)
            {
                fast.FormatTo(sb, segment.Text!, argsArray.AsSpan());

                return;
            }

            switch (segment.Args.Count)
            {
                case 0:
                    fast.FormatTo(sb, segment.Text!);

                    break;
                case 1:
                    fast.FormatTo(sb, segment.Text!, segment.Args.Arg0);

                    break;
                case 2:
                    fast.FormatTo(sb, segment.Text!, segment.Args.Arg0, segment.Args.Arg1);

                    break;
                case 3:
                    fast.FormatTo(sb, segment.Text!, segment.Args.Arg0, segment.Args.Arg1, segment.Args.Arg2);

                    break;
                default:
                    fast.FormatTo(sb, segment.Text!);

                    break;
            }
        }
        else
        {
            sb.Append(RenderText(segment, locale));
        }
    }

    /// <summary>
    ///     Formats a TextWithFallback segment directly into the StringBuilder.
    ///     Appends the fallback string if the key is missing or formatting fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FormatTextWithFallbackTo(StringBuilder sb, in MessageSegment segment, ILocale locale)
    {
        if (locale is Locale fast)
        {
            bool found;

            if (segment.Args.Array is { } argsArray)
            {
                found = fast.TryFormatTo(sb, segment.Text!, argsArray.AsSpan());
            }
            else
            {
                found = segment.Args.Count switch
                {
                    0 => fast.TryFormatTo(sb, segment.Text!),
                    1 => fast.TryFormatTo(sb, segment.Text!, segment.Args.Arg0),
                    2 => fast.TryFormatTo(sb, segment.Text!, segment.Args.Arg0, segment.Args.Arg1),
                    3 => fast.TryFormatTo(sb, segment.Text!, segment.Args.Arg0, segment.Args.Arg1, segment.Args.Arg2),
                    _ => fast.TryFormatTo(sb, segment.Text!),
                };
            }

            if (!found)
            {
                sb.Append(segment.Fallback);
            }
        }
        else
        {
            sb.Append(RenderTextWithFallback(segment, locale));
        }
    }

    /// <summary>
    ///     Append a boxed value with type specialization to avoid ToString() allocation for common types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendValue(StringBuilder sb, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case string s:
                sb.Append(s);

                break;
            case char c:
                sb.Append(c);

                break;
            case int i:
                sb.Append(i);

                break;
            case uint ui:
                sb.Append(ui);

                break;
            case long l:
                sb.Append(l);

                break;
            case ulong ul:
                sb.Append(ul);

                break;
            case float f:
                sb.Append(f);

                break;
            case double d:
                sb.Append(d);

                break;
            case bool b:
                sb.Append(b);

                break;
            case byte by:
                sb.Append(by);

                break;
            case sbyte sby:
                sb.Append(sby);

                break;
            case short sh:
                sb.Append(sh);

                break;
            case ushort ush:
                sb.Append(ush);

                break;
            default:
                sb.Append(value);

                break;
        }
    }
}

[StructLayout(LayoutKind.Auto)]
internal readonly record struct MessageSegment(SegmentKind Kind,
    string?     Text,
    string?     Fallback,
    SegmentArgs Args)
{
    public static MessageSegment Literal(string text)
        => new (SegmentKind.Literal, text, null, default);

    public static MessageSegment FromText(string key, ReadOnlySpan<object?> args)
        => new (SegmentKind.Text, key, null, SegmentArgs.From(args));

    public static MessageSegment FromText(string key, params object?[] args)
        => FromText(key, (ReadOnlySpan<object?>) args);

    public static MessageSegment FromTextWithFallback(string key, string fallback, ReadOnlySpan<object?> args)
        => new (SegmentKind.TextWithFallback, key, fallback, SegmentArgs.From(args));

    public static MessageSegment FromTextWithFallback(string key, string fallback, params object?[] args)
        => FromTextWithFallback(key, fallback, (ReadOnlySpan<object?>) args);

    public static MessageSegment FromValue(object? value)
        => new (SegmentKind.Value, null, null, SegmentArgs.FromValue(value));
}

internal enum SegmentKind : byte
{
    Literal,
    Text,
    TextWithFallback,
    Value,
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct SegmentArgs
{
    private readonly byte _count;

    private SegmentArgs(byte count, object? arg0, object? arg1, object? arg2, object?[]? array)
    {
        _count = count;
        Arg0   = arg0;
        Arg1   = arg1;
        Arg2   = arg2;
        Array  = array;
    }

    public int Count => Array?.Length ?? _count;

    public object?[]? Array { get; }

    public object? Arg0 { get; }

    public object? Arg1 { get; }

    public object? Arg2 { get; }

    public static SegmentArgs From(ReadOnlySpan<object?> args)
    {
        return args.Length switch
        {
            0 => default,
            1 => new SegmentArgs(1, args[0], null, null, null),
            2 => new SegmentArgs(2, args[0], args[1], null, null),
            3 => new SegmentArgs(3, args[0], args[1], args[2], null),
            _ => new SegmentArgs(0, null, null, null, args.ToArray()),
        };
    }

    public static SegmentArgs FromValue(object? value)
        => new (1, value, null, null, null);
}

internal static class StringBuilderCache
{
    [ThreadStatic]
    private static StringBuilder? _cached;

    public static StringBuilder Acquire(int capacity)
    {
        var sb = _cached;

        if (sb is null)
        {
            return new StringBuilder(capacity);
        }

        _cached = null;
        sb.Clear();

        return sb;
    }

    public static string GetStringAndRelease(StringBuilder sb)
    {
        var result = sb.ToString();

        // If capacity has grown too large, discard it entirely to avoid
        // pinning a large char[] on the TLS slot. Next Acquire will new a fresh one.
        if (sb.Capacity <= 4096)
        {
            _cached = sb;
        }

        return result;
    }
}
