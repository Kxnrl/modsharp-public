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
using Sharp.Core.Bridges.Natives;
using Sharp.Core.GameObjects;
using Sharp.Core.Helpers;
using Sharp.Core.Utilities;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;

namespace Sharp.Core.GameEntities;

internal partial class BaseWeapon : EconEntity, IBaseWeapon
{
    private static readonly Lazy<int> ContainerItemOffset
        = new (() => SchemaSystem.GetNetVarOffset("CAttributeContainer", "m_Item"));

    private static readonly Lazy<int> EconItemViewItemDefinitionIndexOffset
        = new (() => SchemaSystem.GetNetVarOffset("CEconItemView", "m_iItemDefinitionIndex"));

    private static readonly Lazy<int> VDataSlotOffset
        = new (() => SchemaSystem.GetNetVarOffset("CCSWeaponBaseVData", "m_GearSlot"));

    private static readonly Lazy<int> VDataMaxClipOffset
        = new (() => SchemaSystem.GetNetVarOffset("CBasePlayerWeaponVData", "m_iMaxClip1"));

    private static readonly Lazy<int> VDataPrimaryReserveAmmoMaxOffset
        = new (() => SchemaSystem.GetNetVarOffset("CCSWeaponBaseVData", "m_nPrimaryReserveAmmoMax"));

    private static readonly Lazy<int> VDataOffset
        = new (() => SchemaSystem.GetNetVarOffset("CBaseEntity", "m_pSubclassVData"));

    public bool IsKnife => Entity.IsWeapon(_this) && ItemDefinitionIndex is >= 500 and <= 526 or 42 or 59;

    public unsafe ushort ItemDefinitionIndex
    {
        get
        {
            var netClass = GetSchemaClassname();

            if (!SchemaSystem.FindNetVar(netClass, "m_AttributeManager"))
            {
                return ushort.MaxValue;
            }

            var offset           = SchemaSystem.GetNetVarOffset(netClass, "m_AttributeManager");
            var attributeManager = new IntPtr((byte*) _this.ToPointer()            + offset);
            var item             = new IntPtr((byte*) attributeManager.ToPointer() + ContainerItemOffset.Value);

            return item.GetUInt16(EconItemViewItemDefinitionIndexOffset.Value);
        }
    }

    public IPlayerPawn? PrevOwnerEntity => PlayerPawn.Create(PrevOwnerEntityHandle.GetEntityPtr());

    public void Holster()
        => Entity.WeaponHolster(_this);

    public void Deploy()
        => Entity.WeaponDeploy(_this);

#region Schemas

    [NativeSchemaField("CBasePlayerWeapon", "m_nNextPrimaryAttackTick", typeof(int))]
    private partial SchemaField GetNextPrimaryAttackTickField();

    [NativeSchemaField("CBasePlayerWeapon", "m_nNextSecondaryAttackTick", typeof(int))]
    private partial SchemaField GetNextSecondaryAttackTickField();

    [NativeSchemaField("CBasePlayerWeapon", "m_iClip1", typeof(int))]
    private partial SchemaField GetClipField();

    [NativeSchemaField("CBasePlayerWeapon", "m_pReserveAmmo", typeof(int))]
    private partial SchemaField GetReserveAmmoField();

    [NativeSchemaField("CCSWeaponBase", "m_hPrevOwner", typeof(CEntityHandle<IPlayerPawn>))]
    private partial SchemaField GetPrevOwnerEntityHandleField();

    [NativeSchemaField("CCSWeaponBase", "m_iWeaponGameplayAnimState", typeof(WeaponGameplayAnimState))]
    private partial SchemaField GetWeaponGameplayAnimStateField();

    [NativeSchemaField("CCSWeaponBase", "m_flWeaponGameplayAnimStateTimestamp", typeof(float))]
    private partial SchemaField GetWeaponGameplayAnimStateTimestampField();

    [NativeSchemaField("CCSWeaponBase", "m_weaponMode", typeof(CStrikeWeaponMode))]
    private partial SchemaField GetWeaponModeField();

    [NativeSchemaField("CCSWeaponBase", "m_nextOwnerTouchTime", typeof(float))]
    private partial SchemaField GetNextOwnerTouchTimeField();

    [NativeSchemaField("CCSWeaponBase", "m_nextPrevOwnerTouchTime", typeof(float))]
    private partial SchemaField GetNextPrevOwnerTouchTimeField();

    [NativeSchemaField("CCSWeaponBase", "m_bCanBePickedUp", typeof(bool))]
    private partial SchemaField GetCanBePickedUpField();

    [NativeSchemaField("CCSWeaponBase", "m_fAccuracyPenalty", typeof(float))]
    private partial SchemaField GetAccuracyPenaltyField();

    [NativeSchemaField("CCSWeaponBase", "m_flLastAccuracyUpdateTime", typeof(float))]
    private partial SchemaField GetLastAccuracyUpdateTimeField();

    [NativeSchemaField("CCSWeaponBase", "m_flRecoilIndex", typeof(float))]
    private partial SchemaField GetRecoilIndexField();

    [NativeSchemaField("CCSWeaponBase", "m_bInReload", typeof(bool))]
    private partial SchemaField GetInReloadField();

    [NativeSchemaField("CCSWeaponBase", "m_bSilencerOn", typeof(bool))]
    private partial SchemaField GetSilencerOnField();

    [NativeSchemaField("CCSWeaponBase", "m_flTimeSilencerSwitchComplete", typeof(float))]
    private partial SchemaField GetTimeSilencerSwitchCompleteField();

    [NativeSchemaField("CCSWeaponBase", "m_fLastShotTime", typeof(float))]
    private partial SchemaField GetLastShotTimeField();

#endregion

    public IWeaponData GetWeaponData()
        => WeaponData.Create(_this.GetObjectPtr(VDataOffset.Value))
           ?? throw new EntryPointNotFoundException("Invalid VData pointer");

    public int MaxClip => _this.GetObjectVarInt32(VDataOffset.Value, VDataMaxClipOffset.Value);

    public int PrimaryReserveAmmoMax => _this.GetObjectVarInt32(VDataOffset.Value, VDataPrimaryReserveAmmoMaxOffset.Value);

    public GearSlot Slot => (GearSlot) _this.GetObjectVarInt32(VDataOffset.Value, VDataSlotOffset.Value);

    ////////////////////////////////////////////////////////////////////////////

    public string GetWeaponClassname()
    {
        var classname = Classname;

        if (classname.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
        {
            return classname;
        }

        if (!SharedGameObject.EconItemDefinitionsById.TryGetValue(ItemDefinitionIndex, out var value))
        {
            throw new InvalidOperationException($"Entity {classname} is not weapon!");
        }

        var definition = value.DefinitionName;

        if (definition[0] == 'w')
        {
            return definition;
        }

        throw new InvalidOperationException($"Entity {classname} is not weapon!");
    }

    public string GetItemDefinitionName()
        => GetItemDefinition().DefinitionName;

    public IEconItemDefinition GetItemDefinition()
    {
        var classname = Classname;

        if (classname.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
        {
            if (SharedGameObject.EconItemDefinitionsByName.TryGetValue(classname, out var itemDefinition))
            {
                return itemDefinition;
            }

            throw new InvalidOperationException($"Item {classname} is not EconItem");
        }

        if (!SharedGameObject.EconItemDefinitionsById.TryGetValue(ItemDefinitionIndex, out var weaponDefinition))
        {
            throw new InvalidOperationException($"Entity {classname} is not weapon!");
        }

        return weaponDefinition;
    }
}
