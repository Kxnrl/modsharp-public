using System;
using Sharp.Shared.Objects;

namespace Sharp.Modules.InputManager.Shared;

public interface IInputManager
{
    const string Identity = nameof(IInputManager);

    public IDisposable AddInputListener(InputKey key, Action<IGameClient> callback);
}
