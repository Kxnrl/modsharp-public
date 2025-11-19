using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.MenuManager.Core.Controllers;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.MenuManager.Core;

internal class CoreMenuManager : IModSharpModule, IClientListener, IMenuManager
{
    public string DisplayName   => "MenuManager";
    public string DisplayAuthor => "Bone";

    private readonly ILogger<CoreMenuManager> _logger;
    private readonly IModSharp                _modSharp;
    private readonly ISharpModuleManager      _modules;
    private readonly IClientManager           _clients;
    private readonly IHookManager             _hooks;
    private readonly IEntityManager           _entityManager;
    private readonly IEventManager            _eventManager;

    private readonly IInternalMenuController?[] _controllers = new IInternalMenuController[PlayerSlot.MaxPlayerSlot];

    public CoreMenuManager(ISharedSystem sharedSystem,
        string                           dllPath,
        string                           sharpPath,
        Version                          version,
        IConfiguration                   configuration,
        bool                             hotReload)
    {
        var loggerFactory = sharedSystem.GetLoggerFactory();

        _logger        = loggerFactory.CreateLogger<CoreMenuManager>();
        _modSharp      = sharedSystem.GetModSharp();
        _modules       = sharedSystem.GetSharpModuleManager();
        _clients       = sharedSystem.GetClientManager();
        _hooks         = sharedSystem.GetHookManager();
        _entityManager = sharedSystem.GetEntityManager();
        _eventManager  = sharedSystem.GetEventManager();

        _clients.InstallCommandCallback("aa", OnAA);
        _hooks.PlayerRunCommand.InstallHookPost(OnPlayerRunCommandPost);
    }

    private ECommandAction OnAA(IGameClient client, StringCommand command)
    {
        DisplayMenu(client,
                    Menu.Create()
                        .Title("Test Menu")
                        .Item("Option 1", controller => { })
                        .Item("Option 2", controller => { })
                        .Item("Option 3", controller => { })
                        .Spacer()
                        .Build());

        return ECommandAction.Handled;
    }

#region IModSharpModule

    public bool Init()
    {
        _clients.InstallClientListener(this);

        return true;
    }

    public void PostInit()
    {
        _modules.RegisterSharpModuleInterface<IMenuManager>(this, IMenuManager.Identity, this);
    }

    public void Shutdown()
    {
        _hooks.PlayerRunCommand.RemoveHookPost(OnPlayerRunCommandPost);
    }

#endregion

    private class DisposeAction : IDisposable
    {
        private readonly Action _disposeAction;
        private          bool   _disposed;

        public DisposeAction(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposeAction();
            _disposed = true;
        }
    }

#region IClientListener

    int IClientListener.ListenerVersion  => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;

#endregion

    private void OnPlayerRunCommandPost(IPlayerRunCommandHookParams @params, HookReturnValue<EmptyHookReturn> @return)
    {
        if (@params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Forward)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Forward))
        {
            _controllers[@params.Client.Slot]
                ?.MoveUpCursor();
        }

        if (@params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Back)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Back))
        {
            _controllers[@params.Client.Slot]
                ?.MoveDownCursor();
        }

        if (@params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.MoveLeft)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.MoveLeft))
        {
            _controllers[@params.Client.Slot]
                ?.GoToPreviousPage();
        }

        if (@params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.MoveRight)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.MoveRight))
        {
            _controllers[@params.Client.Slot]
                ?.GoToNextPage();
        }

        if (@params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Speed)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Speed))
        {
            _controllers[@params.Client.Slot]
                ?.GoBack();
        }

        if (@params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Jump)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Jump))
        {
            _controllers[@params.Client.Slot]
                ?.Exit();
        }

        if (@params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Use)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Use))
        {
            _controllers[@params.Client.Slot]
                ?.Confirm();
        }
    }

    public void OnAllModulesLoaded()
    {
    }

    public void OnLibraryConnected(string name)
    {
    }

    public void OnLibraryDisconnect(string name)
    {
    }

    public void DisplayMenu(IGameClient client, Menu menu)
    {
        if (_controllers[client.Slot] != null)
        {
            _controllers[client.Slot]
                ?.Dispose();

            _controllers[client.Slot] = null;
        }

        _controllers[client.Slot]
            = new SurvivalStatusMenuController(this, _modSharp, _eventManager, _entityManager, _ => menu, client);
    }

    public void ClosePlayerMenu(IGameClient client)
    {
        _modSharp.PushTimer(() =>
                            {
                                var controller = _controllers[client.Slot];

                                if (controller != null)
                                {
                                    controller.Dispose();
                                    _controllers[client.Slot] = null;
                                }
                            },
                            0.01);
    }
}
