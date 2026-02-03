using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Storage;

/// <summary>
///     Delegates IAdminOperationStorageService calls to a switchable underlying implementation so we can
///     swap in user-provided storage at runtime without rebuilding the container.
/// </summary>
internal sealed class AdminOperationStorage : IAdminOperationStorageService
{
    private readonly ILogger<AdminOperationStorage> _logger;
    private readonly IAdminOperationStorageService  _fallback;
    private          IAdminOperationStorageService  _current;

    public AdminOperationStorage(IAdminOperationStorageService fallback, ILogger<AdminOperationStorage> logger)
    {
        _fallback = fallback;
        _current  = fallback;
        _logger   = logger;
    }

    public IAdminOperationStorageService Current => Volatile.Read(ref _current);

    public void Use(IAdminOperationStorageService storage, string? providerName = null)
    {
        if (ReferenceEquals(Current, storage))
        {
            return;
        }

        Volatile.Write(ref _current, storage);

        if (!ReferenceEquals(storage, _fallback))
        {
            if (!string.IsNullOrWhiteSpace(providerName))
            {
                _logger.LogInformation("Using external admin operation storage from {provider}.", providerName);
            }
            else
            {
                _logger.LogInformation("Using custom admin operation storage instance.");
            }
        }
        else
        {
            _logger.LogInformation("Using built-in JSON admin operation storage.");
        }
    }

    public void UseFallback()
        => Use(_fallback);

    public Task<AdminOperationRecord?> GetAsync(SteamID steamId, AdminOperationType type)
        => Current.GetAsync(steamId, type);

    public Task<IReadOnlyList<AdminOperationRecord>> GetAllAsync(SteamID steamId)
        => Current.GetAllAsync(steamId);

    public Task AddAsync(AdminOperationRecord record)
        => Current.AddAsync(record);

    public Task RemoveAsync(SteamID steamId, AdminOperationType type, SteamID? removedBy, string? reason)
        => Current.RemoveAsync(steamId, type, removedBy, reason);

    public Task<bool> HasActiveAsync(SteamID steamId, AdminOperationType type)
        => Current.HasActiveAsync(steamId, type);
}
