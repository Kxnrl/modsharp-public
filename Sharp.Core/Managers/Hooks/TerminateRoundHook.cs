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
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Runtime;

namespace Sharp.Core.Managers.Hooks;

internal class TerminateRoundHook : HookType<TerminateRoundHookParams, EmptyHookReturn, TerminateRoundHook>
{
    public TerminateRoundHook(ILoggerFactory factory) : base(factory)
    {
        Bridges.Forwards.Game.OnTerminateRoundPre  += CCSGameRules_TerminateRoundPre;
        Bridges.Forwards.Game.OnTerminateRoundPost += CCSGameRules_TerminateRoundPost;
    }

    private HookReturnValue<EmptyHookReturn> CCSGameRules_TerminateRoundPre(nint gameRules,
        ref float                                                                delay,
        ref RoundEndReason                                                       reason,
        ref NativeFixedSpan<TeamRewardInfo>                                      teamRewards)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<EmptyHookReturn>(EHookAction.Ignored);
        }

        var param  = new TerminateRoundHookParams(false, gameRules, delay, reason, teamRewards);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        delay             = param.Delay;
        reason            = param.Reason;
        teamRewards.Count = param.RewardCount;

        return result;
    }

    private void CCSGameRules_TerminateRoundPost(nint gameRules,
        float                                         delay,
        RoundEndReason                                reason,
        NativeFixedSpan<TeamRewardInfo>               rewardInfo,
        EHookAction                                   action)
    {
        if (!IsHookInvokeRequired(true))
        {
            return;
        }

        var param = new TerminateRoundHookParams(true, gameRules, delay, reason, rewardInfo);

        InvokeHookPost(param, new HookReturnValue<EmptyHookReturn>(action));

        param.MarkAsDisposed();
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => true;

    protected override HookActionFlags SupportedActions
        => HookActionFlags.Ignored | HookActionFlags.SkipCallReturnOverride | HookActionFlags.ChangeParamReturnDefault;
}

internal sealed class TerminateRoundHookParams : FunctionParams, ITerminateRoundHookParams
{
    private NativeFixedSpan<TeamRewardInfo> _rewards;

    public TerminateRoundHookParams(bool postHook,
        nint                             gameRules,
        float                            delay,
        RoundEndReason                   reason,
        NativeFixedSpan<TeamRewardInfo>  info) : base(postHook)
    {
        GameRules = Objects.GameRules.Create(gameRules)!;
        Delay     = delay;
        Reason    = reason;
        _rewards  = info;
    }

    public IGameRules     GameRules { get; }
    public float          Delay     { get; private set; }
    public RoundEndReason Reason    { get; private set; }

    public ReadOnlySpan<TeamRewardInfo> TeamRewardInfo
    {
        get
        {
            CheckDisposed();

            return _rewards.AsReadOnlySpan();
        }
    }

    internal int RewardCount => _rewards.Count;

    public void OverrideDelay(float delay)
    {
        CheckDisposed();
        CheckEditable();

        Delay = delay;
    }

    public void OverrideReason(RoundEndReason reason)
    {
        CheckDisposed();
        CheckEditable();

        Reason = reason;
    }

    public void OverrideTeamRewards(TeamRewardInfo[] teamRewards)
    {
        CheckDisposed();
        CheckEditable();

        ref var rewards = ref _rewards;

        rewards.Count = 0;

        for (var i = 0; i < teamRewards.Length && i < rewards.Length; i++)
        {
            rewards[i] = teamRewards[i];
            rewards.Count++;
        }
    }
}
