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
using Sharp.Core.Attributes;
using Sharp.Core.CStrike;
using Sharp.Core.GameEntities;
using Sharp.Core.Helpers;
using Sharp.Core.VirtualCalls;
using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;

namespace Sharp.Core.GameObjects;

[EntityComponentInfo("CBodyComponent", "__m_pChainEntity")]
internal partial class BodyComponent : EntityComponent, IBodyComponent
{
    public IBodyComponentSkeletonInstance AsBodyComponentSkeletonInstance => BodyComponentSkeletonInstance.Create(_this)!;

#region Schemas

    [NativeSchemaField("CBodyComponent", "m_pSceneNode", typeof(GameSceneNode))]
    private partial SchemaField GetSceneNodeField();

#endregion

    public override string GetSchemaClassname()
        => "CBodyComponent";
}

[EntityComponentInfo("CBodyComponentSkeletonInstance", "__m_pChainEntity")]
internal partial class BodyComponentSkeletonInstance : BodyComponent, IBodyComponentSkeletonInstance
{
#region Schemas

    [NativeSchemaField("CBodyComponentSkeletonInstance", "m_skeletonInstance", typeof(SkeletonInstance), InlineObject = true)]
    private partial SchemaField GetSkeletonInstanceField();

#endregion

    public override string GetSchemaClassname()
        => "CBodyComponentSkeletonInstance";
}

internal partial class GameSceneNode : SchemaObject, IGameSceneNode
{
    public ISkeletonInstance? AsSkeletonInstance
        => SkeletonInstance.Create(GameSceneNodeVirtualCall.Instance.GetSkeletonInstance(this));

#region Schemas

    [NativeSchemaField("CGameSceneNode", "m_pParent", typeof(GameSceneNode))]
    private partial SchemaField GetParentField();

    [NativeSchemaField("CGameSceneNode", "m_pChild", typeof(GameSceneNode))]
    private partial SchemaField GetChildField();

    [NativeSchemaField("CGameSceneNode", "m_pNextSibling", typeof(GameSceneNode))]
    private partial SchemaField GetNextSiblingField();

    [NativeSchemaField("CGameSceneNode", "m_pOwner", typeof(BaseEntity))]
    private partial SchemaField GetOwnerField();

    [NativeSchemaField("CGameSceneNode", "m_vecAbsOrigin", typeof(Vector))]
    private partial SchemaField GetAbsOriginField();

    [NativeSchemaField("CGameSceneNode", "m_angAbsRotation", typeof(Vector))]
    private partial SchemaField GetAbsRotationField();

    [NativeSchemaField("CGameSceneNode", "m_flScale", typeof(float), IsStruct = true)]
    private partial SchemaField GetScaleField();

    [NativeSchemaField("CGameSceneNode", "m_flAbsScale", typeof(float), IsStruct = true)]
    private partial SchemaField GetAbsScaleField();

#endregion

    public override string GetSchemaClassname()
        => "CGameSceneNode";
}

internal partial class SkeletonInstance : GameSceneNode, ISkeletonInstance
{
#region Schemas

    [NativeSchemaField("CSkeletonInstance", "m_modelState", typeof(ModelState), InlineObject = true)]
    private partial SchemaField GetModelStateField();

    [NativeSchemaField("CSkeletonInstance", "m_bDisableSolidCollisionsForHierarchy", typeof(bool))]
    private partial SchemaField GetDisableSolidCollisionsForHierarchyField();

    [NativeSchemaField("CSkeletonInstance", "m_materialGroup", typeof(uint))]
    private partial SchemaField GetMaterialGroupField();

#endregion

    public override string GetSchemaClassname()
        => "CSkeletonInstance";
}

internal partial class ModelState : SchemaObject, IModelState
{
    private static readonly Lazy<int> ModelNameOffset = new (() => SchemaSystem.GetNetVarOffset("CModelState", "m_ModelName"));

    public string ModelName => _this.Add(ModelNameOffset.Value).Dereference().ReadStringUtf8(0);

#region Schemas

    [NativeSchemaField("CModelState", "m_MeshGroupMask", typeof(ulong))]
    private partial SchemaField GetMeshGroupMaskField();

#endregion

    public override string GetSchemaClassname()
        => "CModelState";
}
