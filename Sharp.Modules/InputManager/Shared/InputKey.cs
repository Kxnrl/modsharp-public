using System;

namespace Sharp.Modules.InputManager.Shared;

/// <summary>
/// 输入键位。 <br/>
/// 这里的键位为「CS2本身的默认键位」，不考虑你改键的情况。换句话说这里都是「约定」。
/// </summary>
public enum InputKey
{
    W,
    S,
    A,
    D,
    F,
    G,
    Tab,
    E,
    Space,
    Shift,
    [Obsolete("Currently does nothing, DO NOT USE IT FOR NOW. It will be implemented in the future release. This is just a placeholder.")]
    F3,
    [Obsolete("Currently does nothing, DO NOT USE IT FOR NOW. It will be implemented in the future release. This is just a placeholder.")]
    F4,
}
