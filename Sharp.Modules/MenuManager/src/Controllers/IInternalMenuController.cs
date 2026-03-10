using Sharp.Modules.MenuManager.Shared;

namespace Sharp.Modules.MenuManager.Core.Controllers;

public interface IInternalMenuController : IMenuController
{
    ulong SessionId { get; }

    bool MoveUpCursor();

    bool MoveDownCursor();

    void Confirm();

    void GoToPreviousPage();

    void GoToNextPage();

    bool IsInMenu(Menu menuInstance);

    bool IsInCurrentMenu(Menu menuInstance);
}
