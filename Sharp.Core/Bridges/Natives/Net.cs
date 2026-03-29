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

using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Core.Bridges.Natives;

public static unsafe partial class Net
{
    public static partial bool SendNetMessage(RecipientFilter* filter, nint handle, byte* pData, int size);

    public static partial nint GetNetMessageHandle(string name);

    public static partial void HookNetMessage(ProtobufNetMessageType msgId);

    public static partial void UnhookNetMessage(ProtobufNetMessageType msgId);

    public static partial void SerializeNetMessage(nint data, int size, byte* output);

    public static partial nint GetNetMessageName(nint msg);

    public static partial ulong GetNetMessageSize(nint size);

    public static partial bool NetMessageHasField(nint msg, string field);

    public static partial int NetMessageGetRepeatedCount(nint msg, string field);

    public static partial bool ReadNetMessageInt32(nint msg, string field, out int value, int repeatedIndex);

    public static partial bool ReadNetMessageUInt32(nint msg, string field, out uint value, int repeatedIndex);

    public static partial bool ReadNetMessageInt64(nint msg, string field, out long value, int repeatedIndex);

    public static partial bool ReadNetMessageUInt64(nint msg, string field, out ulong value, int repeatedIndex);

    public static partial bool ReadNetMessageBool(nint msg, string field, out bool value, int repeatedIndex);

    public static partial bool ReadNetMessageEnum(nint msg, string field, out int value, int repeatedIndex);

    public static partial bool ReadNetMessageFloat(nint msg, string field, out float value, int repeatedIndex);

    public static partial bool ReadNetMessageDouble(nint msg, string field, out double value, int repeatedIndex);

    public static partial nint ReadNetMessageString(nint msg, string field, int repeatedIndex);

    public static partial bool ReadNetMessageBytes(nint msg, string field, out nint data, out int size, int repeatedIndex);

    public static partial bool SetNetMessageInt32(nint msg, string field, int value, int repeatedIndex);

    public static partial bool SetNetMessageUInt32(nint msg, string field, uint value, int repeatedIndex);

    public static partial bool SetNetMessageInt64(nint msg, string field, long value, int repeatedIndex);

    public static partial bool SetNetMessageUInt64(nint msg, string field, ulong value, int repeatedIndex);

    public static partial bool SetNetMessageBool(nint msg, string field, bool value, int repeatedIndex);

    public static partial bool SetNetMessageEnum(nint msg, string field, int value, int repeatedIndex);

    public static partial bool SetNetMessageFloat(nint msg, string field, float value, int repeatedIndex);

    public static partial bool SetNetMessageDouble(nint msg, string field, double value, int repeatedIndex);

    public static partial bool SetNetMessageString(nint msg, string field, string value, int repeatedIndex);

    public static partial bool SetNetMessageBytes(nint msg, string field, byte* data, int size, int repeatedIndex);

    public static partial int AddNetMessageInt32(nint msg, string field, int value);

    public static partial int AddNetMessageUInt32(nint msg, string field, uint value);

    public static partial int AddNetMessageInt64(nint msg, string field, long value);

    public static partial int AddNetMessageUInt64(nint msg, string field, ulong value);

    public static partial int AddNetMessageBool(nint msg, string field, bool value);

    public static partial int AddNetMessageEnum(nint msg, string field, int value);

    public static partial int AddNetMessageFloat(nint msg, string field, float value);

    public static partial int AddNetMessageDouble(nint msg, string field, double value);

    public static partial int AddNetMessageString(nint msg, string field, string value);

    public static partial int AddNetMessageBytes(nint msg, string field, byte* data, int size);

    public static partial nint ReadNetMessageMessage(nint msg, string field, int repeatedIndex);

    public static partial nint AddNetMessageRepeated(nint msg, string field);

    public static partial bool RemoveNetMessageRepeated(nint msg, string field, int repeatedIndex);

    public static partial bool CopyFromOtherMessage(nint msg, byte* data, int size);
}
