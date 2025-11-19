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

using System;
using System.Runtime.CompilerServices;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Core.GameEntities;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Objects;

internal partial class GameEvent : NativeObject, IGameEvent
{
    protected GameEvent(IntPtr ptr) : base(ptr)
        => Editable = false;

    protected GameEvent(IntPtr ptr, bool editable) : base(ptr)
        => Editable = editable;

    protected GameEvent(IntPtr ptr, string name, bool editable) : base(ptr)
    {
        _name    = name;
        Editable = editable;
    }

    public new static GameEvent? Create(IntPtr ptr)
        => ptr == IntPtr.Zero ? null : new GameEvent(ptr);

    public static GameEvent? Create(IntPtr ptr, string name, bool editable)
        => ptr == IntPtr.Zero ? null : new GameEvent(ptr, name, editable);

    public static GameEvent? CreateEditable(IntPtr ptr)
        => ptr == IntPtr.Zero ? null : new GameEvent(ptr, true);

    public static GameEvent? CreateEditable(IntPtr ptr, string name)
        => ptr == IntPtr.Zero ? null : new GameEvent(ptr, name, true);

    private string? _name;

    public void SetString(string key, string value)
    {
        CheckEditable();
        Event.SetString(_this, key, value);
    }

    public void SetFloat(string key, float value)
    {
        CheckEditable();
        Event.SetFloat(_this, key, value);
    }

    public void SetInt(string key, int value)
    {
        CheckEditable();
        Event.SetInt(_this, key, value);
    }

    public void SetUInt64(string key, ulong value)
    {
        CheckEditable();
        Event.SetUInt64(_this, key, value);
    }

    public void SetPlayer(string key, IPlayerController controller)
    {
        CheckEditable();
        Event.SetPlayerPawn(_this, key, controller);
    }

    public void SetPlayer(string key, IPlayerPawn pawn)
    {
        CheckEditable();
        Event.SetPlayerPawn(_this, key, pawn);
    }

    public void SetPlayer(string key, int slot)
    {
        CheckEditable();
        Event.SetPlayerSlot(_this, key, slot);
    }

    public void SetBool(string key, bool value)
        => SetInt(key, value ? 1 : 0);

    public bool GetBool(string key)
        => GetInt(key) != 0;

    public unsafe string GetString(string key, string defaultValue = "")
    {
        CheckDisposed();

        return Utils.ReadString(Event.GetString(_this, key, defaultValue));
    }

    public float GetFloat(string key, float defaultValue = 0.0f)
    {
        CheckDisposed();

        return Event.GetFloat(_this, key, defaultValue);
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        CheckDisposed();

        return Event.GetInt(_this, key, defaultValue);
    }

    public ulong GetUInt64(string key, ulong defaultValue = 0)
    {
        CheckDisposed();

        return Event.GetUInt64(_this, key, defaultValue);
    }

    public IPlayerController? GetPlayerController(string key)
    {
        CheckDisposed();

        return PlayerController.Create(Event.GetPlayerController(_this, key));
    }

    public IPlayerPawn? GetPlayerPawn(string key)
        => GetBasePlayerPawn(key)?.AsPlayer();

    public IBasePlayerPawn? GetBasePlayerPawn(string key)
    {
        CheckDisposed();

        return BasePlayerPawn.Create(Event.GetPlayerPawn(_this, key));
    }

    public string GetName()
    {
        if (_name is not null)
        {
            return _name;
        }

        CheckDisposed();

        _name = GetEventName(_this);

        return _name;
    }

    public void Fire(bool serverOnly)
    {
        CheckEditable();
        Event.FireEvent(_this, serverOnly);
    }

    public void FireToClient(int slot)
    {
        CheckEditable();
        Event.FireToClient(_this, slot);
    }

    public void FireToClient(IGameClient client)
    {
        CheckEditable();
        Event.FireToClient(_this, client.Slot);
    }

    public void FireToClients()
    {
        CheckEditable();
        Event.FireToClients(_this);
    }

    public void Dispose()
    {
        CheckEditable();
        Event.FreeEvent(_this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CheckEditable()
    {
        CheckDisposed();

        if (!Editable)
        {
            throw new InvalidOperationException("Event is ReadOnly!");
        }
    }

    public string Name     => GetName();
    public bool   Editable { get; protected init; }

    public object this[string key]
    {
        set
        {
            if (value is int intValue)
            {
                SetInt(key, intValue);
            }
            else if (value is float floatValue)
            {
                SetFloat(key, floatValue);
            }
            else if (value is bool boolValue)
            {
                SetBool(key, boolValue);
            }
            else if (value is string stringValue)
            {
                SetString(key, stringValue);
            }
            else
            {
                throw new NotSupportedException("Unsupported value type");
            }
        }
    }

    public T Get<T>(string key, int defaultValue = 0) where T : Enum
    {
        var value = GetInt(key, defaultValue);

#if false
        Unsafe.As<int, T>(ref value);
#endif

        return EnumConverter<T>.Convert(value);
    }

    internal static unsafe string GetEventName(IntPtr ptr)
        => Utils.ReadString(Event.GetName(ptr));
}
