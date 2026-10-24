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
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Sharp.Shared;

namespace Sharp.Core.Helpers;

internal sealed unsafe class LogCaptureSink : ILogEventSink
{
    private const string ExportName = "LoggingSystem_AddLogCaptureString";

    private const int StackLimit = 256;

    public LogCaptureSink(string outputTemplate)
    {
        _formatter = new MessageTemplateTextFormatter(outputTemplate);
        _function  = nint.Zero;
    }

    public void Arm(ILibraryModule tier0)
    {
        nint address;

        try
        {
            address = tier0.GetExportFunction(ExportName);
        }
        catch (Exception e)
        {
            Log.Logger.Warning(e, "Failed to resolve tier0 '{Export}', crash dumps will not contain managed logs", ExportName);

            return;
        }

        if (address == nint.Zero)
        {
            Log.Logger.Warning("tier0 export '{Export}' not found, crash dumps will not contain managed logs", ExportName);

            return;
        }

        Volatile.Write(ref _function, address);
    }

    public void Emit(LogEvent logEvent)
    {
        var function = Volatile.Read(ref _function);

        if (function == nint.Zero)
        {
            return;
        }

        try
        {
            using var writer = new StringWriter();

            _formatter.Format(logEvent, writer);

            var builder = writer.GetStringBuilder();

            if (builder.Length == 0)
            {
                return;
            }

            if (builder[^1] != '\n')
            {
                builder.Append('\n');
            }

            Write((delegate* unmanaged<byte*, void>) function, builder.ToString());
        }
        catch
        {
        }
    }

    private static void Write(delegate* unmanaged<byte*, void> function, string text)
    {
        var size = Encoding.UTF8.GetByteCount(text) + 1;

        byte[]? rented = null;

        var buffer = size <= StackLimit ? stackalloc byte[size] : rented = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            var written = Encoding.UTF8.GetBytes(text.AsSpan(), buffer);

            buffer[written] = 0;

            fixed (byte* pText = buffer)
            {
                function(pText);
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private readonly MessageTemplateTextFormatter _formatter;
    private          nint                         _function;
}
