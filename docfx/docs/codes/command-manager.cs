using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Extensions.CommandManager;
using Sharp.Shared;
using Sharp.Shared.Abstractions;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace CommandManagerExample;

public sealed class CommandManagerExample : IModSharpModule
{
    private readonly ServiceProvider _provider;
    private readonly IModSharp       _modSharp;
    private readonly IEntityManager  _entities;
    private readonly ICommandManager _commands;

    public CommandManagerExample(ISharedSystem sharedSystem,
        string                                 dllPath,
        string                                 sharpPath,
        Version                                version,
        IConfiguration                         coreConfiguration,
        bool                                   hotReload)
    {
        _modSharp = sharedSystem.GetModSharp();
        _entities = sharedSystem.GetEntityManager();

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
        _commands.RegisterClientCommand("kill", OnKillCommand);
        _commands.RegisterServerCommand("ms_echo", "Echo to server console", OnEchoCommand);
    }

    public void Shutdown()
    {
        // Shutdown unregisters commands; disposing the container alone does not call ISharpExtension.Shutdown.
        _provider.ShutdownAllSharpExtensions();
        _provider.Dispose();
    }

    // Type 'ms_kill' in the client console, or '.kill', '/kill', '!kill' in chat.
    private void OnKillCommand(IGameClient client, StringCommand command)
    {
        if (_entities.FindPlayerControllerBySlot(client.Slot)?.GetPlayerPawn() is { IsAlive: true } pawn)
        {
            pawn.Slay(true);
        }
    }

    // Type 'ms_echo' in the server console.
    private void OnEchoCommand(StringCommand command)
        => _modSharp.LogMessage($"Trigger command {command.GetCommandString()}");

    public string DisplayName   => "Command Manager Example";
    public string DisplayAuthor => "ModSharp dev team";
}
