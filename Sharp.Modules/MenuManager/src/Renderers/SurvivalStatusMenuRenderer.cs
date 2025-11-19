using System;
using System.Linq;
using System.Text;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.MenuManager.Core.Renderers;

internal class SurvivalStatusMenuRenderer : IMenuRenderer
{
    private readonly Guid          _timer;
    private          string?       _cacheContent;
    private          int           _itemSkipCount;
    private readonly IModSharp     _modSharp;
    private readonly IEventManager _eventManager;
    private readonly IGameClient   _client;

    public bool IsValid => _client.IsValid;

    private static IGameEvent? _showSurvivalRespawnStatusEvent;

    public SurvivalStatusMenuRenderer(IModSharp modSharp, IEventManager eventManager, IGameClient player)
    {
        _client       = player;
        _modSharp     = modSharp;
        _eventManager = eventManager;
        _timer        = modSharp.PushTimer(Think, 0.01, GameTimerFlags.Repeatable);
    }

    private void Think()
    {
        if (_cacheContent is null)
            return;

        Print(_client, _cacheContent);
    }

    private void Print(IGameClient client, string content)
    {
        if (_showSurvivalRespawnStatusEvent is null)
        {
            _showSurvivalRespawnStatusEvent = _eventManager.CreateEvent("show_survival_respawn_status", false)
                                              ?? throw new Exception("Failed to create event");

            _showSurvivalRespawnStatusEvent.SetInt("duration", 5);
            _showSurvivalRespawnStatusEvent.SetInt("userid",   -1);
        }

        _showSurvivalRespawnStatusEvent.SetString("loc_token", content);
        _showSurvivalRespawnStatusEvent.FireToClient(client);
    }

    public void Render(RenderData renderData, int cursor)
    {
        const int maxItemCount     = 5; // 页面总数量
        const int paddingItemCount = 2; // 游标视窗上下预留展示选项数量

        var offset = cursor - _itemSkipCount;

        if (offset >= maxItemCount - paddingItemCount)
        {
            _itemSkipCount = cursor - (maxItemCount - paddingItemCount - 1);

            var maxItemSkipCount = renderData.Items.Count - maxItemCount;

            if (_itemSkipCount >= maxItemSkipCount)
                _itemSkipCount = maxItemSkipCount;
        }
        else if (offset < paddingItemCount)
        {
            _itemSkipCount = cursor - paddingItemCount;

            if (_itemSkipCount < 0)
                _itemSkipCount = 0;
        }

        var items = renderData.Items.Skip(_itemSkipCount)
                              .Take(maxItemCount);

        var sb = new StringBuilder();

        var index = 0;

        sb.Append(
            $"<font class='fontSize-m'>{renderData.Title}<br><font class='fontSize-xs'>\u00A0<br></font><font class='fontSize-sm'>");

        foreach (var item in items)
        {
            if (cursor == index + _itemSkipCount)
            {
                sb.Append(
                    $"<font color='#3399FF'>►<font color='#DDAA11'> {_itemSkipCount + index + 1}. <font color='#fff'>{renderData.Title} <font color='#3399FF'>◄<br>");
            }
            else
            {
                sb.Append($"<font color='#DDAA11'>{_itemSkipCount + index + 1}. <font color='#fff'>{renderData.Title}<br>");
            }

            index++;
        }

        sb.Append(
            $"<br><font class='fontSize-s'><font color='#DDAA11'>E<font color='#fff'> 选择 / <font color='#DDAA11'>R<font color='#fff'> 上一项 / <font color='#DDAA11'>F<font color='#fff'> 下一项 / <font color='#DDAA11'>Tab<font color='#fff'> 退出<br>");

        _cacheContent = sb.ToString();
    }

    public void Dispose()
    {
        _modSharp.StopTimer(_timer);
    }
}
