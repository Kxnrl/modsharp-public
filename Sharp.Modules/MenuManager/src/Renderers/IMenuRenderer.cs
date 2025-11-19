using System;

namespace Sharp.Modules.MenuManager.Core.Renderers;

internal interface IMenuRenderer : IDisposable
{
    void Render(RenderData data, int cursor);

    bool IsValid { get; }
}
