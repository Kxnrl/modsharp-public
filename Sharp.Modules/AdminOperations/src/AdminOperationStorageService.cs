using Sharp.Shared.Units;

namespace Sharp.Modules.AdminOperations;

internal enum EOperationType
{
    Ban,
    Gag,
    Mute
}

internal class AdminOperation
{
    public SteamID SteamId { get; init; }
    public EOperationType Type { get; init; }
    public long StartTime { get; init; }
    public long EndTime { get; init; }
    public SteamID OperatorSteamId { get; init; }
    public long OperateTime { get; init; }
    public string Reason { get; init; } = string.Empty;
    
    public SteamID RemovedBy { get; set; }
    public long RemoveTime { get; set; }
    public string RemoveReason { get; set; } = string.Empty;
}

internal interface IAdminOperationStorageService
{
    IEnumerable<AdminOperation> GetAllOperations();

    IEnumerable<AdminOperation> GetActiveOperations();
    
    IEnumerable<AdminOperation> GetClientOperations(SteamID steamId);
    IEnumerable<AdminOperation> GetClientActiveOperations(SteamID steamId);

    void RemoveOperation(SteamID steamId, SteamID operatorSteamId);

    void UpdateOperation(AdminOperation updatedOp);

    void AddOperation(AdminOperation operation);
}

internal class AdminOperationStorageService : IAdminOperationStorageService
{
    public IEnumerable<AdminOperation> GetAllOperations()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AdminOperation> GetActiveOperations()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AdminOperation> GetClientOperations(SteamID steamId)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AdminOperation> GetClientActiveOperations(SteamID steamId)
    {
        throw new NotImplementedException();
    }

    public void RemoveOperation(SteamID steamId, SteamID operatorSteamId)
    {
        throw new NotImplementedException();
    }

    public void UpdateOperation(AdminOperation updatedOp)
    {
        throw new NotImplementedException();
    }

    public void AddOperation(AdminOperation operation)
    {
        throw new NotImplementedException();
    }
}