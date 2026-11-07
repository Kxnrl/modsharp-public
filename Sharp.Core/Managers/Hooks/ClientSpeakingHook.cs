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
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;

namespace Sharp.Core.Managers.Hooks;

internal class ClientSpeakingHook : HookType<ClientCanSpeakHookParams, EmptyHookReturn, ClientSpeakingHook>
{
    public ClientSpeakingHook(ILoggerFactory factory) : base(factory)
    {
        Bridges.Forwards.Client.OnClientSpeakPre  += CServerSideClient_SpeakPre;
        Bridges.Forwards.Client.OnClientSpeakPost += CServerSideClient_SpeakPost;
    }

    private EHookAction CServerSideClient_SpeakPre(nint ptrClient, ulong xuid, uint sectionNumber, nint pVoiceData, int size)
    {
        if (!IsHookInvokeRequired(false))
        {
            return EHookAction.Ignored;
        }

        var param = new ClientCanSpeakHookParams(false, ptrClient, xuid, sectionNumber, pVoiceData, size);

        var result = InvokeHookPre(param);

        param.MarkAsDisposed();

        return result.Action;
    }

    private void CServerSideClient_SpeakPost(nint ptrClient,
        ulong                                     xuid,
        uint                                      sectionNumber,
        nint                                      pVoiceData,
        int                                       size,
        EHookAction                               action)
    {
        if (!IsHookInvokeRequired(true))
        {
            return;
        }

        var param = new ClientCanSpeakHookParams(true, ptrClient, xuid, sectionNumber, pVoiceData, size);

        InvokeHookPost(param, new HookReturnValue<EmptyHookReturn>(action));

        param.MarkAsDisposed();
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => true;

    protected override HookActionFlags SupportedActions => HookActionFlags.Ignored | HookActionFlags.SkipCallReturnOverride;
}

internal sealed class ClientCanSpeakHookParams : ClientFunctionParams, IClientSpeakingHookParams
{
    public ClientCanSpeakHookParams(bool postHook, nint client, ulong xuid, uint sectionNumber, IntPtr pVoiceData, int size) :
        base(postHook, client)
    {
        Xuid          = xuid;
        SectionNumber = sectionNumber;
        PVoiceData    = pVoiceData;
        Size          = size;
    }

    public ulong Xuid          { get; set; }
    public uint  SectionNumber { get; set; }
    public nint  PVoiceData    { get; set; }
    public int   Size          { get; set; }
}
