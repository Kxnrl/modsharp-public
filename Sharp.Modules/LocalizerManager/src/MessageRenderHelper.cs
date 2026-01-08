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
        string?              prefix,
        bool                 stripColors,
        bool                 colorize)
    {
        var sb = new StringBuilder(256);

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
                    sb.Append(locale.Text(segment.Text!, segment.Args.Span));

                    break;
                case SegmentKind.TextWithFallback:
                    if (locale.TryText(segment.Text!, out var value, segment.Args.Span))
                    {
                        sb.Append(value);
                    }
                    else
                    {
                        sb.Append(segment.Fallback);
                    }

                    break;
                case SegmentKind.Value:
                    sb.Append(segment.Value);

                    break;
            }
        }

        var result = sb.ToString();

        if (stripColors)
        {
            return result.StripChatColors();
        }

        return colorize ? result.ProcessChatColors() : result;
    }
}

internal readonly record struct MessageSegment(
    SegmentKind             Kind,
    string?                 Text,
    string?                 Fallback,
    ReadOnlyMemory<object?> Args,
    object?                 Value)
{
    private static readonly object?[] EmptyArgs = [];

    public static MessageSegment Literal(string text)
        => new (SegmentKind.Literal, text, null, EmptyArgs, null);

    public static MessageSegment FromText(string key, params object?[] args)
        => new (SegmentKind.Text, key, null, args, null);

    public static MessageSegment FromTextWithFallback(string key, string fallback, params object?[] args)
        => new (SegmentKind.TextWithFallback, key, fallback, args, null);

    public static MessageSegment FromValue(object? value)
        => new (SegmentKind.Value, null, null, EmptyArgs, value);
}

internal enum SegmentKind : byte
{
    Literal,
    Text,
    TextWithFallback,
    Value,
}
