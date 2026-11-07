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
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Core.Managers.Hooks;

internal class TextMsgHook : HookType<TextMsgHookParams, NetworkReceiver, TextMsgHook>
{
    public TextMsgHook(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Network.OnTextMsg += NetworkMessages_TextMsg;

    private HookReturnValue<NetworkReceiver> NetworkMessages_TextMsg(HudPrintChannel channel,
        string                                                                       name,
        NetworkReceiver                                                              nReceivers)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<NetworkReceiver>(EHookAction.Ignored);
        }

        var param  = new TextMsgHookParams(false, channel, name, nReceivers);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        return result;
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => false;

    protected override HookActionFlags SupportedActions
        => HookActionFlags.Ignored | HookActionFlags.SkipCallReturnOverride | HookActionFlags.ChangeParamReturnDefault;
}

internal sealed class TextMsgHookParams : FunctionParams, ITextMsgHookParams
{
    public TextMsgHookParams(bool postHook,
        HudPrintChannel           channel,
        string                    name,
        NetworkReceiver           receivers) : base(postHook)
    {
        Channel   = channel;
        Name      = name;
        Receivers = receivers;
    }

    public HudPrintChannel Channel   { get; }
    public string          Name      { get; }
    public NetworkReceiver Receivers { get; }

    public bool HasClient(PlayerSlot slot)
        => Receivers.HasClient(slot);
}
