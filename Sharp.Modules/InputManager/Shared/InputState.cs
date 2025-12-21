namespace Sharp.Modules.InputManager.Shared;

/// <summary>
///     Defines different states of key input
/// </summary>
public enum InputState
{
    /// <summary>
    ///     Key was just pressed this frame
    /// </summary>
    KeyDown,

    /// <summary>
    ///     Key is being held down
    /// </summary>
    KeyHold,

    /// <summary>
    ///     Key was just released this frame
    /// </summary>
    KeyUp,
}
