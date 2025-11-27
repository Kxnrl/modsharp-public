// ReSharper disable UnusedParameter.Local

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.CommandManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using static Sharp.Shared.Managers.IClientManager;

namespace Sharp.Modules.CommandManager;

internal class CommandManager : IModSharpModule, ICommandManager
{
    private readonly Dictionary<string, ICommandRegistry> _registries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _registerCommands = new(StringComparer.OrdinalIgnoreCase);
    private readonly ISharedSystem _shared;
    private readonly ILogger<CommandManager> _logger;

    public CommandManager(
        ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration coreConfiguration,
        bool hotReload)
    {
        _shared = sharedSystem;
        Path.GetFileName(dllPath);
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<CommandManager>();
    }

    #region IModSharpModule

    public bool Init()
    {
        return true;
    }

    public void PostInit()
    {
        _shared.GetSharpModuleManager()
            .RegisterSharpModuleInterface<ICommandManager>(this, ICommandManager.Identity, this);
    }

    public void OnLibraryDisconnect(string name)
    {
        RemoveRegisteredCommands(name);
        RemoveRegistry(name);
    }

    public void Shutdown()
    {
    }

    string IModSharpModule.DisplayName => "Sharp.Modules.CommandManager";

    string IModSharpModule.DisplayAuthor => "laper32";

    #endregion

    #region ICommandManager

    public ICommandRegistry GetRegistry(string moduleIdentity)
    {
        if (_registries.TryGetValue(moduleIdentity, out var registry))
        {
            return registry;
        }

        _registerCommands[moduleIdentity] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        registry = new CommandRegistry(moduleIdentity, this, _shared, _logger);
        _registries[moduleIdentity] = registry;
        return registry;
    }
    #endregion

    public bool IsCommandExists(string command)
    {
        foreach (var (_, value) in _registerCommands)
        {
            if (value.Contains(command))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取经过ms_装饰后的指令，这个一般只有服务端控制台指令需要
    /// </summary>
    /// <param name="originalCommand"></param>
    /// <param name="addPrefix"></param>
    /// <returns></returns>
    public string GetAddPrefixCommand(string originalCommand, bool addPrefix = true)
    {
        string actualRegisterCommand;
        if (addPrefix)
        {
            actualRegisterCommand = !originalCommand.StartsWith("ms_") ? $"ms_{originalCommand}" : originalCommand;
        }
        else
        {
            actualRegisterCommand = originalCommand;
        }

        return actualRegisterCommand;
    }

    /// <summary>
    /// 判断是否有ms_前缀
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public bool HasPrefix(string command)
    {
        return command.StartsWith("ms_", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取移除ms_装饰后的指令，这个一般只有游戏内指令需要
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public string GetStripPrefixCommand(string command)
    {
        return HasPrefix(command)
            ? command[3..]
            : // ms_ => 3 char
            command;
    }

    public void AddRegisteredCommand(string identity, string command)
    {
        if (_registerCommands.TryGetValue(identity, out var set))
        {
            set.Add(command);
        }

        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { command };
        _registerCommands[identity] = set;
    }

    private void RemoveRegistry(string identity)
    {
        if (!_registries.TryGetValue(identity, out var value))
        {
            return;
        }
        ((CommandRegistry)value).Clear();
        _registries.Remove(identity);
    }

    private void RemoveRegisteredCommands(string identity)
    {
        if (!_registerCommands.TryGetValue(identity, out var set))
        {
            return;
        }
        set.Clear();

        _registerCommands.Remove(identity);
    }
}

internal class CommandRegistry : ICommandRegistry
{
    private readonly string _identity;
    private readonly CommandManager _self;
    private readonly IClientManager _clientManager;
    private readonly ILogger<CommandManager> _logger;

    private readonly List<ClientCommandInfo> _clientCommands = [];
    private readonly List<ServerCommandInfo> _serverCommands = [];
    private readonly List<GenericCommandInfo> _genericCommands = [];
    private readonly IConVarManager _conVarManager;

    public CommandRegistry(string identity, CommandManager self, ISharedSystem sharedSystem, ILogger<CommandManager> logger)
    {
        _identity = identity;
        _self = self;
        _logger = logger;
        _clientManager = sharedSystem.GetClientManager();
        sharedSystem.GetModSharp();
        _conVarManager = sharedSystem.GetConVarManager();
    }

    public void RegisterClientCommand(string command, Action<IGameClient, StringCommand> call)
    {
        RegisterClientCommand(command, (client, stringCommand) =>
        {
            call(client, stringCommand);
            return ECommandAction.Handled;
        });
    }

    private void RegisterClientCommand(string command, DelegateClientCommand call)
    {
        if (_self.IsCommandExists(command))
        {
            _logger.LogWarning("Command `{Name}` has already registered.", command);
            return;
        }

        var info = new ClientCommandInfo(command, _self.GetStripPrefixCommand(command), call);
        Console.WriteLine($"Registering: {info.StripPrefixCommand}");
        _clientManager.InstallCommandCallback(info.StripPrefixCommand, info.Function);
        _clientCommands.Add(info);
        _self.AddRegisteredCommand(_identity, info.Command);

    }

    public void RegisterServerCommand(string command, Action<StringCommand> call, string description = "")
    {
        RegisterServerCommand(command, stringCommand =>
        {
            call(stringCommand);
            return ECommandAction.Handled;
        }, description);
    }

    private void RegisterServerCommand(string command, Func<StringCommand, ECommandAction> call,
        string description = "")
    {
        if (_self.IsCommandExists(command))
        {
            _logger.LogWarning("Command `{Name}` has already registered.", command);
            return;
        }
        var info = new ServerCommandInfo(command, _self.GetAddPrefixCommand(command),
            (_, stringCommand) => call(stringCommand));

        _conVarManager.CreateServerCommand(info.AddPrefixCommand, info.OnServerCommand);
        _serverCommands.Add(info);
        _self.AddRegisteredCommand(_identity, info.Command);
    }

    public void RegisterGenericCommand(string command, Action<IGameClient?, StringCommand> call, string description = "")
    {
        RegisterGenericCommand(command, (client, stringCommand) =>
        {
            call(client, stringCommand);
            return ECommandAction.Handled;
        }, description);
    }

    private void RegisterGenericCommand(string command, Func<IGameClient?, StringCommand, ECommandAction> call,
        string description = "")
    {
        if (_self.IsCommandExists(command))
        {
            _logger.LogWarning("Command `{Name}` has already registered.", command);
            return;
        }

        var info = new GenericCommandInfo(command, _self.GetAddPrefixCommand(command),
            _self.GetStripPrefixCommand(command), call);
        _clientManager.InstallCommandCallback(info.StripPrefixCommand, (client, stringCommand) => info.OnClientCommand(client, stringCommand));
        _conVarManager.CreateServerCommand(info.AddPrefixCommand, stringCommand => info.OnServerCommand(stringCommand), description);
        Console.WriteLine($"Registering: {command}, StripPrefix: {info.StripPrefixCommand}, AddPrefix: {info.AddPrefixCommand}");
        _genericCommands.Add(info);
        _self.AddRegisteredCommand(_identity, info.Command);
    }

    public void Clear()
    {
        foreach (var info in _clientCommands)
        {
            _clientManager.RemoveCommandCallback(info.StripPrefixCommand, info.Function);
        }

        foreach (var info in _genericCommands)
        {
            _conVarManager.ReleaseCommand(info.AddPrefixCommand);
            _clientManager.RemoveCommandCallback(info.StripPrefixCommand, info.OnClientCommand);
        }

        foreach (var info in _serverCommands)
        {
            _conVarManager.ReleaseCommand(info.AddPrefixCommand);
        }
    }

    private record ClientCommandInfo(string Command, string StripPrefixCommand, DelegateClientCommand Function);

    private record GenericCommandInfo(
        string Command,
        string AddPrefixCommand,
        string StripPrefixCommand,
        Func<IGameClient?, StringCommand, ECommandAction> Function)
    {
        public ECommandAction OnClientCommand(IGameClient client, StringCommand command)
        {
            return Function(client, command);
        }

        public ECommandAction OnServerCommand(StringCommand command)
        {
            return Function(null, command);
        }
    }

    private record ServerCommandInfo(
        string Command,
        string AddPrefixCommand,
        Func<IGameClient?, StringCommand, ECommandAction> Function)
    {
        //public ECommandAction OnConsoleCommand(IGameClient? client, StringCommand command)
        //{
        //    return Function(client, command);
        //}

        public ECommandAction OnServerCommand(StringCommand command)
        {
            return Function(null, command);
        }
    }
}