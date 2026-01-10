using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminOperations.Services;

internal interface IBanService : IAdminService;

internal class BanService : IBanService
{
    private readonly CancellationTokenSource _token;
    private readonly InterfaceBridge _bridge;
    private readonly IAdminOperationStorageService _operationStorageService;
    private readonly ILogger<BanService> _logger;

    public BanService(InterfaceBridge bridge, IAdminOperationStorageService operationStorageService, ILogger<BanService> logger)
    {
        _token = new CancellationTokenSource();
        _bridge = bridge;
        _operationStorageService = operationStorageService;
        _logger = logger;

        Task.Run(WatchBanOpsAsync);
    }

    public void OnAllModulesLoaded()
    {
        var ident = _bridge.ModuleIdentity;
        if (_bridge.SharpModuleManager.GetRequiredSharpModuleInterface<IAdminManager>(IAdminManager.Identity).Instance
            is not { } instance)
        {
            return;
        }

        var registry = instance.GetCommandRegistry(ident);
        registry.RegisterAdminCommand("ms_ban", OnCommandBan, ["admin:ban"]);
        registry.RegisterAdminCommand("ms_unban", OnCommandUnban, ["admin:unban"]);
        
    }

    public void OnShutdown()
    {
        _token.Cancel();
    }

    private void OnCommandBan(IGameClient? client, StringCommand args)
    {
        // 我随便编的一个target steamID，这里只是为了演示
        var targetClient = new SteamID();
        var endTime = 0;
        var reason = string.Empty;
        var activeBanOps = _operationStorageService.GetClientActiveOperations(targetClient)
            .Where(x => x.Type is EOperationType.Ban);
        // 两种情况：
        // - 这个人已经被ban了
        // - 这个人还没被ban
        // 没被ban过，那事情很好说，直接加上去就完事。
        // 重点在被ban过，这里就有两个选择，是更新数据，还是说不让操作？这里就是各家自己的权衡了。
        // 基础版的话我是更推荐于直接不让操作
        if (!activeBanOps.Any())
        {
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            var op = new AdminOperation
            {
                SteamId = targetClient,
                Type = EOperationType.Ban,
                StartTime = now,
                EndTime = endTime,
                OperatorSteamId = client?.SteamId ?? 0,
                OperateTime = now,
                Reason = reason
            };
            _operationStorageService.AddOperation(op);
            _logger.LogInformation("__steam_id has been banned by __admin_id at __op_time, starts from __start_time to __end_time, reason: __reason");
            return;
        }

        // GameClient = PrintToChat, Server = ServerConsole
        Console.WriteLine("Client __target has been banned.");
    }

    private void OnCommandUnban(IGameClient? client, StringCommand args)
    {
        // Unban我就不演示了吧，一模一样的操作，这里我就不写了。
    }

    private async Task WatchBanOpsAsync()
    {
        while (_token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            var activeBans = _operationStorageService.GetActiveOperations().Where(x => x.Type is EOperationType.Ban);
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            foreach (var op in activeBans)
            {
                if (op.EndTime > now)
                {
                    continue;
                }

                op.RemovedBy = 0; // system
                op.RemoveTime = now;
                op.RemoveReason = "system removed";
            }
        }
    }
}