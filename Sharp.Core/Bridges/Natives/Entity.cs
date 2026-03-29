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
using Sharp.Core.Types;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Core.Bridges.Natives;

public static unsafe partial class Entity
{
    public static partial bool IsValid(uint eHandle);

    public static partial void Kill(IntPtr entity);

    public static partial void ChangeTeam(IntPtr entity, CStrikeTeam team);

    public static partial void NetworkStateChanged(IntPtr entity, ushort offset);

    public static partial void SetStateChanged(IntPtr entity, ushort offset);

    public static partial void SetStructStateChanged(IntPtr entity, ushort offset);

    public static partial void CollisionRulesChanged(IntPtr entity);

    public static partial long EmitSound(IntPtr entity, string sound, float?* volume, RecipientFilter* filter);

    public static partial void StopSound(IntPtr entity, string sound);

    public static partial IntPtr FindByEHandle(uint eHandle);

    public static partial IntPtr FindByIndex(int index);

    public static partial IntPtr FindByClassname(IntPtr start, string classname);

    public static partial IntPtr FindByName(IntPtr start, string name);

    public static partial IntPtr FindInSphere(IntPtr start, Vector* center, float radius);

    public static partial IntPtr SpawnEntitySync(string classname,
        EntityKeyValuesVariant*                         lumps,
        int                                             count);

    public static partial IntPtr CreateByName(string classname);

    public static partial IntPtr AllocPooledString(string content);

    public static partial void HookOutput(string classname, string output);

    public static partial void HookInput(string classname, string input);

    public static partial bool AcceptInput(IntPtr entity,
        string                                    input,
        IntPtr                                    activator,
        IntPtr                                    caller,
        string?                                   value,
        int                                       outputId);

    public static partial bool AcceptInputInt(IntPtr entity,
        string                                       input,
        IntPtr                                       activator,
        IntPtr                                       caller,
        int                                          value,
        int                                          outputId);

    public static partial bool AcceptInputFloat(IntPtr entity,
        string                                         input,
        IntPtr                                         activator,
        IntPtr                                         caller,
        float                                          value,
        int                                            outputId);

    public static partial void AddIOEvent(IntPtr entity,
        float                                    delay,
        string                                   input,
        IntPtr                                   activator,
        IntPtr                                   caller,
        string?                                  value,
        int                                      outputId);

    public static partial void AddIOEventInt(IntPtr entity,
        float                                       delay,
        string                                      input,
        IntPtr                                      activator,
        IntPtr                                      caller,
        int                                         value,
        int                                         outputId);

    public static partial void AddIOEventFloat(IntPtr entity,
        float                                         delay,
        string                                        input,
        IntPtr                                        activator,
        IntPtr                                        caller,
        float                                         value,
        int                                           outputId);

    public static partial long DispatchTraceAttack(IntPtr entity, TakeDamageInfo* info, bool bypassHook);

    public static partial void SetName(IntPtr entity, string name);

    public static partial void SetModel(IntPtr entity, string model);

    public static partial void SetOwner(IntPtr entity, IntPtr owner);

    public static partial void SetGroundEntity(IntPtr entity, IntPtr ground, IntPtr unknown);

    public static partial void SetSolid(IntPtr entity, SolidType solid);

    public static partial void Teleport(IntPtr entity, Vector?* vPosition, Vector?* vAngles, Vector?* vVelocity);

    public static partial void SetMoveType(IntPtr entity, MoveType moveType);

    public static partial void SetGravityScale(IntPtr entity, float scale);

    public static partial void ApplyAbsVelocityImpulse(IntPtr entity, Vector* vVelocity);

    public static partial Vector* GetCenter(IntPtr entity);

    public static partial Vector* GetEyeAngles(IntPtr entity);

    public static partial Vector* GetEyePosition(IntPtr entity);

    public static partial Vector* GetAbsAngles(IntPtr entity);

    public static partial void SetAbsAngles(IntPtr entity, Vector* vAngles);

    public static partial Vector* GetAbsOrigin(IntPtr entity);

    public static partial void SetAbsOrigin(IntPtr entity, Vector* vOrigin);

    public static partial Vector* GetLocalVelocity(IntPtr entity);

    public static partial void SetLocalVelocity(IntPtr entity, Vector* vVelocity);

    public static partial Vector* GetAbsVelocity(IntPtr entity);

    public static partial void SetAbsVelocity(IntPtr entity, Vector* vVelocity);

    public static partial void SetBodyGroupByName(IntPtr entity, string name, int value);

    public static partial void SetMaterialGroupMask(IntPtr entity, ulong mask);

    public static partial int LookupAttachment(IntPtr entity, string attachment);

    public static partial void GetAttachment(IntPtr entity, int attachment, out Vector vOrigin, out Vector vAngles);

    public static partial int LookupBone(IntPtr entity, string bone);

    public static partial void GetBoneTransform(IntPtr entity, int bone, out Matrix3x4 matrix);

    public static partial void SetModelScale(IntPtr entity, float scale);

    public static partial void SetCollisionBounds(IntPtr entity, Vector* mins, Vector* maxs);

    public static partial void WeaponHolster(IntPtr weapon);

    public static partial void WeaponDeploy(IntPtr weapon);

    public static partial nint WeaponFromEntity(IntPtr entity);

    public static partial bool IsWeapon(IntPtr entity);

    public static partial bool IsPlayerController(IntPtr entity);

    public static partial bool IsPlayerPawn(IntPtr entity);

    public static partial nint GetGlobalCStrikeTeam(CStrikeTeam team);

    public static partial void DispatchSpawn(IntPtr entity, EntityKeyValuesVariant* lumps, int count);
}
