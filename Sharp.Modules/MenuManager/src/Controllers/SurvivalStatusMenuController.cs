using System;
using System.Linq;
using System.Text;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Core.Controllers;

internal class SurvivalStatusMenuController : BaseMenuController
{
    public SurvivalStatusMenuController(MenuManager menuManager,
        IModSharp                                       modSharp,
        IEventManager                                   eventManager,
        IEntityManager                                  entityManager,
        Func<IGameClient, Menu>                         menuFactory,
        IGameClient                                     player) : base(menuManager,
                                                                       modSharp,
                                                                       eventManager,
                                                                       entityManager,
                                                                       menuFactory,
                                                                       player)
    {
        _timer = modSharp.PushTimer(Think, 0.01, GameTimerFlags.Repeatable);
    }

    private void Think()
    {
        if (_cacheContent is null)
            return;

        Print(Client, _cacheContent);
    }

    private void Print(IGameClient client, string content)
    {
        if (_showSurvivalRespawnStatusEvent is null)
        {
            _showSurvivalRespawnStatusEvent = EventManager.CreateEvent("show_survival_respawn_status", false)
                                              ?? throw new Exception("Failed to create event");

            _showSurvivalRespawnStatusEvent.SetInt("duration", 5);
            _showSurvivalRespawnStatusEvent.SetInt("userid",   -1);
        }

        _showSurvivalRespawnStatusEvent.SetString("loc_token", content);
        _showSurvivalRespawnStatusEvent.FireToClient(client);
    }

    private readonly Guid    _timer;
    private          string? _cacheContent;

    private static IGameEvent? _showSurvivalRespawnStatusEvent;

    protected override void Render()
    {
        var sb = new StringBuilder();

        const int paddingItemCount = 2; // 游标视窗上下预留展示选项数量

        var offset = Cursor - ItemSkipCount;

        if (offset >= MaxPageItems - paddingItemCount)
        {
            ItemSkipCount = Cursor - (MaxPageItems - paddingItemCount - 1);

            var maxItemSkipCount = BuiltMenuItems.Count - MaxPageItems;

            if (ItemSkipCount >= maxItemSkipCount)
                ItemSkipCount = maxItemSkipCount;
        }
        else if (offset < paddingItemCount)
        {
            ItemSkipCount = Cursor - paddingItemCount;

            if (ItemSkipCount < 0)
                ItemSkipCount = 0;
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
                sb.Append($"<font color='#333'>{itemIndex}. <font color='#333'>{item.Title}<br>");
            }
            else if (itemIndex - 1 + ItemSkipCount == Cursor)
            {
                sb.Append(
                    $"<font color='#3399FF'>►<font color='#DDAA11'> {itemIndex}. <font color='#fff'>{item.Title} <font color='#3399FF'>◄<br>");
            }
            else
            {
                sb.Append($"<font color='#DDAA11'>{itemIndex}. <font color='#fff'>{item.Title}<br>");
            }

            itemIndex++;
        }

        // pad empty line
        for (var i = itemIndex; i <= MaxPageItems; i++)
        {
            sb.Append("<br/>");
        }

        // bottom
        sb.Append(
            $"<br><font class='fontSize-s'><font color='#DDAA11'>E<font color='#fff'> 选择 / <font color='#DDAA11'>W<font color='#fff'> 上一项 / <font color='#DDAA11'>S<font color='#fff'> 下一项 / <font color='#DDAA11'>Tab<font color='#fff'> 退出<br>");

        _cacheContent = sb.ToString();
    }

    public override void Dispose()
    {
        base.Dispose();

        ModSharp.StopTimer(_timer);
    }
}
