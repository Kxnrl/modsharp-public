using System;
using System.Collections.Generic;
using Sharp.Modules.InputManager.Shared;

namespace Sharp.Modules.InputManager;

internal class InputListenerInfo
{
    private readonly List<ListenerInfo> _keyDownListeners = [];
    private readonly List<ListenerInfo> _keyHoldListeners = [];
    private readonly List<ListenerInfo> _keyUpListeners   = [];

    public List<ListenerInfo> GetListeners(InputState state)
    {
        return state switch
        {
            InputState.KeyDown => _keyDownListeners,
            InputState.KeyHold => _keyHoldListeners,
            InputState.KeyUp   => _keyUpListeners,
            _                  => throw new ArgumentException($"Unsupported input state: {state}"),
        };
    }
}
