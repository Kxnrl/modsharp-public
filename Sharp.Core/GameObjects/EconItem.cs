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
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.GameObjects;

namespace Sharp.Core.GameObjects;

internal partial class AttributeContainer : SchemaObject, IAttributeContainer
{
    private EconItemView? _item;

    public IEconItemView Item
    {
        get
        {
            CheckDisposed();

            return _item ??= (EconItemView) GetItem();
        }
    }

    protected override void OnDisposed()
    {
        _item?.MarkAsDisposed();
        _item = null;

        base.OnDisposed();
    }

#region Schemas

    [NativeSchemaField("CAttributeContainer", "m_Item", typeof(EconItemView), InlineObject = true)]
    private partial SchemaField GetItemField();

#endregion

    public override string GetSchemaClassname()
        => "CAttributeContainer";
}

internal partial class EconItemView : SchemaObject, IEconItemView
{
#region Schemas

    [NativeSchemaField("CEconItemView", "m_iItemDefinitionIndex", typeof(ushort), IsStruct = true)]
    private partial SchemaField GetItemDefinitionIndexField();

    [NativeSchemaField("CEconItemView", "m_iEntityQuality", typeof(int), IsStruct = true)]
    private partial SchemaField GetQualityField();

    [NativeSchemaField("CEconItemView", "m_iEntityLevel", typeof(uint), IsStruct = true)]
    private partial SchemaField GetLevelField();

    [NativeSchemaField("CEconItemView", "m_iItemID", typeof(ulong), IsStruct = true)]
    private partial SchemaField GetItemIdField();

    [NativeSchemaField("CEconItemView", "m_iItemIDHigh", typeof(uint), IsStruct = true)]
    private partial SchemaField GetItemIdHighField();

    [NativeSchemaField("CEconItemView", "m_iItemIDLow", typeof(uint), IsStruct = true)]
    private partial SchemaField GetItemIdLowField();

    [NativeSchemaField("CEconItemView", "m_iAccountID", typeof(uint), IsStruct = true)]
    private partial SchemaField GetAccountIdField();

    [NativeSchemaField("CEconItemView", "m_szCustomName", typeof(char[]), IsStruct = true)]
    private partial SchemaField GetCustomNameField();

    [NativeSchemaField("CEconItemView", "m_szCustomNameOverride", typeof(char[]), IsStruct = true)]
    private partial SchemaField GetCustomNameOverrideField();

    [NativeSchemaField("CEconItemView", "m_bInitialized", typeof(bool), IsStruct = true)]
    private partial SchemaField GetInitializedField();

#endregion

    public override string GetSchemaClassname()
        => "CEconItemView";
}
