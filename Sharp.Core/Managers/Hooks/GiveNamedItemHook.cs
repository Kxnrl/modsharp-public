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
using Sharp.Core.GameEntities;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Managers.Hooks;

internal class GiveNamedItemHook : HookType<GiveNamedItemHookParams, IBaseWeapon, GiveNamedItemHook>
{
    public GiveNamedItemHook(ILoggerFactory factory) : base(factory)
    {
        Bridges.Forwards.Extern.GiveNamedItem.OnGiveNamedItemPre  += OnGiveNamedItemPre;
        Bridges.Forwards.Extern.GiveNamedItem.OnGiveNamedItemPost += OnGiveNamedItemPost;
    }

    private HookReturnValue<IBaseWeapon> OnGiveNamedItemPre(nint ptrClient,
        nint                                                     ptrController,
        nint                                                     ptrPawn,
        nint                                                     ptrClassname,
        ref string?                                              classnameOverride,
        ref bool                                                 ignoredCEconItemView,
        ref nint                                                 econItemView)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<IBaseWeapon>(EHookAction.Ignored);
        }

        var classname = NativeString.ReadString(ptrClassname);

        var param = new GiveNamedItemHookParams(false,
                                                ptrClient,
                                                ptrController,
                                                ptrPawn,
                                                classname,
                                                ignoredCEconItemView,
                                                econItemView);

        var result = InvokeHookPre(param);

        param.MarkAsDisposed();

        // restore
        if (param.Classname != classname)
        {
            classnameOverride = param.Classname;
        }

        ignoredCEconItemView = param.IgnoreCEconItemView;
        econItemView         = param.EconItemViewPointer;

        // 因为这个Hook是没有返回值的, 所以不关心返回值
        return result;
    }

    private void OnGiveNamedItemPost(nint ptrClient,
        nint                              ptrController,
        nint                              ptrPawn,
        nint                              ptrClassname,
        bool                              ignoredCEconItemView,
        EHookAction                       hookAction,
        nint                              returnValue,
        nint                              econItemView)
    {
        if (!IsHookInvokeRequired(true))
        {
            return;
        }

        var param = new GiveNamedItemHookParams(true,
                                                ptrClient,
                                                ptrController,
                                                ptrPawn,
                                                NativeString.ReadString(ptrClassname),
                                                ignoredCEconItemView,
                                                econItemView);

        InvokeHookPost(param, new HookReturnValue<IBaseWeapon>(hookAction, BaseWeapon.Create(returnValue)));
        param.MarkAsDisposed();
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => true;

    protected override HookActionFlags SupportedActions => HookActionFlags.Ignored | HookActionFlags.ChangeParamReturnDefault;
}

internal class GiveNamedItemHookParams : PlayerPawnFunctionParams, IGiveNamedItemHookParams
{
    private IEconItemView? _econItemView;

    public GiveNamedItemHookParams(bool postHook,
        nint                            client,
        nint                            controller,
        nint                            pawn,
        string                          classname,
        bool                            ignore,
        nint                            econItemView) :
        base(postHook, client, controller, pawn)
    {
        Classname           = classname;
        IgnoreCEconItemView = ignore;
        EconItemViewPointer = econItemView;
    }

    public string Classname           { get; private set; }
    public bool   IgnoreCEconItemView { get; private set; }
    public nint   EconItemViewPointer { get; private set; }

    public IEconItemView? EconItemView
    {
        get
        {
            CheckDisposed();

            if (_econItemView is { } view && view.GetAbsPtr() == EconItemViewPointer)
            {
                return view;
            }

            return _econItemView = GameObjects.EconItemView.Create(EconItemViewPointer);
        }
    }

    public void SetOverride(string classname, bool ignoreCEconItemView = false)
    {
        CheckDisposed();
        CheckEditable();

        Classname           = classname;
        IgnoreCEconItemView = ignoreCEconItemView;
    }

    public void SetEconItemView(nint econItemView)
    {
        CheckDisposed();
        CheckEditable();

        EconItemViewPointer = econItemView;
    }

    public void SetEconItemView(IEconItemView? econItemView)
    {
        CheckDisposed();
        CheckEditable();

        EconItemViewPointer = econItemView?.GetAbsPtr() ?? 0;
        _econItemView       = econItemView;
    }
}
