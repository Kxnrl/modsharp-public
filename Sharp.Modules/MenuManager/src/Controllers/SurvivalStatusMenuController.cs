using System;
using System.Linq;
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

        const int paddingItemCount = 2; // 游标视窗上下预留展示选项数量

        var offset = Cursor - ItemSkipCount;

        if (offset >= MaxPageItems - paddingItemCount)
        {
            ItemSkipCount = Cursor - (MaxPageItems - paddingItemCount - 1);

            var maxItemSkipCount = BuiltMenuItems.Count - MaxPageItems;

            if (ItemSkipCount >= maxItemSkipCount)
            {
                ItemSkipCount = maxItemSkipCount;
            }
        }
        else if (offset < paddingItemCount)
        {
            ItemSkipCount = Cursor - paddingItemCount;

            if (ItemSkipCount < 0)
            {
                ItemSkipCount = 0;
            }
        }

        string? header = null;

        if (PreviousMenus.Any())
        {
            var builder = new StringBuilder();

            foreach (var previousMenu in PreviousMenus.Reverse())
            {
                builder.Append(previousMenu.Menu.BuildTitle(Client));

                builder.Append(" > ");
            }

            var content = builder.ToString();

            header = content;
        }

        // title
        var title = Menu.BuildTitle(Client);

        sb.Append($"<font class='fontSize-m'>{title}<br><font class='fontSize-xs'>\u00A0<br></font><font class='fontSize-sm'>");

        // description
        string? description = null;

        if (Menu.Description is not null || Menu.DescriptionFactory is not null)
        {
            var content = Menu.BuildDescription(Client);

            description = content;
        }

        // colors
        const string keyColor      = "#DDAA11";
        const string textColor     = "#ffffff";
        const string disabledColor = "#333";
        const string cursorColor   = "#3399FF";

        var itemIndex = 1;

        foreach (var item in BuiltMenuItems.Skip(ItemSkipCount)
                                           .Take(MaxPageItems))
        {
            if (item.State == MenuItemState.Spacer)
            {
                sb.Append("<br>");
            }
            else if (item.State == MenuItemState.Disabled)
            {
                sb.Append($"{Colored(disabledColor, $"{itemIndex}. {item.Title}")}<br>");
            }
            else if ((itemIndex - 1) + ItemSkipCount == Cursor)
            {
                sb.Append(
                    $"{Colored(cursorColor, "►")} {Colored(keyColor, $"{itemIndex}.")} {Colored(textColor, item.Title)} {Colored(cursorColor, "◄")}<br>");
            }
            else
            {
                sb.Append($"{Colored(keyColor, $"{itemIndex}.")} {Colored(textColor, item.Title)}<br>");
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

        string Colored(string color, string content)
            => $"<font color='{color}'>{content}</font>";

        string Key(string key)
            => Colored(keyColor, key);

        string Text(string text)
            => Colored(textColor, text);
    }

    public override void Dispose()
    {
        base.Dispose();

        ModSharp.StopTimer(_timer);
    }
}
