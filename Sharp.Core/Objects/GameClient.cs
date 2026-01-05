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
using System.Runtime.InteropServices;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Core.GameEntities;
using Sharp.Core.Helpers;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Objects;

internal partial class GameClient : NativeObject, IGameClient
{
    private KeyValues? _conVars;

    private GameClient(nint ptr) : base(ptr)
    {
        _defaultUserId  = ptr.GetUInt16(CoreGameData.GameClient.UserId);
        _defaultSlot    = ptr.GetByte(CoreGameData.GameClient.Slot);
        _defaultSteamId = ptr.GetUInt64(CoreGameData.GameClient.SteamId);
    }

    protected override void OnDisposed()
    {
        base.OnDisposed();

        _conVars?.MarkAsDisposed();
    }

    /// <summary>
    ///     输出到客户端控制台
    /// </summary>
    public void ConsolePrint(string message)
    {
        CheckDisposed();
        Client.ConsolePrint(this, message);
    }

    /// <summary>
    ///     覆盖游戏内名字
    /// </summary>
    public void SetName(string name)
    {
        CheckDisposed();
        Client.SetName(this, name);
    }

    /// <summary>
    ///     发生聊天消息
    /// </summary>
    public void SayChatMessage(bool teamOnly, string message)
    {
        CheckDisposed();
        Client.SayChatMessage(this, teamOnly, message);
    }

    /// <summary>
    ///     读取客户端IP地址
    /// </summary>
    /// <returns></returns>
    public string? GetAddress(bool withPort)
    {
        CheckDisposed();

        if (IsFakeClient)
        {
            return null;
        }

        var address = Client.GetNetWorkAddress(this);

        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        if (!withPort)
        {
            var index = address.IndexOf(':');

            if (index > -1)
            {
                return address.Remove(index);
            }
        }

        return address;
    }

    /// <summary>
    ///     获取客户端建立链接的时间
    /// </summary>
    public float GetTimeConnected()
    {
        CheckDisposed();

        return IsFakeClient ? (float) Bridges.Natives.Core.GetEngineTime() : Client.GetTimeConnected(this);
    }

    /// <summary>
    ///     执行命令 (via Client/NetChannel)
    /// </summary>
    public void Command(string command)
    {
        CheckDisposed();

        Client.ClientCommand(this, command);
    }

    public void FakeCommand(string command)
    {
        CheckDisposed();
        Client.FakeClientCommand(this, command);
    }

    public void ExecuteStringCommand(string command)
    {
        CheckDisposed();
        Client.ExecuteStringCommand(this, command);
    }

    public void ForceFullUpdate()
    {
        CheckDisposed();
        _this.WriteInt32(CoreGameData.GameClient.DeltaTick, -1);
    }

    public string? GetConVarValue(string cvarName)
    {
        CheckDisposed();

        var ptr = Client.GetConVarValue(this, cvarName);

        return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }

    public IKeyValues? GetConVars()
    {
        CheckDisposed();

        return _conVars ??= KeyValues.Create(GetConVars(_this));
    }

    public IPlayerController? GetPlayerController()
    {
        CheckDisposed();

        return !IsInGame
               || PlayerController.Create(Player.ControllerFindBySlot(Slot)) is not
               {
                   ConnectedState: PlayerConnectedState.PlayerConnected,
               } controller
            ? null
            : controller;
    }

    // identity
    private readonly ushort _defaultUserId;
    private readonly byte   _defaultSlot;
    private readonly ulong  _defaultSteamId;

    public SignOnState SignOnState     => (SignOnState) _this.GetInt32(CoreGameData.GameClient.SignonState);
    public bool        IsFakeClient    => _this.GetBool(CoreGameData.GameClient.FakeClient);
    public UserID      UserId          => GetUserId(_this);
    public SteamID     SteamId         => IsFakeClient ? 0 : GetSteamId(_this);
    public PlayerSlot  Slot            => GetSlot(_this);
    public EntityIndex ControllerIndex => new (Slot);
    public string      Name            => _this.GetPtrString(CoreGameData.GameClient.GetName);
    public bool        PerfectWorld    => _this.GetBool(CoreGameData.GameClient.PerfectWorld);
    public bool        IsAuthenticated => _this.GetBool(CoreGameData.GameClient.FullyAuthenticated);

    public bool IsValid
        => !IsDisposed
           && _this          == Client.GetClientBySlot(_defaultSlot) // match slot
           && _defaultUserId == GetUserId(_this)                     // default user not changed
           && _defaultSlot   == GetSlot(_this)                       // default slot not changed
           && SignOnState    >= SignOnState.Connected;

    public bool IsConnected => IsValid && SignOnState < SignOnState.ChangeLevel;

    public bool IsInGame => IsValid && SignOnState is SignOnState.Full;

    public bool IsHltv => _this.GetBool(CoreGameData.GameClient.IsHltv);

    public float   TimeConnected => GetTimeConnected();
    public string? Address       => GetAddress(false);

    public override int GetHashCode()
        => HashCode.Combine(_this, _defaultSlot);

    public override bool Equals(object? obj)
    {
        if (obj is not GameClient gc)
        {
            return false;
        }

        if (_this != gc._this)
        {
            return false;
        }

        return UserId == gc.UserId && SteamId == gc.SteamId;
    }

    internal bool CheckMatchClient(nint pointer)
        => !IsDisposed
           && _this           == pointer
           && _defaultSlot    == GetSlot(pointer)
           && _defaultUserId  == GetUserId(pointer)
           && _defaultSteamId == GetSteamId(pointer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort GetUserId(nint pointer)
        => pointer.GetUInt16(CoreGameData.GameClient.UserId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetSlot(nint pointer)
        => pointer.GetByte(CoreGameData.GameClient.Slot);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetSteamId(nint pointer)
        => pointer.GetUInt64(CoreGameData.GameClient.SteamId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nint GetConVars(nint pointer)
        => pointer.GetObjectPtr(CoreGameData.GameClient.ConVars);
}
