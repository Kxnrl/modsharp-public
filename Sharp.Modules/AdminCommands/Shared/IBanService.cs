using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Ban/unban operations. Provided for callers; not intended for external implementations.
///     Side effects: updates cache, may kick/notify. Duplicate checks are the caller's responsibility (command handlers
///     already do this).
/// </summary>
public interface IBanService
{
    /// <summary>
    ///     Ban an online target (may kick/notify, updates cache).
    /// </summary>
    void Ban(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason);

    /// <summary>
    ///     Ban by SteamID (offline path; updates cache/storage, may kick if target joins).
    /// </summary>
    void Ban(IGameClient? admin, SteamID steamId, TimeSpan? duration, string reason);

    /// <summary>
    ///     Remove a ban for the given SteamID (updates cache/storage).
    /// </summary>
    void Unban(IGameClient? admin, SteamID steamId, string reason);
}
