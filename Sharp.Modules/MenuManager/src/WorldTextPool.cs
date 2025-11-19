using System;
using System.Collections.Generic;
using System.Linq;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;

namespace Sharp.Modules.MenuManager.Core;

internal class WorldTextPool : IDisposable
{
    private readonly Action<IWorldText> _onEntityCreated;
    private readonly List<IWorldText>   _list = [];
    private readonly IBaseEntity        _parentEntity;
    private          int                _cursor;
    private readonly IEntityManager     _entityManager;

    public WorldTextPool(IEntityManager entityManager, IBaseEntity parentEntity, Action<IWorldText> onEntityCreated)
    {
        _entityManager   = entityManager;
        _parentEntity    = parentEntity;
        _onEntityCreated = onEntityCreated;
    }

    public bool IsValid => _list.All(x => x.IsValid()) && _parentEntity.IsValid();

    public void ResetCursor()
    {
        _cursor = 0;
    }

    public IWorldText GetNextWorldText()
    {
        if (_cursor < _list.Count)
            return _list[_cursor++];

        var entity = _entityManager.SpawnEntitySync<IWorldText>("point_worldtext",
                                                                new Dictionary<string, KeyValuesVariantValueItem>
                                                                {
                                                                    { "classname", "point_worldtext" },
                                                                    { "rendercolor", "255 255 255 255" },
                                                                    { "message", "" },
                                                                    { "enabled", true },
                                                                    { "fullbright", true },
                                                                    { "color", "255 255 255 255" },
                                                                    { "world_units_per_pixel", 0.05f },
                                                                    { "font_size", 60f },
                                                                    { "font_name", "Arial Black" },
                                                                    { "justify_horizontal", "0" },
                                                                    { "justify_vertical", "2" },
                                                                    { "reorient_mode", "0" },
                                                                    { "depth_render_offset", 0f },
                                                                });

        entity.AcceptInput("SetParent", _parentEntity, entity, "!activator");

        _onEntityCreated(entity);

        _list.Add(entity);
        _cursor++;

        return entity;
    }

    public void ResetTheRest()
    {
        foreach (var entity in _list.Skip(_cursor))
        {
            entity.Message = "";
        }
    }

    public void Dispose()
    {
        foreach (var worldText in _list)
        {
            if (worldText.IsValid())
                worldText.Kill();
        }

        _list.Clear();
    }
}
