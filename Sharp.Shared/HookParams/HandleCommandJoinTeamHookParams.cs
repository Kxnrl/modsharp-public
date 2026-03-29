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

namespace Sharp.Shared.HookParams;

public interface IHandleCommandJoinTeamHookParams : IFunctionParams, IPlayerFunctionParams
{
    /// <summary>
    /// Gets the index of the team the player is attempting to join. 
    /// A value of 0 indicates the "Auto Select" option was chosen, allowing the game logic to assign the team.
    /// </summary>
    int  Team      { get; }
    bool Queue     { get; }
    int  CoachTeam { get; }

    void OverrideTeam(int team);

    void OverrideQueue(bool queue);

    void OverrideCoachTeam(int coachTeam);
}
