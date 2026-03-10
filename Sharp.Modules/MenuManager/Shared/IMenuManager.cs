using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Shared;

public interface IMenuManager
{
    const string Identity = nameof(IMenuManager);

    void DisplayMenu(IGameClient client, Menu menu);

    void DisplayMenu(IGameClient client, Menu menu, out ulong sessionId);

    void QuitMenu(IGameClient client);

    /// <summary>
    /// Returns whether the client currently has an active menu session.
    /// </summary>
    bool IsInMenu(IGameClient client);

    /// <summary>
    /// Returns whether the client is currently in the specified menu instance,
    /// including parent menus in the current menu chain.
    /// </summary>
    bool IsInMenu(IGameClient client, Menu menuInstance);

    /// <summary>
    ///     Returns whether the client is currently in the specified menu session.
    /// </summary>
    bool IsInMenu(IGameClient client, ulong sessionId);

    /// <summary>
    /// Returns whether the current top menu is the specified menu instance.
    /// </summary>
    bool IsInCurrentMenu(IGameClient client, Menu menuInstance);

    /// <summary>
    /// Returns the current menu session identifier when the client is in a menu.
    /// </summary>
    bool TryGetCurrentMenuSessionId(IGameClient client, out ulong sessionId);
}
