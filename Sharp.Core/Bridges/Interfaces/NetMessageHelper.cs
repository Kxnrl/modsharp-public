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
using System.Buffers;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using Sharp.Core.Bridges.Natives;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Core.Bridges.Interfaces;

internal static class NetMessageHelper
{
    private static class NetMessageCache<T>
    {
        public static readonly nint Handle = Net.GetNetMessageHandle(typeof(T).Name);
    }

    [SkipLocalsInit]
    public static unsafe bool SendNetMessage<T>(RecipientFilter filter, T data) where T : class, IMessage
    {
        var handle = NetMessageCache<T>.Handle;

        if (handle == nint.Zero)
        {
            throw new ArgumentException("Invalid NetMessage Type: " + data.GetType().Name, nameof(data));
        }

        var size = data.CalculateSize();

        if (size <= 1024)
        {
            var pBytes = stackalloc byte[1024];
            data.WriteTo(new Span<byte>(pBytes, size));

            return Net.SendNetMessage(&filter, handle, pBytes, size);
        }

        var rentedBuffer = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            data.WriteTo(rentedBuffer.AsSpan(0, size));

            fixed (byte* pBytes = rentedBuffer)
            {
                return Net.SendNetMessage(&filter, handle, pBytes, size);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    public static void PrintChannelFilter(RecipientFilter filter,
                                          HudPrintChannel channel,
                                          string          message,
                                          string?         param1 = null,
                                          string?         param2 = null,
                                          string?         param3 = null,
                                          string?         param4 = null)
    {
        var msg = new CUserMessageTextMsg { Dest = (uint) channel, Param = { message } };
        msg.Param.Add(string.IsNullOrWhiteSpace(param1) ? "" : param1);
        msg.Param.Add(string.IsNullOrWhiteSpace(param2) ? "" : param2);
        msg.Param.Add(string.IsNullOrWhiteSpace(param3) ? "" : param3);
        msg.Param.Add(string.IsNullOrWhiteSpace(param4) ? "" : param4);

        SendNetMessage(filter, msg);
    }

    public static void PrintRadioMessage(RecipientFilter filter,
                                         PlayerSlot      client,
                                         string          message,
                                         string?         param1 = null,
                                         string?         param2 = null,
                                         string?         param3 = null,
                                         string?         param4 = null)
    {
        var msg = new CCSUsrMsg_RadioText { MsgDst = 3, Client = client, MsgName = message };

        msg.Params.Add(string.IsNullOrWhiteSpace(param1) ? "" : param1);
        msg.Params.Add(string.IsNullOrWhiteSpace(param2) ? "" : param2);
        msg.Params.Add(string.IsNullOrWhiteSpace(param3) ? "" : param3);
        msg.Params.Add(string.IsNullOrWhiteSpace(param4) ? "" : param4);

        SendNetMessage(filter, msg);
    }

    public static void ServerMessagePrint(RecipientFilter filter, string message)
    {
        var msg = new CSVCMsg_Print { Text = message };
        SendNetMessage(filter, msg);
    }
}
