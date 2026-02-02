using Sharp.Modules.AdminManager.Shared;

namespace Sharp.Modules.AdminCommands;

internal interface ICommandCategory
{
    void Register(IAdminCommandRegistry registry);

    void Unregister()
    {
    }
}
