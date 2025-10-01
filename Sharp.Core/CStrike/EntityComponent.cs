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

using Sharp.Core.GameEntities;
using Sharp.Core.Helpers;
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Utilities;

namespace Sharp.Core.CStrike;

internal abstract class EntityComponent : SchemaObject, IEntityComponent
{
    protected EntityComponent(nint ptr) : base(ptr)
    {
    }

    private int? _offset;

    public int ChainOffset => _offset ??= SchemaSystem.GetNetVarOffset(GetSchemaClassname(), "__m_pChainEntity");

    public IBaseEntity? ChainEntity => BaseEntity.Create(_this.Add(ChainOffset));
}

internal abstract partial class EntityObject : SchemaObject, IEntityObject
{
    protected EntityObject(nint ptr) : base(ptr)
    {
    }
}
