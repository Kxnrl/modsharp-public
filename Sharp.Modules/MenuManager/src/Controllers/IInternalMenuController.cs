using Sharp.Modules.MenuManager.Shared;

namespace Sharp.Modules.MenuManager.Core.Controllers;

public interface IInternalMenuController : IMenuController
{
    bool MoveUpCursor();

    bool MoveDownCursor();

    void Confirm();

    void GoToPreviousPage();

    void GoToNextPage();
}
