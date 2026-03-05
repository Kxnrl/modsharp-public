using System.Buffers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Storage;

internal class JsonAdminOperationStorage : IAdminOperationStorageService, IDisposable
{
    private readonly string                              _filePath;
    private readonly ILogger<JsonAdminOperationStorage>? _logger;

    // using SteamID + Punishment for quick lookup
    private Dictionary<(SteamID, AdminOperationType), AdminOperationRecord> _records = new ();

    private readonly SemaphoreSlim _lock = new (1, 1);

    private static readonly JsonSerializerOptions SerializerOptions = new () { WriteIndented = true };

    public JsonAdminOperationStorage(string sharpPath, ILogger<JsonAdminOperationStorage>? logger = null)
    {
        _filePath = Path.Combine(sharpPath, "data", "punishments.json");
        _logger   = logger;
        Load();
    }

    public async Task<AdminOperationRecord?> GetAsync(SteamID steamId, AdminOperationType type)
    {
        await _lock.WaitAsync();

        try
        {
            if (_records.TryGetValue((steamId, type), out var record) && !record.IsExpired)
            {
                return record;
            }

            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> HasActiveAsync(SteamID steamId, AdminOperationType type)
    {
        await _lock.WaitAsync();

        try
        {
            return _records.TryGetValue((steamId, type), out var r) && !r.IsExpired;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<AdminOperationRecord>> GetAllAsync(SteamID steamId)
    {
        var list                 = new List<AdminOperationRecord>();
        var shouldPersistCleanup = false;

        await _lock.WaitAsync();

        try
        {
            var expiredKeys  = ArrayPool<(SteamID, AdminOperationType)>.Shared.Rent(_records.Count);
            var expiredCount = 0;

            try
            {
                foreach (var kvp in _records)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expiredKeys[expiredCount++] = kvp.Key;
                    }
                    else if (kvp.Value.SteamId == steamId)
                    {
                        list.Add(kvp.Value);
                    }
                }

                if (expiredCount > 0)
                {
                    for (var i = 0; i < expiredCount; i++)
                    {
                        _records.Remove(expiredKeys[i]);
                    }

                    shouldPersistCleanup = true;
                }
            }
            finally
            {
                ArrayPool<(SteamID, AdminOperationType)>.Shared.Return(expiredKeys, true);
            }
        }
        finally
        {
            _lock.Release();
        }

        if (shouldPersistCleanup)
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save punishments while cleaning expired records.");
            }
        }

        return list;
    }

    public async Task AddAsync(AdminOperationRecord record)
    {
        await _lock.WaitAsync();

        try
        {
            _records[(record.SteamId, record.Type)] = record;
        }
        finally
        {
            _lock.Release();
        }

        try
        {
            await SaveAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save punishments to disk");
        }
    }

    public async Task RemoveAsync(SteamID targetId, AdminOperationType type, SteamID? removedBy, string? reason)
    {
        bool removed;
        await _lock.WaitAsync();

        try
        {
            removed = _records.Remove((targetId, type));
        }
        finally
        {
            _lock.Release();
        }

        if (removed)
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save punishments after removing record for {SteamId}", targetId);
            }
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<AdminOperationRecord>>(json) ?? [];

            _records = new Dictionary<(SteamID, AdminOperationType), AdminOperationRecord>();

            foreach (var record in list)
            {
                _records[(record.SteamId, record.Type)] = record;
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "Punishment storage file is corrupt. Moving it aside and starting fresh.");

            try
            {
                File.Move(_filePath, $"{_filePath}.corrupt", true);
            }
            catch (Exception moveEx)
            {
                _logger?.LogError(moveEx, "Failed to move corrupt punishment storage file.");
            }

            _records = new ();
        }
    }

    private async Task SaveAsync()
    {
        AdminOperationRecord[]? snapshotArray = null;
        var                     count         = 0;

        await _lock.WaitAsync();

        try
        {
            // Rent array to avoid List allocation
            snapshotArray = ArrayPool<AdminOperationRecord>.Shared.Rent(_records.Count);

            foreach (var r in _records.Values)
            {
                if (!r.IsExpired)
                {
                    snapshotArray[count++] = r;
                }
            }
        }
        finally
        {
            _lock.Release();
        }

        try
        {
            var dir = Path.GetDirectoryName(_filePath);

            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tempFile = _filePath + ".tmp";

            await using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // Serialize only the valid segment
                await JsonSerializer.SerializeAsync(stream,
                                                    new ArraySegment<AdminOperationRecord>(snapshotArray, 0, count),
                                                    SerializerOptions);

                await stream.FlushAsync();
            }

            try
            {
                File.Move(tempFile, _filePath, true);
            }
            catch (IOException)
            {
                if (File.Exists(_filePath))
                {
                    File.Replace(tempFile, _filePath, null);
                }
                else
                {
                    File.Move(tempFile, _filePath);
                }
            }
        }
        finally
        {
            if (snapshotArray != null)
            {
                ArrayPool<AdminOperationRecord>.Shared.Return(snapshotArray, true);
            }
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
