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

using System.Runtime.InteropServices;
using Sharp.Shared.Enums;

namespace Sharp.Core.Bridges.Forwards.Extern;

internal static class SendProxy
{
    public delegate EHookAction DelegateSendProxyValue(nint ptrClient, int entityIndex, nint ptrField, int fieldType, nint ptrValue);

    public static event DelegateSendProxyValue? OnSendProxyValue;

    [UnmanagedCallersOnly]
    public static EHookAction OnSendProxyValueExport(nint ptrClient, int entityIndex, nint ptrField, int fieldType, nint ptrValue)
        => OnSendProxyValue?.Invoke(ptrClient, entityIndex, ptrField, fieldType, ptrValue) ?? EHookAction.Ignored;
}
