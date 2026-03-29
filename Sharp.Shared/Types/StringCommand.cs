/*
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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
using System.Linq;
using Sharp.Shared.Utilities;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Sharp.Shared.Types;

public readonly record struct StringCommand
{
    public string CommandName { get; }
    public bool   ChatTrigger { get; }
    public string ArgString   { get; }
    public int    ArgCount    => _arguments?.Length ?? 0;

    private readonly string[]? _arguments;
    private const    string    EmptyArg = "";

    public StringCommand(string command, bool chatTrigger, string? argumentString)
    {
        CommandName = command;
        ChatTrigger = chatTrigger;
        ArgString   = string.IsNullOrWhiteSpace(argumentString) ? EmptyArg : argumentString;
        _arguments  = string.IsNullOrWhiteSpace(argumentString) ? null : SplitCommandLine(argumentString).ToArray();
    }

    public string GetCommandString()
        => $"{CommandName} {ArgString}";

    public string this[int index] => GetArg(index);

    /// <summary>
    ///     Get command argument as string
    /// </summary>
    /// <param name="index">Argument index, starts from <b>1</b></param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range</exception>
    public string GetArg(int index)
    {
        var readIndex = index - 1;

        if (readIndex > ArgCount || readIndex < 0 || _arguments is null)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _arguments[readIndex];
    }

    /// <summary>
    ///     Get command argument converted to specified type
    /// </summary>
    /// <typeparam name="T">Target type, supports Int16, UInt16, Int32, UInt32, Int64, UInt64, Float, String</typeparam>
    /// <param name="index">Argument index, starts from <b>1</b></param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range</exception>
    /// <exception cref="NotSupportedException">Generic type T is not supported</exception>
    public T Get<T>(int index)
    {
        var readIndex = index - 1;

        if (readIndex > ArgCount || readIndex < 0 || _arguments is null)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var t = typeof(T);

        if (t == typeof(short))
        {
            return (T) (object) Convert.ToInt16(_arguments[readIndex]);
        }

        if (t == typeof(ushort))
        {
            return (T) (object) Convert.ToUInt16(_arguments[readIndex]);
        }

        if (t == typeof(int))
        {
            return (T) (object) Convert.ToInt32(_arguments[readIndex]);
        }

        if (t == typeof(uint))
        {
            return (T) (object) Convert.ToUInt32(_arguments[readIndex]);
        }

        if (t == typeof(long))
        {
            return (T) (object) Convert.ToInt64(_arguments[readIndex]);
        }

        if (t == typeof(ulong))
        {
            return (T) (object) Convert.ToUInt64(_arguments[readIndex]);
        }

        if (t == typeof(float))
        {
            return (T) (object) (float) Convert.ToDouble(_arguments[readIndex]);
        }

        if (t == typeof(string))
        {
            return (T) (object) _arguments[readIndex];
        }

        throw new NotSupportedException("type not support!");
    }

    /// <summary>
    ///     Try to get command argument converted to specified type, returns null if conversion fails
    /// </summary>
    /// <typeparam name="T">Target type, supports Int16, UInt16, Int32, UInt32, Int64, UInt64, Float</typeparam>
    /// <param name="index">Argument index, starts from <b>1</b></param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range</exception>
    /// <exception cref="NotSupportedException">Generic type T is not supported</exception>
    public T? TryGet<T>(int index)
    {
        var readIndex = index - 1;

        if (readIndex > ArgCount || readIndex < 0 || _arguments is null)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var t = typeof(T);

        if (t == typeof(short?))
        {
            return (T?) (object?) (short.TryParse(_arguments[readIndex], out var v) ? v : null);
        }

        if (t == typeof(ushort?))
        {
            return (T?) (object?) (ushort.TryParse(_arguments[readIndex], out var v) ? v : null);
        }

        if (t == typeof(int?))
        {
            return (T?) (object?) (int.TryParse(_arguments[readIndex], out var v) ? v : null);
        }

        if (t == typeof(uint?))
        {
            return (T?) (object?) (uint.TryParse(_arguments[readIndex], out var v) ? v : null);
        }

        if (t == typeof(long?))
        {
            return (T?) (object?) (long.TryParse(_arguments[readIndex], out var v) ? v : null);
        }

        if (t == typeof(ulong?))
        {
            return (T?) (object?) (ulong.TryParse(_arguments[readIndex], out var v) ? v : null);
        }

        if (t == typeof(float?))
        {
            return (T?) (object?) (float.TryParse(_arguments[readIndex], out var v) ? v : null);
        }

        throw new NotSupportedException("type not support!");
    }

    /// <summary>
    ///     Get command argument and parse as enum value
    /// </summary>
    /// <param name="index">Argument index, starts from <b>1</b></param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range</exception>
    /// <exception cref="NotSupportedException">Generic type T is not supported</exception>
    public T GetEnum<T>(int index) where T : Enum
    {
        var readIndex = index - 1;

        if (readIndex > ArgCount || readIndex < 0 || _arguments is null)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var value = Convert.ToInt32(_arguments[readIndex]);

        return EnumConverter<T>.Convert(value);
    }

#region Parser

    private static IEnumerable<string> SplitCommandLine(string commandLine)
    {
        ArgumentException.ThrowIfNullOrEmpty(commandLine);

        var memory = commandLine.AsMemory();

        var startTokenIndex = 0;

        var pos = 0;

        var seeking      = Boundary.TokenStart;
        var seekingQuote = Boundary.QuoteStart;

        while (pos < memory.Length)
        {
            var c = memory.Span[pos];

            if (char.IsWhiteSpace(c))
            {
                if (seekingQuote == Boundary.QuoteStart)
                {
                    switch (seeking)
                    {
                        case Boundary.WordEnd:
                            yield return CurrentToken();

                            startTokenIndex = pos;
                            seeking         = Boundary.TokenStart;

                            break;

                        case Boundary.TokenStart:
                            startTokenIndex = pos;

                            break;
                    }
                }
            }
            else if (c == '\"')
            {
                if (seeking == Boundary.TokenStart)
                {
                    switch (seekingQuote)
                    {
                        case Boundary.QuoteEnd:
                            yield return CurrentToken();

                            startTokenIndex = pos;
                            seekingQuote    = Boundary.QuoteStart;

                            break;

                        case Boundary.QuoteStart:
                            startTokenIndex = pos + 1;
                            seekingQuote    = Boundary.QuoteEnd;

                            break;
                    }
                }
                else
                {
                    switch (seekingQuote)
                    {
                        case Boundary.QuoteEnd:
                            seekingQuote = Boundary.QuoteStart;

                            break;

                        case Boundary.QuoteStart:
                            seekingQuote = Boundary.QuoteEnd;

                            break;
                    }
                }
            }
            else if (seeking == Boundary.TokenStart && seekingQuote == Boundary.QuoteStart)
            {
                seeking         = Boundary.WordEnd;
                startTokenIndex = pos;
            }

            Advance();

            if (IsAtEndOfInput())
            {
                switch (seeking)
                {
                    case Boundary.TokenStart:
                        break;
                    default:
                        yield return CurrentToken();

                        break;
                }
            }
        }

        void Advance()
            => pos++;

        string CurrentToken()
            => memory.Slice(startTokenIndex, IndexOfEndOfToken()).ToString().Replace("\"", "");

        int IndexOfEndOfToken()
            => pos - startTokenIndex;

        bool IsAtEndOfInput()
            => pos == memory.Length;
    }

    private enum Boundary
    {
        TokenStart,
        WordEnd,
        QuoteStart,
        QuoteEnd,
    }

#endregion
}
