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

using System.Data;
using Sharp.Shared.Units;
using SqlSugar;

namespace Sharp.Modules.AdminCommands.SQLStorage;

internal sealed class SteamIdDataConvert : ISugarDataConverter
{
    public SugarParameter ParameterConverter<T>(object columnValue, int columnIndex)
    {
        var name = $"@SteamID{columnIndex}";

        SugarParameter parameter;

        if (columnValue is SteamID id)
        {
            parameter = new SugarParameter(name, null)
            {
                CustomDbType = System.Data.DbType.UInt64,
                DbType       = System.Data.DbType.UInt64,
                Value        = id.AsPrimitive(),
                TypeName     = "UInt64",
            };
        }
        else if (columnValue is ulong x)
        {
            parameter = new SugarParameter(name, x, System.Data.DbType.UInt64);
        }
        else
        {
            parameter = new SugarParameter(name, null);
        }

        return parameter;
    }

    public T QueryConverter<T>(IDataRecord dataRecord, int dataRecordIndex)
    {
        if (dataRecord.IsDBNull(dataRecordIndex))
        {
            return default!;
        }

        var value   = (ulong) dataRecord.GetValue(dataRecordIndex);
        var steamId = new SteamID(value);

        return (T) (object) steamId;
    }
}
