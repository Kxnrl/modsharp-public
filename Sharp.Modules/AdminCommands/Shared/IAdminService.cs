namespace Sharp.Modules.AdminCommands.Shared;

/// <summary>
///     Aggregated admin operation services (ban/mute/gag/silence) exposed to consumers.
///     If your ban/mute records live in SQL or another backend, implement <see cref="IAdminOperationStorageService"/>.
/// </summary>
public interface IAdminService
{
    public const string Identity = nameof(IAdminService);

    IBanService     Ban     { get; }
    IMuteService    Mute    { get; }
    IGagService     Gag     { get; }
    ISilenceService Silence { get; }
}
