using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Shared;

public interface IMenuManager
{
    const string Identity = nameof(IMenuManager);

    public void DisplayMenu(IGameClient client, Menu menu);
}
