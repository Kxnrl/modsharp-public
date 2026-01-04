using System;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Core;

public readonly record struct PreviousMenu(Func<IGameClient, Menu> MenuFactory, Menu Menu, int Page, int Cursor);

public readonly record struct BuiltMenuItem(
    string                   Title,
    MenuItemState            State,
    float                    Width,
    Action<IMenuController>? Action = null,
    string?                  Color  = null);
