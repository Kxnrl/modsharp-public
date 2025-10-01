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

#ifndef CSTRIKE_INTERFACE_NETCHANNEL_H
#define CSTRIKE_INTERFACE_NETCHANNEL_H

enum NetChannelBufType_t
{
    BUF_RELIABLE = 0,
    BUF_UNRELIABLE,
    BUF_VOICE,
};

namespace google::protobuf
{
class Message;
}

class INetChannel
{
public:
    virtual const char* GetName()           = 0;
    virtual const char* GetNetWorkAddress() = 0;
    virtual void        GetTime()           = 0;
    virtual float       GetTimeConnected()  = 0;

    // virtual float GetAvgLatency(int type) = 0; // 11

    void SendNetMessage(class INetworkSerializable* pSerializable, google::protobuf::Message* pMessage, int reliable = -1);
};

#endif
