using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Handler for a specific admin operation type (e.g. Ban, Mute, Gag).
///     Implementations run on the game thread and must remain fast/non-blocking (no I/O, no await).
///     OnApplied/OnRemoved may be called with a null target when the client is offline.
///     Use the provided record to implement your operation punishment to a player
/// </summary>
public interface IAdminOperationHandler
{
    /// <summary>
    ///     The operation type this handler is responsible for.
    /// </summary>
    AdminOperationType Type { get; }

    /// <summary>
    ///     Called when an operation is applied. <br />
    ///     Caller must guarantee that when <paramref name="targetClient" /> is not null, it matches
    ///     <see cref="AdminOperationRecord.SteamId" />.
    /// </summary>
    void OnApplied(AdminOperationRecord record, IGameClient? targetClient);

    /// <summary>
    ///     Called when an operation is removed. <br />
    ///     Caller must guarantee that when <paramref name="targetClient" /> is not null, it matches
    ///     <paramref name="steamId" />.
    /// </summary>
    void OnRemoved(SteamID steamId, IGameClient? targetClient);

    /// <summary>
    ///     Returns the localization key and fallback message when this operation is applied.
    /// </summary>
    (string Key, string Fallback) GetAppliedNotification(IGameClient target, string durationText);

    /// <summary>
    ///     Returns the localization key and fallback message when this operation is removed.
    /// </summary>
    (string Key, string Fallback) GetRemovedNotification(IGameClient target);
}
