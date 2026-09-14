/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Common;
using Sharp.Modules.AdminCommands.Services.Handlers;
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

    private static readonly TimeSpan CacheRefreshInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan CacheGraceWindow = CacheRefreshInterval * 2;
    private static TimeSpan NextRefreshDelay => CacheRefreshInterval + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 10_000));

    private CancellationTokenSource? _refreshCts;

    private readonly ILogger<AdminOperationEngine> _logger;
    private readonly InterfaceBridge               _bridge;
    private readonly AdminOperationService         _operations;
    private readonly ModuleContext                 _moduleContext;

    private readonly Dictionary<AdminOperationType, (IAdminOperationHandler Handler, string ModuleIdentity)> _handlers;

    /// <summary>
    ///     Backup of core handlers displaced by external handler registrations.
    ///     Keyed by <see cref="AdminOperationType" />. Restored when the external module unregisters.
    /// </summary>
    private readonly Dictionary<AdminOperationType, (IAdminOperationHandler Handler, string ModuleIdentity)> _coreHandlers;

    public AdminOperationEngine(ILogger<AdminOperationEngine> logger,
        InterfaceBridge                                       bridge,
        AdminOperationService                                 operations,
        ModuleContext                                         moduleContext,
        IEnumerable<IAdminOperationHandler>                   handlers)
    {
        _logger        = logger;
        _bridge        = bridge;
        _operations    = operations;
        _moduleContext = moduleContext;

        _coreHandlers = [];

        _handlers = handlers.ToDictionary(h => h.Type,
                                          h => (h, AdminCommands.AssemblyName));
    }

    /// <summary>
    ///     Returns the currently registered handler for the given operation type, or <c>null</c> if none is registered.
    /// </summary>
    public IAdminOperationHandler? GetHandler(AdminOperationType type)
        => _handlers.TryGetValue(type, out var entry) ? entry.Handler : null;

    public void RegisterHandler(string moduleIdentity, IAdminOperationHandler handler)
    {
        if (_handlers.TryGetValue(handler.Type, out var existing))
        {
            // Save the displaced handler for restoration when this module unregisters.
            if (existing.ModuleIdentity == AdminCommands.AssemblyName)
            {
                if (!_coreHandlers.TryAdd(handler.Type, existing))
                {
                    _logger.LogWarning("Core handler for {Type} was already backed up (possible duplicate registration). "
                                       + "Skipping to preserve the original core handler.",
                                       handler.Type);
                }
            }

            // Unregister hooks of the old handler before replacing it.
            if (existing.Handler is IAdminOperationHookRegistrar oldRegistrar)
            {
                oldRegistrar.UnregisterHooks();
            }

            _logger.LogWarning("Operation handler for {Type} is being replaced: {OldModule} -> {NewModule}",
                               handler.Type,
                               existing.ModuleIdentity,
                               moduleIdentity);
        }

        _handlers[handler.Type] = (handler, moduleIdentity);

        _logger.LogDebug("Registered admin operation handler for {Type} (Module: {Module})", handler.Type, moduleIdentity);
    }

    public void UnregisterHandlers(string moduleIdentity)
    {
        var toRemove = _handlers.Where(x => x.Value.ModuleIdentity == moduleIdentity)
                                .Select(x => x.Key)
                                .ToArray();

        foreach (var type in toRemove)
        {
            // Unregister hooks of the handler being removed before restoring core.
            if (_handlers.TryGetValue(type, out var removed)
                && removed.Handler is IAdminOperationHookRegistrar removedRegistrar)
            {
                removedRegistrar.UnregisterHooks();
            }

            _handlers.Remove(type);

            // Restore the core handler that was displaced, if any.
            if (_coreHandlers.Remove(type, out var coreHandler))
            {
                _handlers[type] = coreHandler;

                if (coreHandler.Handler is IAdminOperationHookRegistrar coreRegistrar)
                {
                    coreRegistrar.RegisterHooks();
                }

                _logger.LogDebug("Restored core handler for {Type} after {Module} unregistered",
                                 type,
                                 moduleIdentity);
            }
            else
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

        _refreshCts = new CancellationTokenSource();
        _           = RefreshCacheLoopAsync(_refreshCts.Token);
    }

    public void Shutdown()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;

        _bridge.ClientManager.RemoveClientListener(this);
    }

    public void ApplyOnline(IGameClient? admin,
        IGameClient                      target,
        AdminOperationType               type,
        TimeSpan?                        duration,
        string                           reason,
        bool                             silent   = false,
        string?                          metadata = null)
    {
        var targetId   = target.SteamId;
        var targetName = target.Name;

        // 如果涉及踢出, 最稳妥期间就是FrameAction
        _bridge.ModSharp.InvokeFrameAction(() =>
        {
            if (target.IsValid)
            {
                ApplyCore(admin is { IsValid: true } ? admin : null,
                          target,
                          target.SteamId,
                          target.Name,
                          target.Slot,
                          type,
                          duration,
                          reason,
                          metadata,
                          silent);

                return;
            }

            ApplyCore(admin is { IsValid: true } ? admin : null,
                      null,
                      targetId,
                      targetName,
                      null,
                      type,
                      duration,
                      reason,
                      metadata,
                      true);
        });
    }

    public void ApplyOffline(IGameClient? admin,
        SteamID                           steamId,
        string                            targetName,
        AdminOperationType                type,
        TimeSpan?                         duration,
        string                            reason,
        string?                           metadata = null)
        => _bridge.ModSharp.InvokeFrameAction(() =>
        {
            ApplyCore(admin is { IsValid: true } ? admin : null,
                      null,
                      steamId,
                      targetName,
                      null,
                      type,
                      duration,
                      reason,
                      metadata,
                      true);
        });

    public void RemoveOnline(IGameClient? admin,
        IGameClient                       target,
        AdminOperationType                type,
        string                            reason,
        bool                              silent = false)
    {
        var targetId   = target.SteamId;
        var targetName = target.Name;

        // 如果涉及踢出, 最稳妥期间就是FrameAction
        _bridge.ModSharp.InvokeFrameAction(() =>
        {
            if (target.IsValid)
            {
                RemoveCore(admin is { IsValid: true } ? admin : null,
                           target,
                           target.SteamId,
                           target.Name,
                           target.Slot,
                           type,
                           reason,
                           silent);

                return;
            }

            RemoveCore(admin is { IsValid: true } ? admin : null,
                       null,
                       targetId,
                       targetName,
                       null,
                       type,
                       reason,
                       true);
        });
    }

    public void RemoveOffline(IGameClient? admin,
        SteamID                            steamId,
        string                             targetName,
        AdminOperationType                 type,
        string                             reason)
        => _bridge.ModSharp.InvokeFrameAction(() =>
        {
            RemoveCore(admin is { IsValid: true } ? admin : null,
                       null,
                       steamId,
                       targetName,
                       null,
                       type,
                       reason,
                       true);
        });

    public void NotifySilenceApplied(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason)
    {
        var adminName         = ResolveAdminName(admin);
        var formattedDuration = FormatDuration(duration);

        Notify(adminName,
               target,
               "Admin.SilenceApplied",
               $"silenced {target.Name} {formattedDuration}",
               formattedDuration,
               reason);
    }

    public void NotifySilenceRemoved(IGameClient? admin, IGameClient target, string reason)
        => Notify(ResolveAdminName(admin),
                  target,
                  "Admin.SilenceRemoved",
                  $"unsilenced {target.Name}",
                  FormatDuration(null),
                  reason);

#region Core

    private void ApplyCore(IGameClient? admin,
        IGameClient?                    target,
        SteamID                         targetId,
        string                          targetName,
        PlayerSlot?                     slot,
        AdminOperationType              type,
        TimeSpan?                       duration,
        string                          reason,
        string?                         metadata,
        bool                            silent)
    {
        var adminName    = ResolveAdminName(admin);
        var adminDisplay = ResolveAdminDisplay(admin);

        if (!_handlers.TryGetValue(type, out var entry))
        {
            _logger.LogWarning("Operation '{type}' does not exist in the handler.", type.Value);

            admin?.GetPlayerController()?.Print(HudPrintChannel.Chat, $"[MS] Operation {type.Value} does not exist.");

            return;
        }

        var record = CreateRecord(targetId, type, admin?.SteamId, duration, reason, metadata);

        entry.Handler.OnApplied(record, target);

        if (!silent && target is not null)
        {
            var formattedDuration = FormatDuration(duration);
            var durationText      = formattedDuration.ToString();

            var (key, fallback) = entry.Handler.GetAppliedNotification(target, durationText);

            Notify(adminName, target, key, fallback, formattedDuration, reason);
        }

        LogOperation(adminDisplay, targetName, targetId, type, duration, reason, "Applied");
        _ = _operations.AddAsync(record);
    }

    private void RemoveCore(IGameClient? admin,
        IGameClient?                     target,
        SteamID                          targetId,
        string                           targetName,
        PlayerSlot?                      slot,
        AdminOperationType               type,
        string                           reason,
        bool                             silent)
    {
        var adminName    = ResolveAdminName(admin);
        var adminDisplay = ResolveAdminDisplay(admin);

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

            Notify(adminName, target, key, fallback, FormatDuration(null), reason);
        }

        LogOperation(adminDisplay, targetName, targetId, type, null, reason, "Removed");
        _ = _operations.RemoveAsync(targetId, type, admin?.SteamId, reason);
    }

    private static AdminOperationRecord CreateRecord(SteamID targetId,
        AdminOperationType                                   type,
        SteamID?                                             adminId,
        TimeSpan?                                            duration,
        string                                               reason,
        string?                                              metadata = null)
    {
        var now       = DateTime.UtcNow;
        var expiresAt = duration.HasValue ? now.Add(duration.Value) : (DateTime?) null;

        return new AdminOperationRecord(targetId, type, adminId, now, expiresAt, reason, metadata);
    }

    private void Notify(string adminName,
        IGameClient?           target,
        string                 locKey,
        string                 fallback,
        LocalizedDuration      duration,
        string                 reason)
    {
        var targetName = target?.Name;

        _bridge.ModSharp.InvokeFrameAction(() =>
        {
            if (targetName is not null && _moduleContext.LocalizerManager is { } localizer)
            {
                var locale = localizer.ForMany(_bridge.ClientManager.GetGameClients(true));
                locale.Localized(locKey, adminName, targetName, duration, reason).Print();
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

    private void LogOperation(string adminDisplay,
        string                       targetName,
        SteamID                      targetId,
        AdminOperationType           type,
        TimeSpan?                    duration,
        string                       reason,
        string                       action)
    {
        _logger.LogInformation("{Action} {Type}: {Admin} -> {Target} ({SteamId}). Duration: {Duration}. Reason: {Reason}",
                               action,
                               type,
                               adminDisplay,
                               targetName,
                               targetId,
                               duration?.ToString() ?? "Permanent",
                               reason);
    }

    private static string ResolveAdminName(IGameClient? admin)
    {
        if (admin is null)
        {
            return "Console";
        }

        return string.IsNullOrWhiteSpace(admin.Name) ? "Unknown" : admin.Name;
    }

    private static string ResolveAdminDisplay(IGameClient? admin)
    {
        if (admin is null)
        {
            return "Console";
        }

        var adminName = ResolveAdminName(admin);

        return $"{adminName} ({admin.SteamId})";
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

    private async Task RefreshCacheLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(NextRefreshDelay, token).ConfigureAwait(false);
                await RefreshCachedOperationsAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh cached admin operations");
            }
        }
    }

    private async Task RefreshCachedOperationsAsync(CancellationToken token)
    {
        var snapshot = await _bridge.ModSharp
                                    .InvokeFrameActionAsync(() => _handlers.Values
                                                                           .Select(x => (x.Handler,
                                                                                       Identities: x.Handler
                                                                                           .GetCachedIdentities(
                                                                                               CacheGraceWindow)))
                                                                           .Where(x => x.Identities.Count > 0)
                                                                           .ToArray(),
                                                            token)
                                    .ConfigureAwait(false);

        var stale = new List<(IAdminOperationHandler Handler, SteamID SteamId)>();

        foreach (var (handler, identities) in snapshot)
        {
            foreach (var steamId in identities)
            {
                if (await _operations.TryHasActiveAsync(steamId, handler.Type, token).ConfigureAwait(false) == false)
                {
                    stale.Add((handler, steamId));
                }
            }
        }

        if (stale.Count == 0)
        {
            return;
        }

        await _bridge.ModSharp.InvokeFrameActionAsync(() =>
                                                      {
                                                          var stillCached
                                                              = new Dictionary<AdminOperationType, IReadOnlySet<SteamID>>();

                                                          foreach (var (handler, steamId) in stale)
                                                          {
                                                              if (!_handlers.TryGetValue(handler.Type, out var current)
                                                                  || !ReferenceEquals(current.Handler, handler))
                                                              {
                                                                  continue;
                                                              }

                                                              if (!stillCached.TryGetValue(handler.Type, out var eligible))
                                                              {
                                                                  eligible = handler.GetCachedIdentities(CacheGraceWindow);
                                                                  stillCached[handler.Type] = eligible;
                                                              }

                                                              if (!eligible.Contains(steamId))
                                                              {
                                                                  continue;
                                                              }

                                                              try
                                                              {
                                                                  handler.OnRemoved(steamId,
                                                                                    _bridge.ClientManager
                                                                                        .GetGameClient(steamId));

                                                                  _logger.LogInformation(
                                                                      "Evicted cached {Type} for {SteamId}: no active record in storage",
                                                                      handler.Type,
                                                                      steamId);
                                                              }
                                                              catch (Exception ex)
                                                              {
                                                                  _logger.LogError(ex,
                                                                                   "Failed to evict cached {Type} for {SteamId}",
                                                                                   handler.Type,
                                                                                   steamId);
                                                              }
                                                          }
                                                      },
                                                      token)
                     .ConfigureAwait(false);
    }

#endregion

#region IClientListener

    public void OnClientPostAdminCheck(IGameClient client)
        => _ = LoadAndApplyOperationsAsync(client.SteamId);

#endregion
}
