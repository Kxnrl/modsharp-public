namespace Sharp.Modules.AdminCommands.Services.Handlers;

internal interface IAdminOperationHookRegistrar
{
    void RegisterHooks();
    void UnregisterHooks();
}
