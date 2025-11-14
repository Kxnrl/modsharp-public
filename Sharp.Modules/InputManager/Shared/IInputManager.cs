using System;
using Sharp.Shared.Objects;

namespace Sharp.Modules.InputManager.Shared;

public interface IInputManager
{
    const string Identity = nameof(IInputManager);

    /// <summary>
    /// Add a single key input listener
    /// </summary>
    /// <param name="key">The input key to listen for</param>
    /// <param name="callback">Callback function to invoke</param>
    /// <param name="state">The key state to listen for</param>
    /// <param name="holdDuration">Hold duration in seconds, only valid for Pressed state. 0 means no time restriction</param>
    public IDisposable AddInputListener(InputKey key, Action<IGameClient> callback, InputState state = InputState.JustPressed, float holdDuration = 0f);

    /// <summary>
    /// Add a combination key listener (all keys must be pressed simultaneously)
    /// </summary>
    /// <param name="keys">Array of keys for the combination</param>
    /// <param name="callback">Callback function to invoke</param>
    /// <param name="state">The key state to listen for, defaults to JustPressed</param>
    public IDisposable AddCombinationListener(InputKey[] keys, Action<IGameClient> callback, InputState state = InputState.JustPressed);

    public IDisposable GetClientNextChat(IGameClient client, Action<IGameClient, string> callback, bool handleChat = true);

    public void CancelClientNextChat(IGameClient client);
}
