using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Mute/unmute operations. Provided for callers; not intended for external implementations.
///     Side effects: updates cache, may notify players. Duplicate checks are the caller's responsibility (command handlers
///     already do this).
/// </summary>
public interface IMuteService
{
    /// <summary>
    ///     Mute an online target (updates cache/storage, may notify).
    /// </summary>
    void Mute(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason);

    /// <summary>
    ///     Mute by SteamID (offline path; updates cache/storage when player joins).
    /// </summary>
    void Mute(IGameClient? admin, SteamID steamId, TimeSpan? duration, string reason);

    /// <summary>
    ///     Unmute an online target (updates cache/storage).
    /// </summary>
    void Unmute(IGameClient? admin, IGameClient target, string reason);
}
