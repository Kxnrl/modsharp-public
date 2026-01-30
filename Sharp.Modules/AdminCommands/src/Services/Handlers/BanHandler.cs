using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services.Handlers;

internal class BanHandler : IAdminOperationHandler, IAdminOperationHookRegistrar
{
    private readonly Dictionary<SteamID, DateTime?> _bans = new ();

    private readonly InterfaceBridge     _bridge;
    private readonly ILogger<BanHandler> _logger;
    private          bool                _hooksRegistered;

    public BanHandler(InterfaceBridge bridge)
    {
        _bridge = bridge;
        _logger = bridge.LoggerFactory.CreateLogger<BanHandler>();
    }

    public AdminOperationType Type => AdminOperationType.Ban;

    public void OnApplied(AdminOperationRecord record, IGameClient? targetClient)
    {
        SetBanned(record.SteamId, true, record.ExpiresAt);

        if (targetClient is not null)
        {
            _bridge.ClientManager.KickClient(targetClient, record.Reason, NetworkDisconnectionReason.SteamBanned);
        }
    }

    public void OnRemoved(SteamID targetId, IGameClient? targetClient)
    {
        SetBanned(targetId, false);
    }

    public (string Key, string Fallback) GetAppliedNotification(IGameClient target, string durationText)
        => ("Admin.BanApplied", $"banned {target.Name} {durationText}");

    public (string Key, string Fallback) GetRemovedNotification(IGameClient target)
        => ("Admin.BanRemoved", $"unbanned {target.Name}");

    public void RegisterHooks()
    {
        if (_hooksRegistered)
        {
            return;
        }

        _bridge.HookManager.ConnectClient.InstallHookPre(OnConnectClientPre);
        _hooksRegistered = true;
    }

    public void UnregisterHooks()
    {
        if (!_hooksRegistered)
        {
            return;
        }

        _bridge.HookManager.ConnectClient.RemoveHookPre(OnConnectClientPre);

        _hooksRegistered = false;
    }

    private HookReturnValue<NetworkDisconnectionReason> OnConnectClientPre(IConnectClientHookParams                    @params,
                                                                           HookReturnValue<NetworkDisconnectionReason> arg2)
    {
        var steamId = @params.SteamId;

        if (!IsBanned(steamId))
        {
            return new HookReturnValue<NetworkDisconnectionReason>();
        }

        return new HookReturnValue<NetworkDisconnectionReason>(EHookAction.SkipCallReturnOverride,
                                                               NetworkDisconnectionReason.SteamBanned);
    }

    private bool IsBanned(SteamID steamId)
    {
        if (!_bans.TryGetValue(steamId, out var expiresAt))
        {
            return false;
        }

        if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
        {
            _bans.Remove(steamId);

            return false;
        }

        return true;
    }

    private void SetBanned(SteamID steamId, bool banned, DateTime? expiresAt = null)
    {
        if (banned)
        {
            _bans[steamId] = expiresAt;
        }
        else
        {
            _bans.Remove(steamId);
        }
    }
}
