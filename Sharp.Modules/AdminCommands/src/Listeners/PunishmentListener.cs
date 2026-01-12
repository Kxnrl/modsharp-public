using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Services.Internal;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Listeners;

internal class PunishmentListener : IClientListener
{
    public int ListenerVersion  => IClientListener.ApiVersion;
    public int ListenerPriority => byte.MaxValue;

    private readonly InterfaceBridge     _bridge;
    private readonly AdminOperationCache _cache;
    private readonly ILogger             _logger;

    public PunishmentListener(InterfaceBridge bridge, AdminOperationCache cache)
    {
        _bridge = bridge;
        _cache  = cache;
        _logger = bridge.LoggerFactory.CreateLogger<PunishmentListener>();
    }

    public void Register()
    {
        _bridge.HookManager.ClientCanHear.InstallHookPre(OnClientCanHearPre);
        _bridge.HookManager.ClientConnect.InstallHookPre(OnClientConnectPre);

        _bridge.ClientManager.InstallClientListener(this);
    }

    public void Unregister()
    {
        _bridge.HookManager.ClientCanHear.RemoveHookPre(OnClientCanHearPre);
        _bridge.HookManager.ClientConnect.RemoveHookPre(OnClientConnectPre);

        _bridge.ClientManager.RemoveClientListener(this);
    }

    private HookReturnValue<bool> OnClientCanHearPre(IClientCanHearHookParams @params, HookReturnValue<bool> ret)
        => _cache.IsMuted(@params.Speaker.Slot)
            ? new HookReturnValue<bool>(EHookAction.SkipCallReturnOverride, false)
            : new HookReturnValue<bool>(EHookAction.Ignored);

    public ECommandAction OnClientSayCommand(IGameClient client,
                                             bool        teamOnly,
                                             bool        isCommand,
                                             string      commandName,
                                             string      message)
        => _cache.IsGagged(client.Slot) ? ECommandAction.Stopped : ECommandAction.Skipped;

    private HookReturnValue<bool> OnClientConnectPre(IClientConnectHookParams @params, HookReturnValue<bool> arg2)
    {
        var steamId = @params.SteamId;

        return _cache.IsBanned(steamId)
            ? new HookReturnValue<bool>(EHookAction.SkipCallReturnOverride, false)
            : new HookReturnValue<bool>(EHookAction.Ignored);
    }
}
