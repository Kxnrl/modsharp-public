using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.MenuManager.Core.Renderers;

internal class ViewmodelMenuRenderer : IMenuRenderer
{
    private static readonly Color32 DefaultColor   = new (255, 255, 255, 255);
    private static readonly Color32 SecondaryColor = new (200, 200, 200, 255);
    private static readonly Color32 DisabledColor  = new (180, 180, 180, 255);

    private readonly IEntityManager _entityManager;

    private IPlayerPawn   ViewModel         { get; }
    private IGameClient   Player            { get; }
    private WorldTextPool WorldTextPool     { get; }
    private IWorldText?   BackgroundEntity  { get; set; }
    private IWorldText?   BackgroundEntity2 { get; set; }
    private IWorldText?   CursorEntity      { get; set; }
    private IWorldText?   CursorEntity2     { get; set; }

    public bool IsValid
        => ViewModel.IsValid()
           && Player.IsValid
           && WorldTextPool.IsValid
           && (BackgroundEntity?.IsValid()  ?? false)
           && (BackgroundEntity2?.IsValid() ?? false)
           && (CursorEntity?.IsValid()      ?? false)
           && (CursorEntity2?.IsValid()     ?? false);

    public ViewmodelMenuRenderer(IEntityManager entityManager, IGameClient player)
    {
        _entityManager = entityManager;

        Player = player;

        ViewModel = Player.GetPlayerController()
                          ?
                          .GetPlayerPawn();

        WorldTextPool = new WorldTextPool(entityManager, ViewModel, _ => { });
    }

    public void Dispose()
    {
        WorldTextPool.Dispose();

        if (BackgroundEntity?.IsValid() ?? false)
            BackgroundEntity?.Kill();

        if (BackgroundEntity2?.IsValid() ?? false)
            BackgroundEntity2?.Kill();

        if (CursorEntity?.IsValid() ?? false)
            CursorEntity?.Kill();

        if (CursorEntity2?.IsValid() ?? false)
            CursorEntity2?.Kill();
    }

    public void Render(RenderData data, int cursor)
    {
        // hack to make sure origin is correct
        // ViewModel.SetAbsOrigin(new Vector());
        // ViewModel.SetAbsAngles(new Vector());

        // calc position
        var origin = ViewModel.GetEyePosition();
        var angles = ViewModel.GetAbsAngles();

        angles.AnglesToVector(out var forward, out var right, out var up);

        // prepare text origin & angles
        var textOrigin = origin + (forward * 9 + up * 6 + right * 0);
        var textAngles = angles;
        textAngles.Z += 90;
        textAngles.Y -= 90;

        if (BackgroundEntity is null)
        {
            BackgroundEntity = _entityManager.SpawnEntitySync<IWorldText>("point_worldtext",
                                                                          new Dictionary<string, KeyValuesVariantValueItem>
                                                                          {
                                                                              { "classname", "point_worldtext" },
                                                                              { "rendercolor", "255 255 255 255" },
                                                                              { "message", "" },
                                                                              { "enabled", true },
                                                                              { "fullbright", true },
                                                                              { "color", "255 255 255 255" },
                                                                              { "world_units_per_pixel", 0.005f },
                                                                              { "font_size", 125f },
                                                                              { "font_name", "Arial Black" },
                                                                              { "justify_horizontal", "0" },
                                                                              { "justify_vertical", "2" },
                                                                              { "reorient_mode", "0" },
                                                                              { "depth_render_offset", 0f },

                                                                              { "draw_background", true },
                                                                              {
                                                                                  "background_material_name",
                                                                                  "materials/dev/annotation_worldtext_background.vmat"
                                                                              },
                                                                              { "background_world_to_uv", 100f },
                                                                              { "background_border_height", 0.1f },
                                                                              { "background_border_width", 0.1f }
                                                                          });

            BackgroundEntity.AcceptInput("SetParent", ViewModel, BackgroundEntity, "!activator");
        }

        BackgroundEntity.Teleport(textOrigin + (forward * 0.1f) - (right * 0.1f / 2), textAngles);

        if (BackgroundEntity2 is null)
        {
            BackgroundEntity2 = _entityManager.SpawnEntitySync<IWorldText>("point_worldtext",
                                                                           new Dictionary<string, KeyValuesVariantValueItem>
                                                                           {
                                                                               { "classname", "point_worldtext" },
                                                                               { "rendercolor", "255 255 255 255" },
                                                                               { "message", "" },
                                                                               { "enabled", true },
                                                                               { "fullbright", true },
                                                                               { "color", "255 255 255 255" },
                                                                               { "world_units_per_pixel", 0.005f },
                                                                               { "font_size", 125f },
                                                                               { "font_name", "Arial Black" },
                                                                               { "justify_horizontal", "0" },
                                                                               { "justify_vertical", "2" },
                                                                               { "reorient_mode", "0" },
                                                                               { "depth_render_offset", 0f },

                                                                               { "draw_background", true },
                                                                               {
                                                                                   "background_material_name",
                                                                                   "materials/dev/annotation_worldtext_background.vmat"
                                                                               },
                                                                               { "background_world_to_uv", 100f },
                                                                               { "background_border_height", 0.1f },
                                                                               { "background_border_width", 0.1f }
                                                                           });

            BackgroundEntity2.AcceptInput("SetParent", ViewModel, BackgroundEntity2, "!activator");
        }

        BackgroundEntity2.Teleport(textOrigin + (forward * 0.1f) - (right * 0.1f / 2), textAngles);

        if (CursorEntity is null)
        {
            CursorEntity = _entityManager.SpawnEntitySync<IWorldText>("point_worldtext",
                                                                      new Dictionary<string, KeyValuesVariantValueItem>
                                                                      {
                                                                          { "classname", "point_worldtext" },
                                                                          { "rendercolor", "255 255 255 255" },
                                                                          { "message", "" },
                                                                          { "enabled", true },
                                                                          { "fullbright", true },
                                                                          { "color", "255 255 255 255" },
                                                                          { "world_units_per_pixel", 0.005f },
                                                                          { "font_size", 125f },
                                                                          { "font_name", "Arial Black" },
                                                                          { "justify_horizontal", "0" },
                                                                          { "justify_vertical", "2" },
                                                                          { "reorient_mode", "0" },
                                                                          { "depth_render_offset", 0f },

                                                                          { "draw_background", true },
                                                                          {
                                                                              "background_material_name",
                                                                              "materials/dev/annotation_worldtext_background.vmat"
                                                                          },
                                                                          { "background_world_to_uv", 100f },
                                                                          { "background_border_height", 0f },
                                                                          { "background_border_width", 0.1f },
                                                                      });

            CursorEntity.AcceptInput("SetParent", ViewModel, CursorEntity, "!activator");
        }

        CursorEntity.Teleport(textOrigin + (forward * 0.1f) - (right * 0.1f / 2), textAngles);

        if (CursorEntity2 is null)
        {
            CursorEntity2 = _entityManager.SpawnEntitySync<IWorldText>("point_worldtext",
                                                                       new Dictionary<string, KeyValuesVariantValueItem>
                                                                       {
                                                                           { "classname", "point_worldtext" },
                                                                           { "rendercolor", "255 255 255 255" },
                                                                           { "message", "" },
                                                                           { "enabled", true },
                                                                           { "fullbright", true },
                                                                           { "color", "255 255 255 255" },
                                                                           { "world_units_per_pixel", 0.005f },
                                                                           { "font_size", 125f },
                                                                           { "font_name", "Arial Black" },
                                                                           { "justify_horizontal", "0" },
                                                                           { "justify_vertical", "2" },
                                                                           { "reorient_mode", "0" },
                                                                           { "depth_render_offset", 0f },

                                                                           { "draw_background", true },
                                                                           {
                                                                               "background_material_name",
                                                                               "materials/dev/annotation_worldtext_background.vmat"
                                                                           },
                                                                           { "background_world_to_uv", 100f },
                                                                           { "background_border_height", 0f },
                                                                           { "background_border_width", 0.1f },
                                                                       });

            CursorEntity2.AcceptInput("SetParent", ViewModel, CursorEntity2, "!activator");
        }

        CursorEntity2.Teleport(textOrigin + (forward * 0.1f) - (right * 0.1f / 2), textAngles);

        WorldTextPool.ResetCursor();

        var builder = new MessageBuilder();

        builder.AppendEmpty();
        builder.AppendPrimary(data.Title);

        var descriptionLines = (int) MathF.Ceiling(((data.Description?.Count(x => x == '\n') ?? 0) + 1) * (2 / 3f));

        while (descriptionLines-- > 0)
        {
            builder.AppendEmpty();
        }

        foreach (var item in data.Items)
        {
            if (item.Type == RenderItemType.Spacer)
            {
                builder.AppendEmpty();
            }
            else if (item.Type == RenderItemType.Disabled)
            {
                builder.AppendDisabled(item.Content);
            }
            else
            {
                builder.AppendPrimary(item.Content, item.IsSelected);
            }
        }

        if (data.Footer is not null)
            builder.AddSecondaryMaxWidth(data.Footer);

        if (data.Description is not null)
            builder.AddSecondaryMaxWidth(data.Description);

        var x = builder.BuildCursor();
        CursorEntity.Message  = x;
        CursorEntity2.Message = x;

        var backgroundBuilder = builder.GetBackgroundBuilder();
        var totalLineCount    = builder.TotalLineCount;

        // primary
        {
            var entity = WorldTextPool.GetNextWorldText();
            entity.Teleport(textOrigin, textAngles);

            entity.Message  = builder.Primary.ToString();
            entity.FontSize = 125f;
            entity.Color    = DefaultColor;
        }

        // disabled
        {
            var entity = WorldTextPool.GetNextWorldText();
            entity.Teleport(textOrigin, textAngles);

            entity.Message  = builder.Disabled.ToString();
            entity.FontSize = 125f;
            entity.Color    = DisabledColor;
        }

        // header & description & footer
        {
            var entity = WorldTextPool.GetNextWorldText();
            entity.Teleport(textOrigin - (up * 0.2f) + (right * 0.55f), textAngles);

            backgroundBuilder.Append("\n\n\n\u3000");

            entity.Message
                = $"{data.Header}\n\n\n{data.Description}{new string('\n', (int) Math.Ceiling(totalLineCount * 1.2))}{data.Footer}";

            entity.FontSize = 125f * (2f / 3f);
            entity.Color    = SecondaryColor;
        }

        var background = backgroundBuilder.ToString();
        BackgroundEntity.Message  = background;
        BackgroundEntity2.Message = background;

        WorldTextPool.ResetTheRest();
    }
}

public class MessageBuilder
{
    private const char Padding = '\u3000';

    public  StringBuilder Primary    { get; } = new ();
    public  StringBuilder Disabled   { get; } = new ();
    private StringBuilder Background { get; } = new ();
    private StringBuilder Cursor     { get; } = new ();

    public int TotalLineCount { get; private set; }

    private int _maxWidth;
    private int _cursorLineCount;

    public void AddSecondaryMaxWidth(string text)
    {
        const int paddingWidth = 8;

        var width = (int) MathF.Ceiling(GetMaxLineWidth(text) * (2f / 3)) + paddingWidth;

        if (width > _maxWidth)
        {
            _maxWidth = width;
        }
    }

    private void CalcMaxWidth(string text)
    {
        const int paddingWidth = 6;

        var width = GetMaxLineWidth(text) + paddingWidth;

        if (width > _maxWidth)
        {
            _maxWidth = width;
        }
    }

    private int GetMaxLineWidth(string text)
        => text.Split('\n')
               .Select(item => item.Sum(x => IsCharAscii(x) ? 1 : 2))
               .Max();

    public void AppendPrimary(string text, bool isSelected = false)
    {
        CalcMaxWidth(text);

        var lineCount = GetLineCount(text);
        var padding   = GetLinePadding(text, lineCount);

        Primary.Append($"{Padding}{text.Replace("\n", $"\n{Padding}")}\n");
        Disabled.Append(padding);
        Background.Append(padding.Replace("\n", $"{Padding}\n"));

        if (isSelected)
        {
            // placeholder to replace
            Cursor.Append('|');
            _cursorLineCount = lineCount;
        }
        else
        {
            Cursor.Append(padding);
        }

        TotalLineCount += lineCount;
    }

    private static bool IsCharAscii(char c)
        => c < 128;

    public void AppendEmpty()
    {
        Primary.Append('\n');
        Disabled.Append('\n');
        Background.Append($"{Padding}\n");
        Cursor.Append('\n');

        TotalLineCount++;
    }

    public void AppendDisabled(string text)
    {
        CalcMaxWidth(text);

        var lineCount = GetLineCount(text);
        var padding   = GetLinePadding(text, lineCount);

        Primary.Append(padding);
        Disabled.Append($"{Padding}{text.Replace("\n", $"\n{Padding}")}\n");
        Background.Append(padding.Replace("\n", $"{Padding}\n"));
        Cursor.Append(padding);

        TotalLineCount += lineCount;
    }

    public StringBuilder GetBackgroundBuilder()
    {
        Background.Append(new string(Padding, _maxWidth / 2));

        if (_maxWidth % 2 == 1)
        {
            Background.Append(' ');
        }

        return Background;
    }

    public string BuildCursor()
    {
        var cursor = new StringBuilder();
        cursor.Append(new string(Padding, _maxWidth / 2));

        if (_maxWidth % 2 == 1)
        {
            cursor.Append(' ');
        }

        var cursorPaddingLine = _cursorLineCount;

        while (--cursorPaddingLine > 0)
        {
            cursor.Append($"\n{Padding}");
        }

        return Cursor.Replace("|", cursor.ToString())
                     .ToString();
    }

    private static int GetLineCount(string text)
        => 1 + text.Count(c => c == '\n');

    private static string GetLinePadding(string text, int lineCount)
        => new string('\n', lineCount);
}
