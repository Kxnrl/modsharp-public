using System;
using Sharp.Shared.Objects;

namespace Sharp.Modules.InputManager.Shared;

public interface IInputManager
{
    const string Identity = nameof(IInputManager);

    /// <summary>
    ///     Get or create an input listener registry for a module
    /// </summary>
    /// <param name="moduleIdentity">The module identity</param>
    /// <returns>The input listener registry for the module</returns>
    public IInputListenerRegistry GetInputListenerRegistry(string moduleIdentity);
}

public interface IInputListenerRegistry
{
    /// <summary>
    ///     Add a single key input listener
    /// </summary>
    /// <param name="key">The input key to listen for</param>
    /// <param name="action">Callback function to invoke</param>
    /// <param name="state">The key state to listen for</param>
    /// <param name="holdDuration">
    ///     Hold duration in seconds, only valid for KeyHold state. This means that how long the key
    ///     must be held down before the action is triggered.
    /// </param>
    void AddInputListener(InputKey key,
        Action<IGameClient>        action,
        InputState                 state        = InputState.KeyDown,
        float                      holdDuration = 0f);

    /// <summary>
    ///     Add a combination key listener (all keys must be pressed simultaneously)
    /// </summary>
    /// <param name="keys">Array of keys for the combination</param>
    /// <param name="action">Callback function to invoke</param>
    /// <param name="state">The key state to listen for, defaults to KeyDown</param>
    void AddCombinationListener(InputKey[] keys, Action<IGameClient> action, InputState state = InputState.KeyDown);
}
