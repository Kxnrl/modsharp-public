using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;

namespace Sharp.Modules.AdminCommands.Services.Internal;

/// <summary>
///     Holds optional cross-module services so they can be shared safely across command handlers.
/// </summary>
internal sealed class ModuleContext
{
    private const int DefaultBroadcastType
        = 1; // 0 = off, 1 = broadcast (ignore immunity), 2 = broadcast with max immunity gate
    private const int DefaultBroadcastMaxImmunity = byte.MaxValue;

    private readonly IConVar? _broadcastTypeConVar;
    private readonly IConVar? _broadcastMaxImmunityConVar;

    public ModuleContext(InterfaceBridge bridge)
    {
        _broadcastTypeConVar = bridge.ConVarManager.CreateConVar("ms_admin_broadcast_type",
                                                                 DefaultBroadcastType,
                                                                 0,
                                                                 2,
                                                                 "Admin reply broadcast mode: 0=off, 1=everyone, 2=respect max immunity.",
                                                                 ConVarFlags.Release);

        _broadcastMaxImmunityConVar = bridge.ConVarManager.CreateConVar("ms_admin_broadcast_max_immunity",
                                                                        DefaultBroadcastMaxImmunity,
                                                                        0,
                                                                        (int) byte.MaxValue,
                                                                        "When ms_admin_broadcast_type is 2, admins with immunity at or below this value broadcast; higher immunity stays private.",
                                                                        ConVarFlags.Release);
    }

    public ILocalizerManager? LocalizerManager { get; private set; }
    public ITargetingManager? TargetingManager { get; private set; }
    public IAdminManager?     AdminManager     { get; private set; }

    public int BroadcastType => Math.Clamp(_broadcastTypeConVar?.GetInt32() ?? DefaultBroadcastType, 0, 2);

    public int BroadcastMaxImmunity
        => Math.Clamp(_broadcastMaxImmunityConVar?.GetInt32() ?? DefaultBroadcastMaxImmunity, 0, byte.MaxValue);

    public void UpdateLocalizer(ILocalizerManager? manager)
        => LocalizerManager = manager;

    public void UpdateTargeting(ITargetingManager? manager)
        => TargetingManager = manager;

    public void UpdateAdminManager(IAdminManager? manager)
        => AdminManager = manager;
}
