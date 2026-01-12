using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Common;
using Sharp.Modules.AdminCommands.Services.Internal;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services;

/// <summary>
///     Core execution pipeline for admin operations (ban/mute/gag) and load-on-connect handling.
///     Provides shared behaviors: cache updates, storage persistence, notifications, kick, and listener hooks.
/// </summary>
internal class AdminOperationEngine : IClientListener
{
    public int ListenerVersion  => IClientListener.ApiVersion;
    public int ListenerPriority => 0;

    private readonly InterfaceBridge               _bridge;
    private readonly AdminOperationCache           _cache;
    private readonly AdminOperationService         _operations;
    private readonly ModuleContext                 _moduleContext;
    private readonly ILogger<AdminOperationEngine> _logger;

    public AdminOperationEngine(
        InterfaceBridge       bridge,
        AdminOperationCache   cache,
        AdminOperationService operations,
        ModuleContext         moduleContext)
    {
        _bridge        = bridge;
        _cache         = cache;
        _operations    = operations;
        _moduleContext = moduleContext;
        _logger        = bridge.LoggerFactory.CreateLogger<AdminOperationEngine>();
    }

    public void Init()
    {
        _bridge.ClientManager.InstallClientListener(this);
    }

    public void Shutdown()
    {
        _bridge.ClientManager.RemoveClientListener(this);
    }

    public void ApplyOnline(IGameClient?       admin,
                            IGameClient        target,
                            AdminOperationType type,
                            TimeSpan?          duration,
                            string             reason,
                            bool               silent = false)
        => ApplyCore(admin, target, target.SteamId, target.Name, target.Slot, type, duration, reason, silent);

    public void ApplyOffline(IGameClient?       admin,
                             SteamID            steamId,
                             string             targetName,
                             AdminOperationType type,
                             TimeSpan?          duration,
                             string             reason)
        => ApplyCore(admin, null, steamId, targetName, null, type, duration, reason, true);

    public void RemoveOnline(IGameClient?       admin,
                             IGameClient        target,
                             AdminOperationType type,
                             string             reason,
                             bool               silent = false)
        => RemoveCore(admin, target, target.SteamId, target.Name, target.Slot, type, reason, silent);

    public void RemoveOffline(IGameClient?       admin,
                              SteamID            steamId,
                              string             targetName,
                              AdminOperationType type,
                              string             reason)
        => RemoveCore(admin, null, steamId, targetName, null, type, reason, true);

    public void NotifySilenceApplied(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason)
    {
        var formattedDuration = FormatDuration(duration);

        Notify(admin,
               target,
               "Admin.SilenceApplied",
               $"silenced {target.Name} {formattedDuration.ToString()}",
               formattedDuration,
               reason);
    }

    public void NotifySilenceRemoved(IGameClient? admin, IGameClient target, string reason)
        => Notify(admin,
                  target,
                  "Admin.SilenceRemoved",
                  $"unsilenced {target.Name}",
                  FormatDuration(null),
                  reason);

#region Core

    private void ApplyCore(IGameClient?       admin,
                           IGameClient?       target,
                           SteamID            targetId,
                           string             targetName,
                           PlayerSlot?        slot,
                           AdminOperationType type,
                           TimeSpan?          duration,
                           string             reason,
                           bool               silent)
    {
        var record = CreateRecord(targetId, type, admin?.SteamId, duration, reason);

        if (slot.HasValue)
        {
            UpdateCache(targetId, slot.Value, type, true, record.ExpiresAt);
        }

        if (!silent && target is not null)
        {
            var formattedDuration = FormatDuration(duration);
            var durationText      = formattedDuration.ToString();

            var (key, fallback) = type switch
            {
                AdminOperationType.Mute => ("Admin.MuteApplied", $"muted {target.Name} {durationText}"),
                AdminOperationType.Gag  => ("Admin.GagApplied", $"gagged {target.Name} {durationText}"),
                _                       => ("Admin.BanApplied", $"banned {target.Name} {durationText}"),
            };

            Notify(admin, target, key, fallback, formattedDuration, reason);
        }

        if (type == AdminOperationType.Ban)
        {
            _cache.SetBanned(targetId, true, record.ExpiresAt);

            if (target is not null)
            {
                _bridge.ClientManager.KickClient(target, reason, NetworkDisconnectionReason.SteamBanned);
            }
        }

        LogOperation(admin, targetName, targetId, type, duration, reason, "Applied");
        _ = _operations.AddAsync(record);
    }

    private void RemoveCore(IGameClient?       admin,
                            IGameClient?       target,
                            SteamID            targetId,
                            string             targetName,
                            PlayerSlot?        slot,
                            AdminOperationType type,
                            string             reason,
                            bool               silent)
    {
        if (slot.HasValue)
        {
            UpdateCache(targetId, slot.Value, type, false, null);
        }

        if (type == AdminOperationType.Ban)
        {
            _cache.SetBanned(targetId, false);
        }

        if (!silent && target is not null)
        {
            var (key, fallback) = type switch
            {
                AdminOperationType.Mute => ("Admin.MuteRemoved", "unmuted"),
                AdminOperationType.Gag  => ("Admin.GagRemoved", "ungagged"),
                _                       => ("Admin.BanRemoved", "unbanned"),
            };

            Notify(admin, target, key, $"{fallback} {target.Name}", FormatDuration(null), reason);
        }

        LogOperation(admin, targetName, targetId, type, null, reason, "Removed");
        _ = _operations.RemoveAsync(targetId, type);
    }

    private void UpdateCache(SteamID steamId, PlayerSlot slot, AdminOperationType type, bool active, DateTime? expiresAt)
    {
        switch (type)
        {
            case AdminOperationType.Mute:
                _cache.SetMuted(slot, active, expiresAt);

                break;
            case AdminOperationType.Gag:
                _cache.SetGagged(slot, active, expiresAt);

                break;
            case AdminOperationType.Ban:
                _cache.SetBanned(steamId, active, expiresAt);

                break;
        }
    }

    private static AdminOperationRecord CreateRecord(SteamID            targetId,
                                                     AdminOperationType type,
                                                     SteamID?           adminId,
                                                     TimeSpan?          duration,
                                                     string             reason)
    {
        var now       = DateTime.UtcNow;
        var expiresAt = duration.HasValue ? now.Add(duration.Value) : (DateTime?) null;

        return new AdminOperationRecord(targetId, type, adminId, now, expiresAt, reason);
    }

    private Task TryKickOnlineTargetAsync(SteamID steamId, string reason)
        => _bridge.ModSharp.InvokeFrameActionAsync(() =>
        {
            if (_bridge.ClientManager.GetGameClient(steamId) is { } onlineClient)
            {
                _bridge.ClientManager.KickClient(onlineClient, reason, NetworkDisconnectionReason.SteamBanned);
            }
        });

    private void Notify(IGameClient?      admin,
                        IGameClient?      target,
                        string            locKey,
                        string            fallback,
                        LocalizedDuration duration,
                        string            reason)
    {
        _bridge.ModSharp.InvokeFrameAction(() =>
        {
            var adminName = admin?.Name ?? "Console";

            if (target is not null && _moduleContext.LocalizerManager is { } localizer)
            {
                var locale = localizer.ForMany(_bridge.ClientManager.GetGameClients(true));
                locale.Localized(locKey, adminName, target.Name, duration, reason).Print();
            }
            else
            {
                var message = $"[MS] {adminName} {fallback}. Reason: {reason}";
                _bridge.ModSharp.PrintToChatAll(message);
            }
        });
    }

    private LocalizedDuration FormatDuration(TimeSpan? duration)
        => new (duration, _moduleContext.LocalizerManager);

    private void LogOperation(IGameClient?       admin,
                              string             targetName,
                              SteamID            targetId,
                              AdminOperationType type,
                              TimeSpan?          duration,
                              string             reason,
                              string             action)
    {
        _logger.LogInformation("{Action} {Type}: {Admin} -> {Target} ({SteamId}). Duration: {Duration}. Reason: {Reason}",
                               action,
                               type,
                               admin?.Name ?? "Console",
                               targetName,
                               targetId,
                               duration?.ToString() ?? "Permanent",
                               reason);
    }

    private async Task LoadAndApplyOperationsAsync(SteamID steamId)
    {
        try
        {
            var operations = await _operations.GetAllAsync(steamId).ConfigureAwait(false);

            var hasBan       = false;
            var hasActiveOps = false;

            foreach (var operation in operations)
            {
                if (operation.IsExpired)
                {
                    continue;
                }

                hasActiveOps = true;

                if (operation.Type == AdminOperationType.Ban)
                {
                    hasBan = true;
                    break;
                }
            }

            if (hasBan)
            {
                await TryKickOnlineTargetAsync(steamId, "Banned").ConfigureAwait(false);

                return;
            }

            if (!hasActiveOps)
            {
                return;
            }

            await _bridge.ModSharp.InvokeFrameActionAsync(() =>
                         {
                             if (_bridge.ClientManager.GetGameClient(steamId) is { } current)
                             {
                                 _cache.SetState(current.Slot, steamId, operations);
                             }
                         })
                         .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load operations for {SteamId}", steamId);
        }
    }

#endregion

#region IClientListener

    public void OnClientPutInServer(IGameClient client)
        => _cache.EnsureSlot(client.Slot);

    public void OnClientConnected(IGameClient client)
        => _cache.EnsureSlot(client.Slot);

    public void OnClientPostAdminCheck(IGameClient client)
        => _ = LoadAndApplyOperationsAsync(client.SteamId);

    public void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
        => _cache.Clear(client.Slot);

#endregion
}
