using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Sharp.Modules.LocalizerManager.Shared;

namespace Sharp.Modules.LocalizerManager;

internal static class MessageRenderHelper
{
    internal static string Render(
        List<MessageSegment> segments,
        ILocale              locale,
        bool                 applyPrefix,
        string?              prefix)
    {
        var sb = StringBuilderCache.Acquire(256);

        if (applyPrefix && !string.IsNullOrEmpty(prefix))
        {
            sb.Append(' ');
            sb.Append(prefix);
            sb.Append(' ');
        }

        var span = CollectionsMarshal.AsSpan(segments);

        foreach (ref readonly var segment in span)
        {
            switch (segment.Kind)
            {
                case SegmentKind.Literal:
                    sb.Append(segment.Text);

                    break;
                case SegmentKind.Text:
                    if (segment.Args.Array is { } argsArray)
                    {
                        sb.Append(locale.Text(segment.Text!, argsArray.AsSpan()));

                        break;
                    }

                    sb.Append(segment.Args.Count switch
                    {
                        0 => locale.Text(segment.Text!),
                        1 => locale.Text(segment.Text!, segment.Args.Arg0),
                        2 => locale.Text(segment.Text!, segment.Args.Arg0, segment.Args.Arg1),
                        3 => locale.Text(segment.Text!, segment.Args.Arg0, segment.Args.Arg1, segment.Args.Arg2),
                        _ => locale.Text(segment.Text!),
                    });

                    break;
                case SegmentKind.TextWithFallback:
                    if (segment.Args.Array is { } fallbackArgsArray)
                    {
                        sb.Append(locale.TryText(segment.Text!, out var value, fallbackArgsArray.AsSpan())
                                      ? value
                                      : segment.Fallback);

                        break;
                    }

                    switch (segment.Args.Count)
                    {
                        case 0:
                            sb.Append(locale.TryText(segment.Text!, out var value0) ? value0 : segment.Fallback);

                            break;
                        case 1:
                            sb.Append(locale.TryText(segment.Text!, out var value1, segment.Args.Arg0)
                                          ? value1
                                          : segment.Fallback);

                            break;
                        case 2:
                            sb.Append(locale.TryText(segment.Text!, out var value2, segment.Args.Arg0, segment.Args.Arg1)
                                          ? value2
                                          : segment.Fallback);

                            break;
                        case 3:
                            sb.Append(locale.TryText(segment.Text!,
                                                     out var value3,
                                                     segment.Args.Arg0,
                                                     segment.Args.Arg1,
                                                     segment.Args.Arg2)
                                          ? value3
                                          : segment.Fallback);

                            break;
                        default:
                            sb.Append(locale.TryText(segment.Text!, out var valueN) ? valueN : segment.Fallback);

                            break;
                    }

                    break;
                case SegmentKind.Value:
                    sb.Append(segment.Value);

                    break;
            }
        }

        return StringBuilderCache.GetStringAndRelease(sb);
    }
}

internal readonly record struct MessageSegment(
    SegmentKind Kind,
    string?     Text,
    string?     Fallback,
    SegmentArgs Args,
    object?     Value)
{
    public static MessageSegment Literal(string text)
        => new (SegmentKind.Literal, text, null, default, null);

    public static MessageSegment FromText(string key, ReadOnlySpan<object?> args)
        => new (SegmentKind.Text, key, null, SegmentArgs.From(args), null);

    public static MessageSegment FromText(string key, params object?[] args)
        => FromText(key, (ReadOnlySpan<object?>) args);

    public static MessageSegment FromTextWithFallback(string key, string fallback, ReadOnlySpan<object?> args)
        => new (SegmentKind.TextWithFallback, key, fallback, SegmentArgs.From(args), null);

    public static MessageSegment FromTextWithFallback(string key, string fallback, params object?[] args)
        => FromTextWithFallback(key, fallback, (ReadOnlySpan<object?>) args);

    public static MessageSegment FromValue(object? value)
        => new (SegmentKind.Value, null, null, default, value);
}

internal enum SegmentKind : byte
{
    Literal,
    Text,
    TextWithFallback,
    Value,
}

internal readonly struct SegmentArgs
{
    private readonly byte       _count;

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
            1 => new SegmentArgs(1, args[0], null,    null,    null),
            2 => new SegmentArgs(2, args[0], args[1], null,    null),
            3 => new SegmentArgs(3, args[0], args[1], args[2], null),
            _ => new SegmentArgs(0, null,    null,    null,    args.ToArray()),
        };
    }
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

        if (sb.Capacity > 4096)
        {
            return new StringBuilder(capacity);
        }

        sb.Clear();

        return sb;
    }

    public static string GetStringAndRelease(StringBuilder sb)
    {
        var result = sb.ToString();
        _cached = sb;

        return result;
    }
}
