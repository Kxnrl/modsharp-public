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

    public void Render(RenderData renderData)
    {
        var sb = new StringBuilder();

        sb.Append(
            $"<font class='fontSize-m'>{renderData.Title}<br><font class='fontSize-xs'>\u00A0<br></font><font class='fontSize-sm'>");

        foreach (var item in renderData.Items)
        {
            if (item.IsSelected)
            {
                sb.Append(
                    $"<font color='#3399FF'>►<font color='#DDAA11'> {item.No}. <font color='#fff'>{item.Content} <font color='#3399FF'>◄<br>");
            }
            else
            {
                sb.Append($"<font color='#DDAA11'>{item.No}. <font color='#fff'>{item.Content}<br>");
            }
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
