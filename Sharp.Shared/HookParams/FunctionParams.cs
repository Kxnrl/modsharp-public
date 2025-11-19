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

using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Shared.HookParams;

public interface IFunctionParams : IContextObject;

public readonly struct EmptyHookReturn;

public interface IClientFunctionParams
{
    IGameClient Client { get; }
}

public interface IPlayerFunctionParams : IClientFunctionParams
{
    IPlayerController Controller { get; }
}

public interface IPlayerPawnFunctionParams : IPlayerFunctionParams
{
    IPlayerPawn Pawn { get; }
}

public interface IPlayerWeaponFunctionParams : IPlayerPawnFunctionParams
{
    IBaseWeapon Weapon { get; }
}

public interface IPlayerWeaponWithServiceFunctionParams : IPlayerWeaponFunctionParams
{
    IWeaponService Service { get; }
}

public interface IPlayerMovementFunctionParams : IPlayerPawnFunctionParams
{
    IPlayerMovementService Service { get; }
}

public unsafe interface IPlayerMovementWithDataFunctionParams : IPlayerMovementFunctionParams
{
    MoveData* Info { get; }

    Vector ViewAngles     { get; set; }
    Vector Velocity       { get; set; }
    Vector Angles         { get; set; }
    Vector AbsOrigin      { get; set; }
    float  MaxSpeed       { get; set; }
    float  ClientMaxSpeed { get; set; }
    Vector OutWishVel     { get; set; }
    bool   InAir          { get; set; }
}

public unsafe interface ITakeDamageInfoParams
{
    TakeDamageInfo* Info { get; }

    /* GameTrace */
    Vector       TraceEndPosition { get; }
    HitGroupType HitGroup         { get; }
    int          HitBoxId         { get; }
    GameTrace*   Trace            { get; }

    /* Bundle */
    Vector                     DamageForce          { get; set; }
    Vector                     DamagePosition       { get; set; }
    Vector                     ReportedPosition     { get; set; }
    Vector                     DamageDirection      { get; set; }
    CEntityHandle<IBaseEntity> InflictorHandle      { get; set; }
    CEntityHandle<IBaseEntity> AttackerHandle       { get; set; }
    CEntityHandle<IBaseEntity> AbilityHandle        { get; set; }
    float                      Damage               { get; set; }
    DamageFlagBits             DamageType           { get; set; }
    int                        DamageCustom         { get; set; }
    byte                       AmmoType             { get; set; }
    float                      OriginalDamage       { get; set; }
    bool                       ShouldBleed          { get; set; }
    bool                       ShouldSpark          { get; set; }
    TakeDamageFlags            TakeDamageFlags      { get; set; }
    int                        NumObjectsPenetrated { get; set; }
    bool                       InTakeDamageFlow     { get; set; }

    /* Attacker Info */
    bool                       IsPawn             { get; set; }
    bool                       IsWorld            { get; set; }
    int                        AttackerPlayerSlot { get; set; }
    CEntityHandle<IPlayerPawn> AttackerPawnHandle { get; set; }
    int                        TeamChecked        { get; set; }
    int                        Team               { get; set; }
}

public unsafe interface ITakeDamageResultParams
{
    TakeDamageResult* Result { get; }

    int   HealthLost          { get; set; }
    int   HealthBefore        { get; set; }
    int   DamageDealt         { get; set; }
    float PreModifiedDamage   { get; set; }
    int   TotalledHealthLost  { get; set; }
    int   TotalledDamageDealt { get; set; }
    bool  WasDamageSuppressed { get; set; }
}

public unsafe interface IFireBulletsInfoParams
{
    FireBulletInfo* Info { get; }

    Vector ShootPosition    { get; set; }
    Vector ShootAngles      { get; set; }
    float  Range            { get; set; }
    float  RangeModifier    { get; set; }
    float  Penetration      { get; set; }
    int    PenetrationCount { get; set; }
    int    Damage           { get; set; }
    int    CurrentBullet    { get; set; }
    float  SpreadX          { get; set; }
    float  SpreadY          { get; set; }
}
