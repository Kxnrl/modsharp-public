using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Extensions.CommandManager;
using Sharp.Shared;
using Sharp.Shared.Abstractions;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace CommandManagerExample;

public sealed class CommandManagerExample : IModSharpModule
{
    private readonly IServiceProvider _provider;
    private readonly IModSharp        _modSharp;
    private readonly IClientManager   _clients;
    private readonly IEntityManager   _entities;
    private readonly ICommandManager  _commands;

    public CommandManagerExample(ISharedSystem sharedSystem,
        string                                 dllPath,
        string                                 sharpPath,
        Version                                version,
        IConfiguration                         coreConfiguration,
        bool                                   hotReload)
    {
        _modSharp  = sharedSystem.GetModSharp();
        _clients   = sharedSystem.GetClientManager();
        _entities  = sharedSystem.GetEntityManager();

        var services = new ServiceCollection();
        services.AddCommandManager(sharedSystem);

        _provider = services.BuildServiceProvider();
        _commands = _provider.GetRequiredService<ICommandManager>();
    }

    public bool Init()
    {
        _provider.LoadAllSharpExtensions();

        return true;
    }

    public void PostInit()
    {
        _commands.RegisterAdminCommand("kick", OnKickCommand, "kick");
        _commands.RegisterClientCommand("kill", OnKillCommand);
        _commands.RegisterServerCommand("ms_echo", "Echo to server console", OnEchoCommand);
    }

    public void Shutdown()
        => _provider.ShutdownAllSharpExtensions();

    // type 'ms_kick' in client console, or chat trigger with '.kick' or '/kick' or '!kick'
    // the command won't be executed if the client is not an admin or has no kick permission
    private void OnKickCommand(IGameClient client, StringCommand command)
    {
        var reason = command.ArgCount > 0 ? command.GetArg(1) : "No reason specified";

        _modSharp.InvokeAction(() =>
        {
            if (!client.IsValid)
            {
                return;
            }

            _clients.KickClient(client, reason, NetworkDisconnectionReason.Kicked);
        });
    }

    // type 'ms_kill' in client console, or chat trigger with '.kill' or '/kill' or '!kill'
    private void OnKillCommand(IGameClient client, StringCommand command)
    {
        if (_entities.FindPlayerControllerBySlot(client.Slot)?.GetPlayerPawn() is { IsAlive: true } pawn)
        {
            pawn.Slay(true);
        }
    }

    // type 'ms_econ' on server console
    private void OnEchoCommand(StringCommand command)
    {
        Console.WriteLine("Hello");
        _modSharp.LogMessage($"Trigger command {command.GetCommandString()}");
    }

    public string DisplayName   => "NetMessage Hook Example";
    public string DisplayAuthor => "ModSharp dev team";
}