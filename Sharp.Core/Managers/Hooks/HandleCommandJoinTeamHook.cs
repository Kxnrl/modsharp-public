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

namespace Sharp.Core.Managers.Hooks;

internal class HandleCommandJoinTeamHook : HookType<HandleCommandJoinTeamHookParams, bool, HandleCommandJoinTeamHook>
{
    public HandleCommandJoinTeamHook(ILoggerFactory factory) : base(factory)
    {
        Bridges.Forwards.Extern.HandleCommandJoinTeam.OnHandleCommandJoinTeamPre
            += HandleCommandJoinTeam_OnHandleCommandJoinTeamPre;

        Bridges.Forwards.Extern.HandleCommandJoinTeam.OnHandleCommandJoinTeamPost
            += HandleCommandJoinTeam_OnHandleCommandJoinTeamPost;
    }

    private HookReturnValue<bool> HandleCommandJoinTeam_OnHandleCommandJoinTeamPre(nint ptrClient,
        nint                                                                            ptrController,
        ref int                                                                         team,
        ref JoinTeamFlags                                                               flags,
        ref int                                                                         coachTeam)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<bool>(EHookAction.Ignored, false);
        }

        var param  = new HandleCommandJoinTeamHookParams(false, ptrClient, ptrController, team, flags, coachTeam);
        var result = InvokeHookPre(param);

        // restore
        team      = param.Team;
        flags     = param.Flags;
        coachTeam = param.CoachTeam;

        param.MarkAsDisposed();

        return result;
    }

    private void HandleCommandJoinTeam_OnHandleCommandJoinTeamPost(nint ptrClient,
        nint                                                            ptrController,
        int                                                             team,
        JoinTeamFlags                                                   flags,
        int                                                             coachTeam,
        EHookAction                                                     hookAction,
        bool                                                            returnValue)
    {
        if (!IsHookInvokeRequired(true))
        {
            return;
        }

        var param = new HandleCommandJoinTeamHookParams(true, ptrClient, ptrController, team, flags, coachTeam);

        InvokeHookPost(param, new HookReturnValue<bool>(hookAction, returnValue));

        param.MarkAsDisposed();
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => true;

    protected override HookActionFlags SupportedActions
        => HookActionFlags.Ignored
           | HookActionFlags.SkipCallReturnOverride
           | HookActionFlags.ChangeParamReturnDefault
           | HookActionFlags.ChangeParamReturnOverride;
}

internal sealed class HandleCommandJoinTeamHookParams : PlayerFunctionParams, IHandleCommandJoinTeamHookParams
{
    public HandleCommandJoinTeamHookParams(bool postHook,
        nint                                    client,
        nint                                    controller,
        int                                     team,
        JoinTeamFlags                           flags,
        int                                     coachTeam) :
        base(postHook, client, controller)
    {
        Team      = team;
        Flags     = flags;
        CoachTeam = coachTeam;
    }

    public int           Team      { get; private set; }
    public JoinTeamFlags Flags     { get; private set; }
    public int           CoachTeam { get; private set; }
    public bool          Queue     => (Flags & JoinTeamFlags.Queue) != 0;

    public void OverrideTeam(int team)
    {
        CheckDisposed();
        CheckEditable();
        Team = team;
    }

    public void OverrideFlags(JoinTeamFlags flags)
    {
        CheckDisposed();
        CheckEditable();
        Flags = flags;
    }

    public void OverrideQueue(bool queue)
    {
        CheckDisposed();
        CheckEditable();
        Flags = queue ? Flags | JoinTeamFlags.Queue : Flags & ~JoinTeamFlags.Queue;
    }

    public void OverrideCoachTeam(int coachTeam)
    {
        CheckDisposed();
        CheckEditable();
        CoachTeam = coachTeam;
    }
}
