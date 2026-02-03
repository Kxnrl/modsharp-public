using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services;

/// <summary>
///     Thin wrapper over IAdminOperationStorageService with basic guardrails (skip duplicate active records).
/// </summary>
internal sealed class AdminOperationService
{
    private readonly IAdminOperationStorageService  _storage;
    private readonly ILogger<AdminOperationService> _logger;

    public AdminOperationService(IAdminOperationStorageService storage, InterfaceBridge bridge)
    {
        _storage = storage;
        _logger  = bridge.LoggerFactory.CreateLogger<AdminOperationService>();
    }

    public async Task<IReadOnlyList<AdminOperationRecord>> GetAllAsync(SteamID           steamId,
                                                                       CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _storage.GetAllAsync(steamId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load punishment records for {SteamId}", steamId);

            return Array.Empty<AdminOperationRecord>();
        }
    }

    public async Task<bool> HasActiveAsync(SteamID            steamId,
                                           AdminOperationType type,
                                           CancellationToken  cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _storage.HasActiveAsync(steamId, type).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check active {Type} for {SteamId}", type, steamId);

            return false;
        }
    }

    public async Task<bool> AddAsync(AdminOperationRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (await _storage.HasActiveAsync(record.SteamId, record.Type).ConfigureAwait(false))
            {
                _logger.LogInformation("Skip {Type}: {SteamId} already has active record.", record.Type, record.SteamId);

                return false;
            }

            await _storage.AddAsync(record).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add {Type} punishment for {SteamId}", record.Type, record.SteamId);

            return false;
        }
    }

    public async Task RemoveAsync(SteamID            steamId,
                                  AdminOperationType type,
                                  SteamID?           adminId,
                                  string?            reason,
                                  CancellationToken  cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _storage.RemoveAsync(steamId, type, adminId, reason).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove {Type} punishment for {SteamId}", type, steamId);
        }
    }
}
