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

using Sharp.Generator;
using Sharp.Shared;
using Sharp.Shared.GameEntities;

namespace Sharp.Core.GameEntities;

internal partial class BaseTeam : BaseEntity, IBaseTeam
{
#region Schemas

    [NativeSchemaField("CCSTeam", "m_iScore", typeof(int))]
    private partial SchemaField GetScoreField();

    [NativeSchemaField("CCSTeam", "m_scoreFirstHalf", typeof(int))]
    private partial SchemaField GetScoreFirstHalfField();

    [NativeSchemaField("CCSTeam", "m_scoreSecondHalf", typeof(int))]
    private partial SchemaField GetScoreSecondHalfField();

    [NativeSchemaField("CCSTeam", "m_scoreOvertime", typeof(int))]
    private partial SchemaField GetScoreOvertimeField();

    [NativeSchemaField("CCSTeam", "m_iLastUpdateSentAt", typeof(int))]
    private partial SchemaField GetLastUpdateSentAtField();

    [NativeSchemaField("CCSTeam", "m_flNextResourceTime", typeof(float))]
    private partial SchemaField GetNextResourceTimeField(); 

#endregion
}
