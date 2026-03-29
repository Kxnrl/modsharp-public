using System;

namespace Sharp.Modules.InputManager.Shared;

/// <summary>
///     Input Keys <br />
///     the keys in here is「CS2 Original binds」
/// </summary>
public enum InputKey
{
    W,
    S,
    A,
    D,
    F,
    Tab,
    E,
    R,
    Space,
    Shift,
    Attack1,
    Attack2,

    [Obsolete("Currently does nothing. It will be implemented in the future release. This is just a placeholder.")]
    F3,

    [Obsolete("Currently does nothing. It will be implemented in the future release. This is just a placeholder.")]
    F4,

    [Obsolete("Currently does nothing. It will be implemented in the future release. This is just a placeholder.")]
    G,
}
