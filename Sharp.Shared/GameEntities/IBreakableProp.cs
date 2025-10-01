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

using Sharp.Shared.Attributes;
using Sharp.Shared.Types;

namespace Sharp.Shared.GameEntities;

[NetClass("CBreakableProp")]
public interface IBreakableProp : IBaseAnimGraph
{
    /// <summary>
    ///     m_iMinHealthDmg
    /// </summary>
    int MinHealthDamage { get; set; }

    /// <summary>
    ///     m_hBreaker
    /// </summary>
    IBaseEntity? BreakerEntity { get; }

    /// <summary>
    ///     m_hBreaker
    /// </summary>
    CEntityHandle<IBaseEntity> BreakerEntityHandle { get; }

    /// <summary>
    ///     m_hLastAttacker
    /// </summary>
    IBaseEntity? LastAttackerEntity { get; }

    /// <summary>
    ///     m_hLastAttacker
    /// </summary>
    CEntityHandle<IBaseEntity> LastAttackerEntityHandle { get; }

    /// <summary>
    ///     m_explodeDamage
    /// </summary>
    float ExplodeDamage { get; set; }

    /// <summary>
    ///     m_explodeRadius
    /// </summary>
    float ExplodeRadius { get; set; }

    /// <summary>
    ///     m_explosionDelay
    /// </summary>
    float ExplosionDelay { get; set; }
}
