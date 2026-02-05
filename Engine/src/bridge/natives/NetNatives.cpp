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

#include "bridge/natives/NetNatives.h"
#include "bridge/adapter.h"

#include "hook/network.h"
#include "logging.h"
#include "protobufproxy.h"

#include "CoreCLR/RuntimeProtobufMessage.h"
#include "CoreCLR/RuntimeRecipientFilter.h"

#include "cstrike/interface/IRecipientFilter.h"

namespace natives::net
{
static bool SendNetMessage(const RuntimeRecipientFilter* pFilter, INetworkMessageInternal* pNetMessage, uint8_t* pData, int32_t size)
{
    if (!pNetMessage) [[unlikely]]
        return false;

    int32_t           bitWide = 0;
    NetworkReceiver_t clients = 0;
    if (!ParseNetworkReceivers(pFilter, &bitWide, &clients))
        return false;

    const CSharpNetworkDataWrapper data(pData, size, pNetMessage);
    SetNetworkMessageBypassHook(true);
    g_pGameEventSystem->PostEventAbstract(0, false, bitWide, &clients, pNetMessage, &data, 0, true);
    SetNetworkMessageBypassHook(false);

    return true;
}

static void* GetNetMessageHandle(const char* name)
{
    return GetRuntimeProtobufSerializable(name);
}

static void HookNetMessage(uint16_t netMsgId)
{
    AddHookedNetMsgId(netMsgId);
}

static void UnhookNetMessage(uint16_t netMsgId)
{
    RemoveHookedNetMsgId(netMsgId);
}

static void SerializeNetMessage(const google::protobuf::Message* message, int32_t nSize, void* pOutput)
{
    AssertPtr(message);
    AssertPtr(pOutput);

    message->SerializeToArray(pOutput, nSize);
}

static const char* GetNetMessageName(const google::protobuf::Message* message)
{
    AssertPtr(message);
    static std::string out;
    out.clear();

    out = message->GetTypeName();

    return out.c_str();
}

static uint64_t GetNetMessageSize(const google::protobuf::Message* message)
{
    AssertPtr(message);

    return message->ByteSizeLong();
}

static bool NetMessageHasField(const google::protobuf::Message* message, const char* field)
{
    return ProtobufHasField(message, field);
}

static int32_t NetMessageGetRepeatedCount(const google::protobuf::Message* message, const char* field)
{
    return ProtobufGetFieldRepeatedCount(message, field);
}

static bool ReadNetMessageInt32(const google::protobuf::Message* message, const char* field, int32_t* out, int32_t repeatedIndex)
{
    return ProtobufReadFieldInt32(message, field, out, repeatedIndex);
}

static bool ReadNetMessageUInt32(const google::protobuf::Message* message, const char* field, uint32_t* out, int32_t repeatedIndex)
{
    return ProtobufReadFieldUInt32(message, field, out, repeatedIndex);
}

static bool ReadNetMessageBool(const google::protobuf::Message* message, const char* field, bool* out, int32_t repeatedIndex)
{
    return ProtobufReadFieldBool(message, field, out, repeatedIndex);
}

static bool ReadNetMessageEnum(const google::protobuf::Message* message, const char* field, int32_t* out, int32_t repeatedIndex)
{
    return ProtobufReadFieldEnum(message, field, out, repeatedIndex);
}

static bool ReadNetMessageFloat(const google::protobuf::Message* message, const char* field, float* out, int32_t repeatedIndex)
{
    return ProtobufReadFieldFloat(message, field, out, repeatedIndex);
}

static bool ReadNetMessageDouble(const google::protobuf::Message* message, const char* field, double* out, int32_t repeatedIndex)
{
    return ProtobufReadFieldDouble(message, field, out, repeatedIndex);
}

static bool ReadNetMessageInt64(const google::protobuf::Message* message, const char* field, int64_t* out, int32_t repeatedIndex)
{
    return ProtobufReadFieldInt64(message, field, out, repeatedIndex);
}

static bool ReadNetMessageUInt64(const google::protobuf::Message* message, const char* field, uint64_t* out, int32_t repeatedIndex)
{
    return ProtobufReadFieldUInt64(message, field, out, repeatedIndex);
}

static const char* ReadNetMessageString(const google::protobuf::Message* message, const char* field, int32_t repeatedIndex)
{
    static std::string out;
    out.clear();

    if (!ProtobufReadFieldString(message, field, &out, repeatedIndex))
        return nullptr;

    return out.c_str();
}

static bool ReadNetMessageBytes(const google::protobuf::Message* message, const char* field, char** data, int32_t* size, int32_t repeatedIndex)
{
    static std::string out;
    out.clear();

    if (!ProtobufReadFieldBytes(message, field, &out, repeatedIndex))
        return false;

    if (out.empty())
        return false;

    *size = static_cast<int32_t>(out.size());
    *data = out.data();

    return true;
}

static bool SetNetMessageInt32(google::protobuf::Message* message, const char* field, int32_t value, int32_t repeatedIndex)
{
    return ProtobufSetFieldInt32(message, field, value, repeatedIndex);
}

static bool SetNetMessageUInt32(google::protobuf::Message* message, const char* field, uint32_t value, int32_t repeatedIndex)
{
    return ProtobufSetFieldUInt32(message, field, value, repeatedIndex);
}

static bool SetNetMessageInt64(google::protobuf::Message* message, const char* field, int64_t value, int32_t repeatedIndex)
{
    return ProtobufSetFieldInt64(message, field, value, repeatedIndex);
}

static bool SetNetMessageUInt64(google::protobuf::Message* message, const char* field, uint64_t value, int32_t repeatedIndex)
{
    return ProtobufSetFieldUInt64(message, field, value, repeatedIndex);
}

static bool SetNetMessageBool(google::protobuf::Message* message, const char* field, bool value, int32_t repeatedIndex)
{
    return ProtobufSetFieldBool(message, field, value, repeatedIndex);
}

static bool SetNetMessageEnum(google::protobuf::Message* message, const char* field, int32_t value, int32_t repeatedIndex)
{
    return ProtobufSetFieldEnum(message, field, value, repeatedIndex);
}

static bool SetNetMessageFloat(google::protobuf::Message* message, const char* field, float value, int32_t repeatedIndex)
{
    return ProtobufSetFieldFloat(message, field, value, repeatedIndex);
}

static bool SetNetMessageDouble(google::protobuf::Message* message, const char* field, double value, int32_t repeatedIndex)
{
    return ProtobufSetFieldDouble(message, field, value, repeatedIndex);
}

static bool SetNetMessageString(google::protobuf::Message* message, const char* field, const char* value, int32_t repeatedIndex)
{
    return ProtobufSetFieldString(message, field, value, repeatedIndex);
}

static bool SetNetMessageBytes(google::protobuf::Message* message, const char* field, const char* value, int size, int32_t repeatedIndex)
{
    const std::string data(value, size);
    return ProtobufSetFieldBytes(message, field, data, repeatedIndex);
}

static int32_t AddNetMessageInt32(google::protobuf::Message* message, const char* field, int32_t value)
{
    return ProtobufAddFieldInt32(message, field, value);
}

static int32_t AddNetMessageUInt32(google::protobuf::Message* message, const char* field, uint32_t value)
{
    return ProtobufAddFieldUInt32(message, field, value);
}

static int32_t AddNetMessageInt64(google::protobuf::Message* message, const char* field, int64_t value)
{
    return ProtobufAddFieldInt64(message, field, value);
}

static int32_t AddNetMessageUInt64(google::protobuf::Message* message, const char* field, uint64_t value)
{
    return ProtobufAddFieldUInt64(message, field, value);
}

static int32_t AddNetMessageBool(google::protobuf::Message* message, const char* field, bool value)
{
    return ProtobufAddFieldBool(message, field, value);
}

static int32_t AddNetMessageEnum(google::protobuf::Message* message, const char* field, int32_t value)
{
    return ProtobufAddFieldEnum(message, field, value);
}

static int32_t AddNetMessageFloat(google::protobuf::Message* message, const char* field, float value)
{
    return ProtobufAddFieldFloat(message, field, value);
}

static int32_t AddNetMessageDouble(google::protobuf::Message* message, const char* field, double value)
{
    return ProtobufAddFieldDouble(message, field, value);
}

static int32_t AddNetMessageString(google::protobuf::Message* message, const char* field, const char* value)
{
    return ProtobufAddFieldString(message, field, value);
}

static int32_t AddNetMessageBytes(google::protobuf::Message* message, const char* field, const char* value, int size)
{
    const std::string data(value, size);
    return ProtobufAddFieldBytes(message, field, data);
}

static void* ReadNetMessageMessage(google::protobuf::Message* message, const char* field, int32_t repeatedIndex)
{
    return ProtobufReadFieldMessage(message, field, repeatedIndex);
}

static void* AddNetMessageRepeated(google::protobuf::Message* message, const char* field)
{
    return ProtobufAddRepeatedFieldMessage(message, field);
}

static bool RemoveNetMessageRepeated(google::protobuf::Message* message, const char* field, int32_t repeatedIndex)
{
    return ProtobufRemoveNetMessageRepeatedField(message, field, repeatedIndex);
}

static bool CopyFromOtherMessage(google::protobuf::Message* message, uint8_t* data, int32_t size)
{
    return ProtobufCopyFromOtherMessage(message, data, size);
}

void Init()
{
    bridge::CreateNative("Net.SendNetMessage", reinterpret_cast<void*>(SendNetMessage));
    bridge::CreateNative("Net.GetNetMessageHandle", reinterpret_cast<void*>(GetNetMessageHandle));
    
    bridge::CreateNative("Net.HookNetMessage", reinterpret_cast<void*>(HookNetMessage));
    bridge::CreateNative("Net.UnhookNetMessage", reinterpret_cast<void*>(UnhookNetMessage));

    bridge::CreateNative("Net.SerializeNetMessage", reinterpret_cast<void*>(SerializeNetMessage));

    bridge::CreateNative("Net.GetNetMessageName", reinterpret_cast<void*>(GetNetMessageName));
    bridge::CreateNative("Net.GetNetMessageSize", reinterpret_cast<void*>(GetNetMessageSize));

    bridge::CreateNative("Net.NetMessageHasField", reinterpret_cast<void*>(NetMessageHasField));
    bridge::CreateNative("Net.NetMessageGetRepeatedCount", reinterpret_cast<void*>(NetMessageGetRepeatedCount));

    bridge::CreateNative("Net.ReadNetMessageInt32", reinterpret_cast<void*>(ReadNetMessageInt32));
    bridge::CreateNative("Net.ReadNetMessageUInt32", reinterpret_cast<void*>(ReadNetMessageUInt32));
    bridge::CreateNative("Net.ReadNetMessageInt64", reinterpret_cast<void*>(ReadNetMessageInt64));
    bridge::CreateNative("Net.ReadNetMessageUInt64", reinterpret_cast<void*>(ReadNetMessageUInt64));
    bridge::CreateNative("Net.ReadNetMessageBool", reinterpret_cast<void*>(ReadNetMessageBool));
    bridge::CreateNative("Net.ReadNetMessageEnum", reinterpret_cast<void*>(ReadNetMessageEnum));
    bridge::CreateNative("Net.ReadNetMessageFloat", reinterpret_cast<void*>(ReadNetMessageFloat));
    bridge::CreateNative("Net.ReadNetMessageDouble", reinterpret_cast<void*>(ReadNetMessageDouble));
    bridge::CreateNative("Net.ReadNetMessageString", reinterpret_cast<void*>(ReadNetMessageString));
    bridge::CreateNative("Net.ReadNetMessageBytes", reinterpret_cast<void*>(ReadNetMessageBytes));

    bridge::CreateNative("Net.SetNetMessageInt32", reinterpret_cast<void*>(SetNetMessageInt32));
    bridge::CreateNative("Net.SetNetMessageUInt32", reinterpret_cast<void*>(SetNetMessageUInt32));
    bridge::CreateNative("Net.SetNetMessageInt64", reinterpret_cast<void*>(SetNetMessageInt64));
    bridge::CreateNative("Net.SetNetMessageUInt64", reinterpret_cast<void*>(SetNetMessageUInt64));
    bridge::CreateNative("Net.SetNetMessageBool", reinterpret_cast<void*>(SetNetMessageBool));
    bridge::CreateNative("Net.SetNetMessageEnum", reinterpret_cast<void*>(SetNetMessageEnum));
    bridge::CreateNative("Net.SetNetMessageFloat", reinterpret_cast<void*>(SetNetMessageFloat));
    bridge::CreateNative("Net.SetNetMessageDouble", reinterpret_cast<void*>(SetNetMessageDouble));
    bridge::CreateNative("Net.SetNetMessageString", reinterpret_cast<void*>(SetNetMessageString));
    bridge::CreateNative("Net.SetNetMessageBytes", reinterpret_cast<void*>(SetNetMessageBytes));

    bridge::CreateNative("Net.AddNetMessageInt32", reinterpret_cast<void*>(AddNetMessageInt32));
    bridge::CreateNative("Net.AddNetMessageUInt32", reinterpret_cast<void*>(AddNetMessageUInt32));
    bridge::CreateNative("Net.AddNetMessageInt64", reinterpret_cast<void*>(AddNetMessageInt64));
    bridge::CreateNative("Net.AddNetMessageUInt64", reinterpret_cast<void*>(AddNetMessageUInt64));
    bridge::CreateNative("Net.AddNetMessageBool", reinterpret_cast<void*>(AddNetMessageBool));
    bridge::CreateNative("Net.AddNetMessageEnum", reinterpret_cast<void*>(AddNetMessageEnum));
    bridge::CreateNative("Net.AddNetMessageFloat", reinterpret_cast<void*>(AddNetMessageFloat));
    bridge::CreateNative("Net.AddNetMessageDouble", reinterpret_cast<void*>(AddNetMessageDouble));
    bridge::CreateNative("Net.AddNetMessageString", reinterpret_cast<void*>(AddNetMessageString));
    bridge::CreateNative("Net.AddNetMessageBytes", reinterpret_cast<void*>(AddNetMessageBytes));

    bridge::CreateNative("Net.ReadNetMessageMessage", reinterpret_cast<void*>(ReadNetMessageMessage));
    bridge::CreateNative("Net.AddNetMessageRepeated", reinterpret_cast<void*>(AddNetMessageRepeated));
    bridge::CreateNative("Net.RemoveNetMessageRepeated", reinterpret_cast<void*>(RemoveNetMessageRepeated));
    bridge::CreateNative("Net.CopyFromOtherMessage", reinterpret_cast<void*>(CopyFromOtherMessage));
}
} // namespace natives::net