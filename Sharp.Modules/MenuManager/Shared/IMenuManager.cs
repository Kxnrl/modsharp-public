using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Shared;

public interface IMenuManager
{
    const string Identity = nameof(IMenuManager);

    void DisplayMenu(IGameClient client, Menu menu);

    void QuitMenu(IGameClient client);

    bool IsInMenu(IGameClient client);
}
