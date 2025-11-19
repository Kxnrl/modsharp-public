using System.Collections.Generic;

namespace Sharp.Modules.MenuManager.Core.Renderers;

internal enum RenderItemType
{
    Default,
    Disabled,
    Spacer,
}

internal class RenderItem
{
    public string         Content    { get; set; }
    public RenderItemType Type       { get; set; }
    public bool           IsSelected { get; set; }

    public RenderItem(string content, RenderItemType type, bool isSelected = false)
    {
        Content    = content;
        Type       = type;
        IsSelected = isSelected;
    }
}

internal class RenderData
{
    public required string           Title       { get; set; }
    public required List<RenderItem> Items       { get; set; }
    public          string?          Header      { get; set; }
    public          string?          Description { get; set; }
    public          string?          Footer      { get; set; }
}
