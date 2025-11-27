using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.CommandManager.Shared;

public interface ICommandManager
{
    const string Identity = nameof(ICommandManager);

    /// <summary>
    /// Add Command registry. <br/>
    /// </summary>
    /// <param name="moduleIdentity"></param>
    public ICommandRegistry GetRegistry(string moduleIdentity);

}

public interface ICommandRegistry
{
    void RegisterClientCommand(string command, Action<IGameClient, StringCommand> call);

    void RegisterServerCommand(string command, Action<StringCommand> call, string description = "");

    void RegisterServerCommand(string command, Action call, string description = "")
    {
        RegisterServerCommand(command, _ =>
        {
            call();
        }, description);
    }

    void RegisterGenericCommand(string command, Action<IGameClient?, StringCommand> call, string description = "");
}