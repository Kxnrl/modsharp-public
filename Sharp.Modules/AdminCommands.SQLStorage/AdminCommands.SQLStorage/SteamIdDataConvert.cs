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
            parameter = new SugarParameter(name, null);
            parameter.CustomDbType = System.Data.DbType.UInt64;
            parameter.DbType = System.Data.DbType.UInt64;
            parameter.Value = id.AsPrimitive();
            parameter.TypeName = "UInt64";
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

        var value = (ulong)dataRecord.GetValue(dataRecordIndex);
        var steamId = new SteamID(value);

        return (T)(object)steamId;
    }
}