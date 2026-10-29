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

using Sharp.Core.Objects;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Units;

namespace Sharp.Core.GameEvents;

[GameEventName("player_team")]
internal partial class EventPlayerTeam : GameEvent, IEventPlayerTeam
{
    [GameEventProperty("userid", true)]
    private IPlayerController? _controller;

    [GameEventProperty("userid", true)]
    private IPlayerPawn? _pawn;

    [GameEventProperty("userid")]
    private UserID? _userId;

    [GameEventProperty("team")]
    private CStrikeTeam? _newTeam;

    [GameEventProperty("oldteam")]
    private CStrikeTeam? _oldTeam;

    [GameEventProperty("disconnect")]
    private bool? _disconnect;

    [GameEventProperty("silent")]
    private bool? _silent;

    [GameEventProperty("isbot")]
    private bool? _bot;
}
