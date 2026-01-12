using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Gag/ungag operations. Provided for callers; not intended for external implementations.
///     Side effects: updates cache, may notify players. Duplicate checks are the caller's responsibility (command handlers
///     already do this).
/// </summary>
public interface IGagService
{
    /// <summary>
    ///     Gag an online target (updates cache/storage, may notify).
    /// </summary>
    void Gag(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason);

    /// <summary>
    ///     Gag by SteamID (offline path; updates cache/storage when player joins).
    /// </summary>
    void Gag(IGameClient? admin, SteamID steamId, TimeSpan? duration, string reason);

    /// <summary>
    ///     Remove gag on an online target (updates cache/storage).
    /// </summary>
    void Ungag(IGameClient? admin, IGameClient target, string reason);
}
