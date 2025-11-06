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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Shared.Types;

[StructLayout(LayoutKind.Explicit, Pack = 16, Size = 16)]
public readonly struct RecipientFilter
{
    [FieldOffset(0)]
    public readonly RecipientFilterType Type;

    [FieldOffset(4)]
    public readonly CStrikeTeam Team;

    [FieldOffset(8)]
    public readonly NetworkReceiver Receivers;

    [FieldOffset(8)]
    public readonly ulong ReceiverSlot;

    public RecipientFilter(CStrikeTeam team)
    {
        Type      = RecipientFilterType.Team;
        Team      = team;
        Receivers = default;
    }

    public RecipientFilter(PlayerSlot slot)
    {
        Type         = RecipientFilterType.Single;
        Team         = 0;
        ReceiverSlot = slot.AsPrimitive();
    }

    public RecipientFilter(IGameClient client)
    {
        Type         = RecipientFilterType.Single;
        Team         = 0;
        ReceiverSlot = client.Slot.AsPrimitive();
    }

    public RecipientFilter(EntityIndex index)
    {
        Type         = RecipientFilterType.Single;
        Team         = 0;
        ReceiverSlot = (ulong) index.AsPrimitive() - 1;
    }

    public RecipientFilter(NetworkReceiver receiver)
    {
        Type      = RecipientFilterType.Players;
        Team      = 0;
        Receivers = receiver;
    }

    public RecipientFilter(IEnumerable<PlayerSlot> players)
    {
        Type      = RecipientFilterType.Players;
        Team      = 0;
        Receivers = new NetworkReceiver(players);
    }

    public RecipientFilter(IReadOnlyCollection<PlayerSlot> players)
    {
        Type      = RecipientFilterType.Players;
        Team      = 0;
        Receivers = new NetworkReceiver(players);
    }

    public RecipientFilter(IEnumerable<IGameClient> players)
    {
        Type      = RecipientFilterType.Players;
        Team      = 0;
        Receivers = new NetworkReceiver(players.Where(x => !x.IsFakeClient).Select(x => x.Slot));
    }

    public RecipientFilter()
    {
        Type      = RecipientFilterType.All;
        Team      = 0;
        Receivers = default;
    }

    public bool IsEmpty()
    {
        switch (Type)
        {
            case RecipientFilterType.All:
                return false;
            case RecipientFilterType.Team:
                return Team == CStrikeTeam.UnAssigned;
            case RecipientFilterType.Players:
                return Receivers.IsEmpty();
            case RecipientFilterType.Single:
                return false;
        }

        throw new InvalidEnumArgumentException();
    }

    public string DestructureTransform()
        => Type switch
        {
            RecipientFilterType.All  => $"{{ \"Type\": {Type} }}",
            RecipientFilterType.Team => $"{{ \"Type\": {Type}, \"Team\": {Team} }}",
            RecipientFilterType.Players =>
                $"{{ \"Type\": {Type}, \"Receivers\": [{string.Join(',', Receivers.GetClients())}] }}",
            RecipientFilterType.Single => $"{{ \"Type\": {Type}, \"Player\": {ReceiverSlot} }}",
            _                          => throw new InvalidEnumArgumentException(),
        };
}
