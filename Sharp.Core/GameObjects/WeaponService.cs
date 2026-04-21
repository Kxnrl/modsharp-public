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

using Sharp.Core.CStrike;
using Sharp.Core.GameEntities;
using Sharp.Core.Utilities;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Core.GameObjects;

internal partial class WeaponService : PlayerPawnComponent, IWeaponService
{
    public IBaseWeapon? ActiveWeapon => BaseWeapon.Create(ActiveWeaponHandle.GetEntityPtr());
    public IBaseWeapon? LastWeapon   => BaseWeapon.Create(LastWeaponHandle.GetEntityPtr());

#region Schemas

    [NativeSchemaField("CCSPlayer_WeaponServices", "m_flNextAttack", typeof(float))]
    private partial SchemaField GetNextAttackField();

    [NativeSchemaField("CCSPlayer_WeaponServices", "m_bIsPickingUpItemWithUse", typeof(bool))]
    private partial SchemaField GetIsPickingUpItemWithUseField();

    [NativeSchemaField("CCSPlayer_WeaponServices", "m_bPickedUpWeapon", typeof(bool))]
    private partial SchemaField GetPickedUpWeaponField();

    [NativeSchemaField("CCSPlayer_WeaponServices", "m_bDisableAutoDeploy", typeof(bool))]
    private partial SchemaField GetDisableAutoDeployField();

    [NativeSchemaField("CCSPlayer_WeaponServices", "m_bIsPickingUpGroundWeapon", typeof(bool))]
    private partial SchemaField GetIsPickingUpGroundWeaponField();

    [NativeSchemaField("CPlayer_WeaponServices", "m_bPreventWeaponPickup", typeof(bool))]
    private partial SchemaField GetPreventWeaponPickupField();

    [NativeSchemaField("CPlayer_WeaponServices",
                       "m_hMyWeapons",
                       typeof(SchemaUnmanagedVector<CEntityHandle<IBaseWeapon>>),
                       InlineObject = true,
                       ReturnType = typeof(ISchemaList<CEntityHandle<IBaseWeapon>>))]
    private partial SchemaField GetMyWeaponsField();

    [NativeSchemaField("CPlayer_WeaponServices", "m_iAmmo", typeof(ushort[]))]
    private partial SchemaField GetAmmoField();

    [NativeSchemaField("CPlayer_WeaponServices", "m_hActiveWeapon", typeof(CEntityHandle<IBaseWeapon>))]
    private partial SchemaField GetActiveWeaponHandleField();

    [NativeSchemaField("CPlayer_WeaponServices", "m_hLastWeapon", typeof(CEntityHandle<IBaseWeapon>))]
    private partial SchemaField GetLastWeaponHandleField();

    [NativeSchemaField("CCSPlayer_WeaponServices", "m_hSavedWeapon", typeof(CEntityHandle<IBaseWeapon>))]
    private partial SchemaField GetSavedWeaponHandleField();

#endregion

    public override string GetSchemaClassname()
        => "CCSPlayer_WeaponServices";
}
