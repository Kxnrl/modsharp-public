using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Storage contract for admin operations (ban/mute/gag). This is the primary external extension point; implement this
///     to plug in your own persistence.
///     <para>Register your implementation with identity <see cref="Identity"/>.</para>
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
    Task RemoveAsync(SteamID steamId, AdminOperationType type);

    /// <summary>
    ///     Returns true if there is an active (non-expired/non-removed) record of the given type.
    /// </summary>
    Task<bool> HasActiveAsync(SteamID steamId, AdminOperationType type);
}

public enum AdminOperationType
{
    Ban,
    Mute, // Voice
    Gag,  // Text chat
}

public record AdminOperationRecord(
    SteamID            SteamId,
    AdminOperationType Type,
    SteamID?           AdminSteamId,
    DateTime           CreatedAt,
    DateTime?          ExpiresAt, // null = permanent
    string             Reason,
    SteamID?           RemovedBy    = null,
    DateTime?          RemovedAt    = null,
    string?            RemoveReason = null
)
{
    public bool IsExpired   => RemovedAt.HasValue || (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow);
    public bool IsPermanent => !ExpiresAt.HasValue;
}
