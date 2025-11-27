using System.Collections.Immutable;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminManager.Shared;

public interface IAdminManager
{
    public const char RolesOperator = '@';
    public const char DenyOperator = '!';
    public const char WildCardOperator = '*';
    public const char SeparatorOperator = ':';

    public const string Identity = nameof(IAdminManager);

    public IAdmin? GetAdmin(SteamID identity);

    public IAdminCommandRegistry GetCommandRegistry(string moduleIdentity);
}

public interface IAdminCommandRegistry
{
    public void RegisterAdminCommand(string command, Action<IGameClient?, StringCommand> call,
        ImmutableArray<string> permissions);
}