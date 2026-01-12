using Sharp.Modules.AdminCommands.Shared;

namespace Sharp.Modules.AdminCommands.Services;

/// <summary>
///     Aggregates feature services for external callers.
/// </summary>
internal sealed class AdminService : IAdminService
{
    public AdminService(IBanService ban, IMuteService mute, IGagService gag, ISilenceService silence)
    {
        Ban     = ban;
        Mute    = mute;
        Gag     = gag;
        Silence = silence;
    }

    public IBanService     Ban     { get; }
    public IMuteService    Mute    { get; }
    public IGagService     Gag     { get; }
    public ISilenceService Silence { get; }
}
