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
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Core.GameObjects;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;

namespace Sharp.Core.GameEntities;

internal partial class PlayerPawn : BasePlayerPawn, IPlayerPawn
{
    protected override bool IsObserver()
        => false;

    public override IPlayerPawn? AsPlayer()
        => this;

    public void Slay(bool explode = false)
        => Player.PawnSlay(_this, explode);

    public IBaseWeapon? GiveNamedItem(string weapon)
        => BaseWeapon.Create(Player.PawnGiveNamedItem(_this, weapon));

    public IBaseWeapon? GiveNamedItem(EconItemId id)
    {
        if (!Enum.IsDefined(id) || !SharedGameObject.EconItemDefinitionsById.TryGetValue((ushort) id, out var value))
        {
            throw new InvalidOperationException($"Invalid ItemId {id}");
        }

        return GiveNamedItem(value.DefinitionName);
    }

    public IPlayerController? GetControllerAuto()
        => IsAlive ? GetController() : GetOriginalController();

    public IBaseWeapon? GetActiveWeapon()
        => BaseWeapon.Create(Player.PawnGetActiveWeapon(_this));

    public IBaseWeapon? GetWeaponBySlot(GearSlot slot, int position = -1)
        => BaseWeapon.Create(Player.PawnGetWeaponBySlot(_this, slot, position));

    public void RemovePlayerItem(IBaseWeapon item)
        => Player.PawnRemovePlayerItem(_this, item.GetAbsPtr());

    public void RemoveAllItems(bool removeSuit = false)
        => Player.PawnRemoveAllItems(_this, removeSuit);

    public void DropWeapon(IBaseWeapon item)
        => Player.PawnDropWeapon(_this, item.GetAbsPtr());

    public bool SelectItem(IBaseWeapon item)
        => Player.PawnSelectItem(_this, item.GetAbsPtr());

    public bool DetachWeapon(IBaseWeapon item)
        => Player.PawnDetachWeapon(_this, item.GetAbsPtr());

    public void SwitchWeapon(IBaseWeapon? weapon)
        => Player.PawnSwitchWeapon(_this, weapon?.GetAbsPtr() ?? nint.Zero);

    public void GiveGloves(int itemDefIndex, int prefab, float wear, int seed)
        => Player.PawnGiveGloves(_this, itemDefIndex, prefab, wear, seed);

    public void GiveGloves(EconGlovesId id, int prefab, float wear, int seed)
    {
        if (!Enum.IsDefined(id))
        {
            Bridges.Natives.Core.LogWarning($"[PlayerPawn::GiveGloves] Invalid GlovesId {id}");

            return;
        }

        Player.PawnGiveGloves(_this, (int) id, prefab, wear, seed);
    }

    public void TransientChangeVelocityModifier(float velocityModifier)
        => SetVelocityModifierLocal(velocityModifier);

#region Schemas

    [NativeSchemaField("CCSPlayerPawn", "m_ArmorValue", typeof(int))]
    private partial SchemaField GetArmorValueField();

    [NativeSchemaField("CCSPlayerPawn", "m_fMolotovDamageTime", typeof(float))]
    private partial SchemaField GetMolotovDamageTimeField();

    [NativeSchemaField("CCSPlayerPawn", "m_flHealthShotBoostExpirationTime", typeof(float))]
    private partial SchemaField GetHealthShotBoostExpirationTimeField();

    [NativeSchemaField("CCSPlayerPawn", "m_EconGloves", typeof(EconItemView), InlineObject = true)]
    private partial SchemaField GetEconGlovesField();

    [NativeSchemaField("CCSPlayerPawn", "m_flVelocityModifier", typeof(float))]
    private partial SchemaField GetVelocityModifierField();

    [NativeSchemaField("CCSPlayerPawn", "m_iShotsFired", typeof(int))]
    private partial SchemaField GetShotsFiredField();

    [NativeSchemaField("CCSPlayerPawn", "m_flFlinchStack", typeof(float))]
    private partial SchemaField GetFlinchStackField();

    [NativeSchemaField("CCSPlayerPawn", "m_bInBuyZone", typeof(bool))]
    private partial SchemaField GetInBuyZoneField();

    [NativeSchemaField("CCSPlayerPawn", "m_bInHostageRescueZone", typeof(bool))]
    private partial SchemaField GetInHostageRescueZoneField();

    [NativeSchemaField("CCSPlayerPawn", "m_bInBombZone", typeof(bool))]
    private partial SchemaField GetInBombZoneField();

    [NativeSchemaField("CCSPlayerPawn", "m_bIsBuyMenuOpen", typeof(bool))]
    private partial SchemaField GetIsBuyMenuOpenField();

    [NativeSchemaField("CCSPlayerPawn", "m_bLeftHanded", typeof(bool))]
    private partial SchemaField GetLeftHandedField();

    [NativeSchemaField("CCSPlayerPawn", "m_fSwitchedHandednessTime", typeof(float))]
    private partial SchemaField GetSwitchedHandednessTimeField();

    [NativeSchemaField("CCSPlayerPawn", "m_vecTotalBulletForce", typeof(Vector))]
    private partial SchemaField GetTotalBulletForceField();

    [NativeSchemaField("CCSPlayerPawn", "m_iDeathFlags", typeof(int))]
    private partial SchemaField GetDeathFlagsField();

    [NativeSchemaField("CCSPlayerPawn", "m_bWaitForNoAttack", typeof(bool))]
    private partial SchemaField GetWaitForNoAttackField();

    [NativeSchemaField("CCSPlayerPawn", "m_aimPunchAngle", typeof(Vector))]
    private partial SchemaField GetAimPunchAngleField();

    [NativeSchemaField("CCSPlayerPawn", "m_aimPunchAngleVel", typeof(Vector))]
    private partial SchemaField GetAimPunchAngleVelocityField();

    [NativeSchemaField("CCSPlayerPawn", "m_aimPunchTickBase", typeof(int))]
    private partial SchemaField GetAimPunchTickBaseField();

    [NativeSchemaField("CCSPlayerPawn", "m_aimPunchTickFraction", typeof(float))]
    private partial SchemaField GetAimPunchTickFractionField();

    [NativeSchemaField("CCSPlayerPawn",
                       "m_aimPunchCache",
                       typeof(SchemaUnmanagedVector<Vector>),
                       InlineObject = true,
                       ReturnType = typeof(ISchemaList<Vector>))]
    private partial SchemaField GetAimPunchCacheField();

#endregion

#region Service Schema

    public unsafe IItemService? GetItemService()
        => ItemService.Create(*(nint*) IntPtr.Add(_this, GetItemServiceField().Offset));

    public unsafe IWeaponService? GetWeaponService()
        => WeaponService.Create(*(nint*) IntPtr.Add(_this, GetWeaponServiceField().Offset));

    public override unsafe IMovementService? GetMovementService()
        => PlayerMovementService.Create(*(nint*) IntPtr.Add(_this, GetMovementServiceField().Offset));

    public unsafe IPlayerMovementService? GetPlayerMovementService()
        => PlayerMovementService.Create(*(nint*) IntPtr.Add(_this, GetMovementServiceField().Offset));

#endregion
}
