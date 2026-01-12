using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Convenience helpers for constructing admin operation records against the storage contract.
/// </summary>
public static class AdminOperationStorageExtensions
{
    public static Task AddBanAsync(this IAdminOperationStorageService storage,
                                   SteamID                            targetId,
                                   SteamID?                           adminId,
                                   TimeSpan?                          duration,
                                   string                             reason)
        => storage.AddAsync(CreateRecord(targetId, adminId, AdminOperationType.Ban, duration, reason));

    public static Task RemoveBanAsync(this IAdminOperationStorageService storage, SteamID targetId)
        => storage.RemoveAsync(targetId, AdminOperationType.Ban);

    public static Task AddMuteAsync(this IAdminOperationStorageService storage,
                                    SteamID                            targetId,
                                    SteamID?                           adminId,
                                    TimeSpan?                          duration,
                                    string                             reason)
        => storage.AddAsync(CreateRecord(targetId, adminId, AdminOperationType.Mute, duration, reason));

    public static Task RemoveMuteAsync(this IAdminOperationStorageService storage, SteamID targetId)
        => storage.RemoveAsync(targetId, AdminOperationType.Mute);

    public static Task AddGagAsync(this IAdminOperationStorageService storage,
                                   SteamID                            targetId,
                                   SteamID?                           adminId,
                                   TimeSpan?                          duration,
                                   string                             reason)
        => storage.AddAsync(CreateRecord(targetId, adminId, AdminOperationType.Gag, duration, reason));

    public static Task RemoveGagAsync(this IAdminOperationStorageService storage, SteamID targetId)
        => storage.RemoveAsync(targetId, AdminOperationType.Gag);

    private static AdminOperationRecord CreateRecord(SteamID            targetId,
                                                     SteamID?           adminId,
                                                     AdminOperationType type,
                                                     TimeSpan?          duration,
                                                     string             reason)
    {
        var expiresAt = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : (DateTime?) null;

        return new AdminOperationRecord(targetId, type, adminId, DateTime.UtcNow, expiresAt, reason);
    }
}
