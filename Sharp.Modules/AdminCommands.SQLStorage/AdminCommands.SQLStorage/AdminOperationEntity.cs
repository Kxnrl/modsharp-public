using Sharp.Shared.Units;
using SqlSugar;

namespace Sharp.Modules.AdminCommands.SQLStorage;

[SugarTable("admin_operations")]
[SugarIndex("idx_operations_lookup",
            nameof(TargetSteamId),
            OrderByType.Asc,
            nameof(Type),
            OrderByType.Asc,
            nameof(RemovedAt),
            OrderByType.Asc,
            nameof(ExpiresAt),
            OrderByType.Asc)]
internal sealed class AdminOperationEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(SqlParameterDbType = typeof(SteamIdDataConvert), ColumnDataType = "BIGINT UNSIGNED")]
    public SteamID TargetSteamId { get; set; }

    [SugarColumn(Length = 64)]
    public string Type { get; set; } = string.Empty;

    [SugarColumn(SqlParameterDbType = typeof(SteamIdDataConvert), ColumnDataType = "BIGINT UNSIGNED", IsNullable = true)]
    public SteamID? AdminSteamId { get; set; }

    public DateTime CreatedAt { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? ExpiresAt { get; set; }

    [SugarColumn(Length = 2048)]
    public string Reason { get; set; } = string.Empty;

    [SugarColumn(Length = 4096, IsNullable = true)]
    public string? Metadata { get; set; }

    [SugarColumn(SqlParameterDbType = typeof(SteamIdDataConvert), ColumnDataType = "BIGINT UNSIGNED", IsNullable = true)]
    public SteamID? RemovedBy { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? RemovedAt { get; set; }

    [SugarColumn(Length = 2048, IsNullable = true)]
    public string? RemoveReason { get; set; }
}
