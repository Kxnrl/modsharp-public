using System;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Shared;

public interface IMenuController : IDisposable
{
    Menu Menu { get; }

    void Refresh();

    IGameClient Player { get; }

    void Next(Menu menu);

    void Next(Func<IGameClient, Menu> menuFactory);

    void Exit();

    void GoBack();
}
