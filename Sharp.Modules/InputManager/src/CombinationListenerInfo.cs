using System;
using Sharp.Modules.InputManager.Shared;
using Sharp.Shared.Objects;

namespace Sharp.Modules.InputManager;

internal class CombinationListenerInfo
{
    public InputKey[]          Keys     { get; }
    public Action<IGameClient> Callback { get; }
    public InputState          State    { get; }

    public CombinationListenerInfo(InputKey[] keys, Action<IGameClient> callback, InputState state)
    {
        Keys     = keys;
        Callback = callback;
        State    = state;
    }
}
