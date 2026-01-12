using Sharp.Shared.Objects;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Silence/unsilence operations (mute+gag). Provided for callers; not intended for external implementations.
///     Side effects: updates cache, may notify players. Duplicate checks are the caller's responsibility (command handlers
///     already do this).
/// </summary>
public interface ISilenceService
{
    /// <summary>
    ///     Apply silence (mute+gag) to an online target (updates cache/storage, may notify).
    /// </summary>
    void Silence(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason);

    /// <summary>
    ///     Remove silence (mute+gag) from an online target (updates cache/storage).
    /// </summary>
    void Unsilence(IGameClient? admin, IGameClient target, string reason);
}
