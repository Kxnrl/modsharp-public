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
                                   string                             reason,
                                   string?                            metadata = null)
        => storage.AddAsync(CreateRecord(targetId, adminId, AdminOperationType.Ban, duration, reason, metadata));

    public static Task RemoveBanAsync(this IAdminOperationStorageService storage,
                                      SteamID                            targetId,
                                      SteamID?                           adminId,
                                      string?                            removeReason)
        => storage.RemoveAsync(targetId, AdminOperationType.Ban, adminId, removeReason);

    public static Task AddMuteAsync(this IAdminOperationStorageService storage,
                                    SteamID                            targetId,
                                    SteamID?                           adminId,
                                    TimeSpan?                          duration,
                                    string                             reason,
                                    string?                            metadata = null)
        => storage.AddAsync(CreateRecord(targetId, adminId, AdminOperationType.Mute, duration, reason, metadata));

    public static Task RemoveMuteAsync(this IAdminOperationStorageService storage,
                                       SteamID                            targetId,
                                       SteamID?                           adminId,
                                       string?                            removeReason)
        => storage.RemoveAsync(targetId, AdminOperationType.Mute, adminId, removeReason);

    public static Task AddGagAsync(this IAdminOperationStorageService storage,
                                   SteamID                            targetId,
                                   SteamID?                           adminId,
                                   TimeSpan?                          duration,
                                   string                             reason,
                                   string?                            metadata = null)
        => storage.AddAsync(CreateRecord(targetId, adminId, AdminOperationType.Gag, duration, reason, metadata));

    public static Task RemoveGagAsync(this IAdminOperationStorageService storage,
                                      SteamID                            targetId,
                                      SteamID?                           adminId,
                                      string?                            removeReason)
        => storage.RemoveAsync(targetId, AdminOperationType.Gag, adminId, removeReason);

    private static AdminOperationRecord CreateRecord(SteamID            targetId,
                                                     SteamID?           adminId,
                                                     AdminOperationType type,
                                                     TimeSpan?          duration,
                                                     string             reason,
                                                     string?            metadata = null)
    {
        var expiresAt = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : (DateTime?) null;

        return new AdminOperationRecord(targetId, type, adminId, DateTime.UtcNow, expiresAt, reason, metadata);
    }
}
