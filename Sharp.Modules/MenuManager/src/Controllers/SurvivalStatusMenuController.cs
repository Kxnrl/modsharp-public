using System;
using System.Runtime.InteropServices;
using System.Text;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Core.Controllers;

internal class SurvivalStatusMenuController : BaseMenuController
{
    private readonly ILocalizerManager? _localizerManager;

    public SurvivalStatusMenuController(MenuManager menuManager,
        IModSharp                                   modSharp,
        IEventManager                               eventManager,
        IEntityManager                              entityManager,
        Func<IGameClient, Menu>                     menuFactory,
        IGameClient                                 player,
        ILocalizerManager?                          localizerManager) : base(menuManager,
                                                                             modSharp,
                                                                             eventManager,
                                                                             entityManager,
                                                                             menuFactory,
                                                                             player)
    {
        _localizerManager = localizerManager;
        _timer            = modSharp.PushTimer(Think, 0.01, GameTimerFlags.Repeatable);
    }

    private void Think()
    {
        if (_cacheContent is null)
        {
            return;
        }

        Print(Client, _cacheContent);
    }

    private void Print(IGameClient client, string content)
    {
        if (_showSurvivalRespawnStatusEvent is null)
        {
            _showSurvivalRespawnStatusEvent = EventManager.CreateEvent("show_survival_respawn_status", false)
                                              ?? throw new Exception("Failed to create event");

            _showSurvivalRespawnStatusEvent.SetInt("duration", 1);
            _showSurvivalRespawnStatusEvent.SetInt("userid",   -1);
        }

        _showSurvivalRespawnStatusEvent.SetString("loc_token", content);
        _showSurvivalRespawnStatusEvent.FireToClient(client);
    }

    private readonly Guid    _timer;
    private          string? _cacheContent;

    private static IGameEvent? _showSurvivalRespawnStatusEvent;

    public override void Render()
    {
        var sb = new StringBuilder();

        const int paddingItemCount = 2; // Buffer zones above and below the cursor

        var offset = Cursor - ItemSkipCount;

        // Scroll down if cursor hits the bottom buffer
        if (offset >= MaxPageItems - paddingItemCount)
        {
            ItemSkipCount = Cursor - (MaxPageItems - paddingItemCount - 1);

            // Prevent scrolling past the last item
            var maxItemSkipCount = Math.Max(0, BuiltMenuItems.Count - MaxPageItems);

            if (ItemSkipCount >= maxItemSkipCount)
            {
                ItemSkipCount = maxItemSkipCount;
            }
        }

        // Scroll up if cursor hits the top buffer
        else if (offset < paddingItemCount)
        {
            // Clamp to 0 to prevent negative skip when cursor is near the start
            ItemSkipCount = Math.Max(0, Cursor - paddingItemCount);
        }

        /*string? header = null;

        if (PreviousMenus.Count > 0)
        {
            var builder = new StringBuilder();

            foreach (var previousMenu in PreviousMenus.Reverse())
            {
                builder.Append(previousMenu.Menu.BuildTitle(Client));

                builder.Append(" > ");
            }

            var content = builder.ToString();

            header = content;
        }*/

        // title
        var title = Menu.BuildTitle(Client);

        sb.Append($"<font class='fontSize-m'>{title}<br><font class='fontSize-xs'>\u00A0<br></font><font class='fontSize-sm'>");

        // colors
        const string keyColor      = "#DDAA11";
        const string textColor     = "#ffffff";
        const string disabledColor = "#888888";
        const string cursorColor   = "#3399FF";

        var itemIndex = 1;

        var start = ItemSkipCount;
        var end   = Math.Min(start + MaxPageItems, BuiltMenuItems.Count);

        ReadOnlySpan<BuiltMenuItem> span = CollectionsMarshal.AsSpan(BuiltMenuItems);

        for (var i = start; i < end; i++)
        {
            ref readonly var item = ref span[i];

            if (item.State == MenuItemState.Spacer)
            {
                sb.Append("<br>");

                continue;
            }

            var indexStr = Menu.ShowIndex ? $"{Colored(keyColor, $"{itemIndex}.")} " : "";

            if (item.State == MenuItemState.Disabled)
            {
                sb.Append($"{Colored(disabledColor, $"{indexStr}{item.Title}")}<br>");
            }
            else if (i == Cursor)
            {
                sb.Append($"{Colored(cursorColor, Menu.CursorLeft)} {indexStr}{Colored(item.Color ?? textColor, item.Title)} {Colored(cursorColor, Menu.CursorRight)}<br>");
            }
            else
            {
                sb.Append($"{indexStr}{Colored(item.Color ?? textColor, item.Title)}<br>");
            }

            itemIndex++;
        }

        // pad empty line
        for (var i = itemIndex; i <= MaxPageItems; i++)
        {
            sb.Append("<br/>");
        }

        const string confirmKey  = "MenuSelection.Confirm";
        const string prevItemKey = "MenuSelection.PrevItem";
        const string nextItemKey = "MenuSelection.NextItem";
        const string exitKey     = "MenuSelection.Exit";
        const string backKey     = "MenuSelection.Back";

        string confirm;
        string prevItem;
        string nextItem;
        string exit;
        string back;

        if (_localizerManager is null)
        {
            confirm  = confirmKey;
            prevItem = prevItemKey;
            nextItem = nextItemKey;
            exit     = exitKey;
            back     = backKey;
        }
        else
        {
            var localizer = _localizerManager.GetLocalizer(Client);
            confirm  = localizer.TryGet(confirmKey)  ?? confirmKey;
            prevItem = localizer.TryGet(prevItemKey) ?? prevItemKey;
            nextItem = localizer.TryGet(nextItemKey) ?? nextItemKey;
            exit     = localizer.TryGet(exitKey)     ?? exitKey;
            back     = localizer.TryGet(backKey)     ?? backKey;
        }

        // sb.Append("<font class='fontSize-s'>");

        sb.Append($"{Key("F")} {Text(confirm)} / {Key("F3")} {Text(prevItem)} / {Key("F4")} {Text(nextItem)}");
        sb.Append("<br>"); // without this it won't show this hint
        sb.Append($"{Key("Tab")} {Text(exit)} / {Key("Shift")} {Text(back)}");

        // sb.Append("</font>");

        _cacheContent = sb.ToString();

        return;

        static string Colored(string color, string content)
            => $"<font color='{color}'>{content}</font>";

        static string Key(string key)
            => Colored(keyColor, key);

        static string Text(string text)
            => Colored(textColor, text);
    }

    public override void Dispose()
    {
        base.Dispose();

        ModSharp.StopTimer(_timer);
    }
}
