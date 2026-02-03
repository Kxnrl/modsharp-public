using System.Text.Json;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Storage contract for admin operations (ban/mute/gag). This is the primary external extension point; implement this
///     to plug in your own persistence.
///     <para>
///         Metadata on <see cref="AdminOperationRecord" /> is an optional JSON string for handler-specific payload
///         (e.g., weapon lists for custom operations).
///     </para>
///     <para>Register your implementation with identity <see cref="Identity" />.</para>
///     <para>All methods are async and should not touch anything from the game.</para>
/// </summary>
public interface IAdminOperationStorageService
{
    public const string Identity = nameof(IAdminOperationStorageService);

    /// <summary>
    ///     Returns a single record for the given SteamID and operation type, or null if none/expired.
    /// </summary>
    Task<AdminOperationRecord?> GetAsync(SteamID steamId, AdminOperationType type);

    /// <summary>
    ///     Returns all records for a SteamID (may include expired/removed ones depending on implementation policy).
    /// </summary>
    Task<IReadOnlyList<AdminOperationRecord>> GetAllAsync(SteamID steamId);

    /// <summary>
    ///     Adds a new record. Implementations should be idempotent if desired (skip or replace duplicates as needed).
    /// </summary>
    Task AddAsync(AdminOperationRecord record);

    /// <summary>
    ///     Removes a record of the given type for the SteamID (no-op if missing).
    /// </summary>
    Task RemoveAsync(SteamID targetId, AdminOperationType type, SteamID? removedBy, string? reason);

    /// <summary>
    ///     Returns true if there is an active (non-expired/non-removed) record of the given type.
    /// </summary>
    Task<bool> HasActiveAsync(SteamID steamId, AdminOperationType type);
}

public readonly record struct AdminOperationType(string Value)
{
    public static readonly AdminOperationType Ban  = new ("core:ban");
    public static readonly AdminOperationType Mute = new ("core:mute");
    public static readonly AdminOperationType Gag  = new ("core:gag");

    public override string ToString() => Value;

    public bool Equals(AdminOperationType other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
}

public record AdminOperationRecord(
    SteamID            SteamId,
    AdminOperationType Type,
    SteamID?           AdminSteamId,
    DateTime           CreatedAt,
    DateTime?          ExpiresAt, // null = permanent
    string             Reason,
    string?            Metadata     = null,
    SteamID?           RemovedBy    = null,
    DateTime?          RemovedAt    = null,
    string?            RemoveReason = null
)
{
    public bool IsExpired   => RemovedAt.HasValue || (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow);
    public bool IsPermanent => !ExpiresAt.HasValue;

    public T? GetMetadata<T>()
    {
        if (string.IsNullOrWhiteSpace(Metadata))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(Metadata);
        }
        catch
        {
            return default;
        }
    }
}
