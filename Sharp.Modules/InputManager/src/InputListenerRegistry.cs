using System;
using System.Collections.Generic;
using Sharp.Modules.InputManager.Shared;
using Sharp.Shared.Objects;

namespace Sharp.Modules.InputManager;

public class InputListenerRegistry : IInputListenerRegistry
{
    private readonly InputManager                                                            _manager;
    private readonly List<(InputKey Key, Action<IGameClient> Callback, InputState State)>    _inputListeners;
    private readonly List<(InputKey[] Keys, Action<IGameClient> Callback, InputState State)> _combinationListeners;

    internal InputListenerRegistry(InputManager manager)
    {
        _manager = manager;

        _inputListeners       = [];
        _combinationListeners = [];
    }

    public void AddInputListener(InputKey key,
        Action<IGameClient>               action,
        InputState                        state        = InputState.KeyDown,
        float                             holdDuration = 0)
    {
        _manager.AddInputListener(key, action, state, holdDuration);
        _inputListeners.Add((key, action, state));
    }

    public void AddCombinationListener(InputKey[] keys, Action<IGameClient> action, InputState state = InputState.KeyDown)
    {
        _manager.AddCombinationListener(keys, action, state);
        _combinationListeners.Add((keys, action, state));
    }

    internal void Cleanup()
    {
        foreach (var (key, callback, state) in _inputListeners)
        {
            _manager.RemoveInputListener(key, callback, state);
        }

        foreach (var (keys, callback, state) in _combinationListeners)
        {
            _manager.RemoveCombinationListener(keys, callback, state);
        }

        _inputListeners.Clear();
        _combinationListeners.Clear();
    }
}
