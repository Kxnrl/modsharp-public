using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services.Handlers;

internal class BanHandler : IAdminOperationHandler, IAdminOperationHookRegistrar
{
    private readonly Dictionary<SteamID, BanEntry> _bans   = new ();
    private readonly Dictionary<string, DateTime?> _ipBans = new ();

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
        var metadata = record.GetMetadata<BanMetadata>();
        var type     = (BanType?) metadata?.Bantype ?? BanType.SteamId;
        var ip       = metadata?.Ip;

        SetBanned(record.SteamId, true, type, ip, record.ExpiresAt);

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

        // todo: use @params.Ip
        if (!IsBanned(steamId, @params.Ip))
        {
            return new HookReturnValue<NetworkDisconnectionReason>();
        }

        return new HookReturnValue<NetworkDisconnectionReason>(EHookAction.SkipCallReturnOverride,
                                                               NetworkDisconnectionReason.SteamBanned);
    }

    private bool IsBanned(SteamID steamId, string? ip)
    {
        if (!string.IsNullOrWhiteSpace(ip) && _ipBans.TryGetValue(ip, out var expiresAt))
        {
            if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
            {
                _ipBans.Remove(ip);
            }
            else
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(ip))
        {
            var subnet = GetSubnet(ip);

            if (_ipBans.TryGetValue(subnet, out expiresAt))
            {
                if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
                {
                    _ipBans.Remove(subnet);
                }
                else
                {
                    return true;
                }
            }
        }

        if (!_bans.TryGetValue(steamId, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
        {
            SetBanned(steamId, false); // cleanup both cache

            return false;
        }

        return true;
    }

    private void SetBanned(SteamID   steamId,
                           bool      banned,
                           BanType   type      = BanType.SteamId,
                           string?   ip        = null,
                           DateTime? expiresAt = null)
    {
        if (banned)
        {
            _bans[steamId] = new BanEntry(expiresAt, type, ip);

            if (!string.IsNullOrWhiteSpace(ip))
            {
                if (type == BanType.Ip)
                {
                    _ipBans[ip] = expiresAt;
                }

                else if (type == BanType.IpRange)
                {
                    // Subnet ban: "1.1.1."
                    _ipBans[GetSubnet(ip)] = expiresAt;
                }
            }
        }
        else
        {
            if (_bans.Remove(steamId, out var entry))
            {
                if (entry.Type == BanType.Ip && !string.IsNullOrWhiteSpace(entry.Ip))
                {
                    _ipBans.Remove(entry.Ip);
                }
                else if (entry.Type == BanType.IpRange && !string.IsNullOrWhiteSpace(entry.Ip))
                {
                    _ipBans.Remove(GetSubnet(entry.Ip));
                }
            }
        }
    }

    private static string GetSubnet(string ip)
    {
        var lastDotIndex = ip.LastIndexOf('.');

        if (lastDotIndex == -1)
        {
            return ip;
        }

        return ip.Substring(0, lastDotIndex + 1);
    }

    private record struct BanEntry(DateTime? ExpiresAt, BanType Type, string? Ip);

    private class BanMetadata
    {
        [JsonPropertyName("ip")]
        public string? Ip { get; set; }

        [JsonPropertyName("bantype")]
        public int Bantype { get; set; }
    }

    public int ListenerVersion  => IClientListener.ApiVersion;
    public int ListenerPriority => 0;
}
