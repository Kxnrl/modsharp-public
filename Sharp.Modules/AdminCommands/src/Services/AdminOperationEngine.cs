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
///     Provides shared behaviors: storage persistence, notifications, and kick.
/// </summary>
internal class AdminOperationEngine : IClientListener
{
    public int ListenerVersion  => IClientListener.ApiVersion;
    public int ListenerPriority => 0;

    private readonly InterfaceBridge               _bridge;
    private readonly AdminOperationService         _operations;
    private readonly ModuleContext                 _moduleContext;
    private readonly ILogger<AdminOperationEngine> _logger;

    private readonly Dictionary<AdminOperationType, (IAdminOperationHandler Handler, string ModuleIdentity)> _handlers;

    public AdminOperationEngine(
        InterfaceBridge                     bridge,
        AdminOperationService               operations,
        ModuleContext                       moduleContext,
        IEnumerable<IAdminOperationHandler> handlers)
    {
        _bridge        = bridge;
        _operations    = operations;
        _moduleContext = moduleContext;
        _logger        = bridge.LoggerFactory.CreateLogger<AdminOperationEngine>();

        _handlers = handlers.ToDictionary(h => h.Type,
                                          h => (h, AdminCommands.AssemblyName));
    }

    public void RegisterHandler(string moduleIdentity, IAdminOperationHandler handler)
    {
        if (!_handlers.TryAdd(handler.Type, (handler, moduleIdentity)))
        {
            _logger.LogWarning("Failed to register handler for {Type} from {Module}: Handler already registered.",
                               handler.Type,
                               moduleIdentity);

            return;
        }

        _logger.LogDebug("Registered admin operation handler for {Type} (Module: {Module})", handler.Type, moduleIdentity);
    }

    public void UnregisterHandlers(string moduleIdentity)
    {
        var toRemove = _handlers.Where(x => x.Value.ModuleIdentity == moduleIdentity)
                                .Select(x => x.Key)
                                .ToArray();

        foreach (var type in toRemove)
        {
            if (_handlers.Remove(type))
            {
                _logger.LogDebug("Unregistered admin operation handler for {Type} (Module: {Module})",
                                 type,
                                 moduleIdentity);
            }
        }
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
        if (!_handlers.TryGetValue(type, out var entry))
        {
            _logger.LogWarning("Operation '{type}' does not exist in the handler.", type.Value);

            admin?.GetPlayerController()?.Print(HudPrintChannel.Chat, $"[MS] Operation {type.Value} does not exist.");

            return;
        }

        var record = CreateRecord(targetId, type, admin?.SteamId, duration, reason);

        entry.Handler.OnApplied(record, target);

        if (!silent && target is not null)
        {
            var formattedDuration = FormatDuration(duration);
            var durationText      = formattedDuration.ToString();

            var (key, fallback) = entry.Handler.GetAppliedNotification(target, durationText);

            Notify(admin, target, key, fallback, formattedDuration, reason);
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
        if (!_handlers.TryGetValue(type, out var entry))
        {
            _logger.LogWarning("Operation '{type}' does not exist in the handler.", type.Value);

            admin?.GetPlayerController()?.Print(HudPrintChannel.Chat, $"[MS] Operation {type.Value} does not exist.");

            return;
        }

        entry.Handler.OnRemoved(targetId, target);

        if (!silent && target is not null)
        {
            var (key, fallback) = entry.Handler.GetRemovedNotification(target);

            Notify(admin, target, key, fallback, FormatDuration(null), reason);
        }

        LogOperation(admin, targetName, targetId, type, null, reason, "Removed");
        _ = _operations.RemoveAsync(targetId, type);
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

            if (operations.Count == 0)
            {
                return;
            }

            await _bridge.ModSharp.InvokeFrameActionAsync(() =>
                         {
                             if (_bridge.ClientManager.GetGameClient(steamId) is not { } target)
                             {
                                 return;
                             }

                             foreach (var operation in operations)
                             {
                                 if (operation.IsExpired)
                                 {
                                     continue;
                                 }

                                 if (_handlers.TryGetValue(operation.Type, out var entry))
                                 {
                                     entry.Handler.OnApplied(operation, target);
                                 }

                                 if (operation.Type == AdminOperationType.Ban)
                                 {
                                     break;
                                 }
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

    public void OnClientPostAdminCheck(IGameClient client)
        => _ = LoadAndApplyOperationsAsync(client.SteamId);

#endregion
}
