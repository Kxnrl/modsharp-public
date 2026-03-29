using System;
using Sharp.Shared.Objects;

namespace Sharp.Modules.InputManager;

internal class ListenerInfo
{
    public Action<IGameClient> Callback     { get; }
    public float               HoldDuration { get; }

    public ListenerInfo(Action<IGameClient> callback, float holdDuration)
    {
        Callback     = callback;
        HoldDuration = holdDuration;
    }
}