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

#include "protobufproxy.h"

#include <proto/networkbasetypes.pb.h>

#include <google/protobuf/descriptor.h>
#include <google/protobuf/message.h>

#ifndef PLATFORM_WINDOWS
static_assert(_GLIBCXX_USE_CXX11_ABI == 0, "engine protobuf requires the pre-C++11 COW string ABI (_GLIBCXX_USE_CXX11_ABI=0)");
static_assert(sizeof(std::string) == 8, "std::string layout is not the COW ABI - toolchain mismatch with the engine");
#endif

bool ProtobufHasField(const google::protobuf::Message* message, const char* field)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (!fieldDescriptor)
        return false;

    return reflection->HasField(*message, fieldDescriptor);
}

int32_t ProtobufGetFieldRepeatedCount(const google::protobuf::Message* message, const char* field)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (!fieldDescriptor)
        return -1;

    if (fieldDescriptor->label() != google::protobuf::FieldDescriptor::LABEL_REPEATED)
        return -1;

    return reflection->FieldSize(*message, fieldDescriptor);
}

bool ProtobufReadFieldBool(const google::protobuf::Message* message, const char* field, bool* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_BOOL)
        {
            if (!fieldDescriptor->is_repeated())
            {
                *out = reflection->GetBool(*message, fieldDescriptor);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                *out = reflection->GetRepeatedBool(*message, fieldDescriptor, repeatedIndex);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufReadFieldEnum(const google::protobuf::Message* message, const char* field, int32_t* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_ENUM)
        {
            if (!fieldDescriptor->is_repeated())
            {
                *out = reflection->GetEnumValue(*message, fieldDescriptor);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                *out = reflection->GetRepeatedEnumValue(*message, fieldDescriptor, repeatedIndex);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufReadFieldInt32(const google::protobuf::Message* message, const char* field, int32_t* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_INT32
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SFIXED32
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SINT32)
        {
            if (!fieldDescriptor->is_repeated())
            {
                *out = reflection->GetInt32(*message, fieldDescriptor);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                *out = reflection->GetRepeatedInt32(*message, fieldDescriptor, repeatedIndex);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufReadFieldUInt32(const google::protobuf::Message* message, const char* field, uint32_t* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_UINT32 || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_FIXED32)
        {
            if (!fieldDescriptor->is_repeated())
            {
                *out = reflection->GetUInt32(*message, fieldDescriptor);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                *out = reflection->GetRepeatedUInt32(*message, fieldDescriptor, repeatedIndex);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufReadFieldFloat(const google::protobuf::Message* message, const char* field, float* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_FLOAT)
        {
            if (!fieldDescriptor->is_repeated())
            {
                *out = reflection->GetFloat(*message, fieldDescriptor);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                *out = reflection->GetRepeatedFloat(*message, fieldDescriptor, repeatedIndex);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufReadFieldDouble(const google::protobuf::Message* message, const char* field, double* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_DOUBLE)
        {
            if (!fieldDescriptor->is_repeated())
            {
                *out = reflection->GetDouble(*message, fieldDescriptor);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                *out = reflection->GetRepeatedDouble(*message, fieldDescriptor, repeatedIndex);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufReadFieldInt64(const google::protobuf::Message* message, const char* field, int64_t* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_INT64
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SFIXED64
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SINT64)
        {
            if (!fieldDescriptor->is_repeated())
            {
                *out = reflection->GetInt64(*message, fieldDescriptor);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                *out = reflection->GetRepeatedInt64(*message, fieldDescriptor, repeatedIndex);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufReadFieldUInt64(const google::protobuf::Message* message, const char* field, uint64_t* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_UINT64 || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_FIXED64)
        {
            if (!fieldDescriptor->is_repeated())
            {
                *out = reflection->GetUInt64(*message, fieldDescriptor);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                *out = reflection->GetRepeatedUInt64(*message, fieldDescriptor, repeatedIndex);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufReadFieldString(const google::protobuf::Message* message, const char* field, std::string* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_STRING)
        {
            if (!fieldDescriptor->is_repeated())
            {
                std::string scratch;
                *out = reflection->GetStringReference(*message, fieldDescriptor, &scratch);
                return true;
            }
            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                std::string scratch;
                *out = reflection->GetRepeatedStringReference(*message, fieldDescriptor, repeatedIndex, &scratch);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufReadFieldBytes(const google::protobuf::Message* message, const char* field, std::string* out, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_STRING
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_BYTES)
        {
            if (!fieldDescriptor->is_repeated())
            {
                std::string scratch;
                *out = reflection->GetStringReference(*message, fieldDescriptor, &scratch);
                return true;
            }
            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                std::string scratch;
                *out = reflection->GetRepeatedStringReference(*message, fieldDescriptor, repeatedIndex, &scratch);
                return true;
            }
        }
    }

    return false;
}

bool ProtobufSetFieldBool(google::protobuf::Message* message, const char* field, bool value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_BOOL)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetBool(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedBool(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}

bool ProtobufSetFieldEnum(google::protobuf::Message* message, const char* field, int32_t value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_ENUM)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetEnumValue(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedEnumValue(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}

bool ProtobufSetFieldInt32(google::protobuf::Message* message, const char* field, int32_t value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_INT32
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SFIXED32
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SINT32)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetInt32(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedInt32(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}

bool ProtobufSetFieldUInt32(google::protobuf::Message* message, const char* field, uint32_t value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_UINT32 || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_FIXED32)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetUInt32(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedUInt32(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufSetFieldFloat(google::protobuf::Message* message, const char* field, float value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_FLOAT)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetFloat(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedFloat(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufSetFieldDouble(google::protobuf::Message* message, const char* field, double value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_DOUBLE)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetDouble(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedDouble(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufSetFieldInt64(google::protobuf::Message* message, const char* field, int64_t value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_INT64
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SFIXED64
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SINT64)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetInt64(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedInt64(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufSetFieldUInt64(google::protobuf::Message* message, const char* field, uint64_t value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_UINT64 || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_FIXED64)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetUInt64(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedUInt64(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufSetFieldString(google::protobuf::Message* message, const char* field, const char* value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_STRING)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetString(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedString(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}
bool ProtobufSetFieldBytes(google::protobuf::Message* message, const char* field, const std::string& value, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_STRING || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_BYTES)
        {
            if (!fieldDescriptor->is_repeated())
            {
                reflection->SetString(message, fieldDescriptor, value);
                return true;
            }

            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                reflection->SetRepeatedString(message, fieldDescriptor, repeatedIndex, value);
                return true;
            }
        }
    }

    return false;
}

int ProtobufAddFieldBool(google::protobuf::Message* message, const char* field, bool value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_BOOL)
        {
            reflection->AddBool(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}
int ProtobufAddFieldEnum(google::protobuf::Message* message, const char* field, int32_t value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_ENUM)
        {
            reflection->AddEnumValue(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}
int ProtobufAddFieldInt32(google::protobuf::Message* message, const char* field, int32_t value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_INT32
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SFIXED32
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SINT32)
        {
            reflection->AddInt32(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}
int ProtobufAddFieldUInt32(google::protobuf::Message* message, const char* field, uint32_t value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_UINT32 || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_FIXED32)
        {
            reflection->AddUInt32(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}
int ProtobufAddFieldFloat(google::protobuf::Message* message, const char* field, float value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_FLOAT)
        {
            reflection->AddFloat(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}
int ProtobufAddFieldDouble(google::protobuf::Message* message, const char* field, double value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_DOUBLE)
        {
            reflection->AddDouble(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}
int ProtobufAddFieldInt64(google::protobuf::Message* message, const char* field, int64_t value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_INT64
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SFIXED64
            || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_SINT64)
        {
            reflection->AddInt64(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}
int ProtobufAddFieldUInt64(google::protobuf::Message* message, const char* field, uint64_t value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_UINT64 || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_FIXED64)
        {
            reflection->AddUInt64(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}
int ProtobufAddFieldString(google::protobuf::Message* message, const char* field, const char* value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_STRING)
        {
            reflection->AddString(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}
int ProtobufAddFieldBytes(google::protobuf::Message* message, const char* field, const std::string& value)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_STRING || fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_BYTES)
        {
            reflection->AddString(message, fieldDescriptor, value);
            return reflection->FieldSize(*message, fieldDescriptor) - 1;
        }
    }
    return -1;
}

google::protobuf::Message* ProtobufReadFieldMessage(google::protobuf::Message* message, const char* field, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_MESSAGE)
        {
            if (!fieldDescriptor->is_repeated())
            {
                return reflection->MutableMessage(message, fieldDescriptor);
            }
            if (repeatedIndex >= 0 && repeatedIndex < reflection->FieldSize(*message, fieldDescriptor))
            {
                return reflection->MutableRepeatedMessage(message, fieldDescriptor, repeatedIndex);
            }
        }
    }

    return nullptr;
}

google::protobuf::Message* ProtobufAddRepeatedFieldMessage(google::protobuf::Message* message, const char* field)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        if (fieldDescriptor->type() == google::protobuf::FieldDescriptor::Type::TYPE_MESSAGE)
        {
            return reflection->AddMessage(message, fieldDescriptor);
        }
    }
    return nullptr;
}

bool ProtobufRemoveNetMessageRepeatedField(google::protobuf::Message* message, const char* field, int repeatedIndex)
{
    const auto descriptor      = message->GetDescriptor();
    const auto reflection      = message->GetReflection();
    const auto fieldDescriptor = descriptor->FindFieldByName(field);

    if (fieldDescriptor != nullptr && fieldDescriptor->label() == google::protobuf::FieldDescriptor::LABEL_REPEATED)
    {
        const auto count = reflection->FieldSize(*message, fieldDescriptor);
        if (repeatedIndex >= 0 && repeatedIndex < count)
        {
            for (auto i = repeatedIndex; i < count - 1; i++)
            {
                reflection->SwapElements(message, fieldDescriptor, i, i + 1);
            }
            reflection->RemoveLast(message, fieldDescriptor);
            return true;
        }
    }
    return false;
}

bool ProtobufCopyFromOtherMessage(google::protobuf::Message* message, uint8_t* data, int32_t size)
{
    message->Clear();

    google::protobuf::io::CodedInputStream stream(data, size);
    return message->MergeFromCodedStream(&stream);
}