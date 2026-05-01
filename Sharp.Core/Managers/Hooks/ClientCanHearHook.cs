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

using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Core.Managers.Hooks;

internal class ClientCanHearHook : HookType<IClientCanHearHookParams, bool, ClientCanHearHook>
{
    public ClientCanHearHook(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Client.OnClientCanHear += CServerSideClient_CanHear;

    private HookReturnValue<bool> CServerSideClient_CanHear(nint ptrClient, nint ptrSource)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<bool>(EHookAction.Ignored, true);
        }

        var param  = new ClientCanHearHookParams(false, ptrClient, ptrSource);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        return result;
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => false;
}

internal sealed class ClientCanHearHookParams : ClientFunctionParams, IClientCanHearHookParams
{
    private readonly nint _ptrSpeaker;

    public ClientCanHearHookParams(bool postHook, nint client, nint speaker) : base(postHook, client)
        => _ptrSpeaker = speaker;

    private IGameClient? _speaker;

    public IGameClient Speaker
    {
        get
        {
            if (_speaker is { } speaker)
            {
                return speaker;
            }

            CheckDisposed();
            _speaker = CreateClient(_ptrSpeaker);

            return _speaker;
        }
    }
}
