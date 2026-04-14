/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.LocalizerManager.Shared;
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

internal class MenuManager : IModSharpModule, IClientListener, IMenuManager
{
    public string DisplayName   => "MenuManager";
    public string DisplayAuthor => "Bone";

    private readonly ILogger<MenuManager>                        _logger;
    private readonly IModSharp                                   _modSharp;
    private readonly ISharpModuleManager                         _modules;
    private readonly IClientManager                              _clientManager;
    private readonly IHookManager                                _hooks;
    private readonly IEntityManager                              _entityManager;
    private readonly IEventManager                               _eventManager;
    private readonly IConfiguration                              _configuration;
    private          IModSharpModuleInterface<ILocalizerManager> _localizerManager = null!;

    private readonly IInternalMenuController?[] _controllers = new IInternalMenuController[PlayerSlot.MaxPlayerCount];
    private          ulong                      _nextSessionId;

    internal MenuKeyBindings KeyBindings { get; private set; }

    public MenuManager(ISharedSystem sharedSystem,
        string                       dllPath,
        string                       sharpPath,
        Version                      version,
        IConfiguration               configuration,
        bool                         hotReload)
    {
        var loggerFactory = sharedSystem.GetLoggerFactory();

        _logger        = loggerFactory.CreateLogger<MenuManager>();
        _modSharp      = sharedSystem.GetModSharp();
        _modules       = sharedSystem.GetSharpModuleManager();
        _clientManager = sharedSystem.GetClientManager();
        _hooks         = sharedSystem.GetHookManager();
        _entityManager = sharedSystem.GetEntityManager();
        _eventManager  = sharedSystem.GetEventManager();
        _configuration = configuration;

        KeyBindings = MenuKeyBindings.Load(configuration, _logger);
        MenuColor.Load(configuration, _logger);
    }

    private void OnConfigReload()
    {
        var oldBindings = KeyBindings;
        KeyBindings = MenuKeyBindings.Load(_configuration, _logger);
        MenuColor.Load(_configuration, _logger);
        _configuration.GetReloadToken().RegisterChangeCallback(_ => OnConfigReload(), null);

        _modSharp.InvokeFrameAction(() =>
        {
            RemoveCommandBindings(oldBindings);
            InstallCommandBindings(KeyBindings);

            foreach (var controller in _controllers)
            {
                controller?.Refresh();
            }
        });
    }

    private void InstallCommandBindings(MenuKeyBindings bindings)
    {
        if (bindings.MoveUpCursor.Type == MenuBindingType.Command)
        {
            _clientManager.InstallCommandListener(bindings.MoveUpCursor.Command!, OnMoveUpCommand);
        }

        if (bindings.MoveDownCursor.Type == MenuBindingType.Command)
        {
            _clientManager.InstallCommandListener(bindings.MoveDownCursor.Command!, OnMoveDownCommand);
        }

        if (bindings.GoBack.Type == MenuBindingType.Command)
        {
            _clientManager.InstallCommandListener(bindings.GoBack.Command!, OnGoBackCommand);
        }

        if (bindings.Confirm.Type == MenuBindingType.Command)
        {
            _clientManager.InstallCommandListener(bindings.Confirm.Command!, OnConfirmCommand);
        }

        if (bindings.Exit.Type == MenuBindingType.Command)
        {
            _clientManager.InstallCommandListener(bindings.Exit.Command!, OnExitCommand);
        }
    }

    private void RemoveCommandBindings(MenuKeyBindings bindings)
    {
        if (bindings.MoveUpCursor.Type == MenuBindingType.Command)
        {
            _clientManager.RemoveCommandListener(bindings.MoveUpCursor.Command!, OnMoveUpCommand);
        }

        if (bindings.MoveDownCursor.Type == MenuBindingType.Command)
        {
            _clientManager.RemoveCommandListener(bindings.MoveDownCursor.Command!, OnMoveDownCommand);
        }

        if (bindings.GoBack.Type == MenuBindingType.Command)
        {
            _clientManager.RemoveCommandListener(bindings.GoBack.Command!, OnGoBackCommand);
        }

        if (bindings.Confirm.Type == MenuBindingType.Command)
        {
            _clientManager.RemoveCommandListener(bindings.Confirm.Command!, OnConfirmCommand);
        }

        if (bindings.Exit.Type == MenuBindingType.Command)
        {
            _clientManager.RemoveCommandListener(bindings.Exit.Command!, OnExitCommand);
        }
    }

    private ECommandAction OnMoveUpCommand(IGameClient client, StringCommand command)
    {
        _controllers[client.Slot]?.MoveUpCursor();

        return ECommandAction.Stopped;
    }

    private ECommandAction OnMoveDownCommand(IGameClient client, StringCommand command)
    {
        _controllers[client.Slot]?.MoveDownCursor();

        return ECommandAction.Stopped;
    }

    private ECommandAction OnGoBackCommand(IGameClient client, StringCommand command)
    {
        _controllers[client.Slot]?.GoBack();

        return ECommandAction.Stopped;
    }

    private ECommandAction OnConfirmCommand(IGameClient client, StringCommand command)
    {
        _controllers[client.Slot]?.Confirm();

        return ECommandAction.Stopped;
    }

    private ECommandAction OnExitCommand(IGameClient client, StringCommand command)
    {
        _controllers[client.Slot]?.Exit();

        return ECommandAction.Stopped;
    }

#region IModSharpModule

    public bool Init()
    {
        _clientManager.InstallClientListener(this);

        // take the highest priority to prevent other hooks from modifying the buttons
        _hooks.PlayerRunCommand.InstallHookPre(OnPlayerRunCommandPre, int.MaxValue);

        InstallCommandBindings(KeyBindings);

        _configuration.GetReloadToken().RegisterChangeCallback(_ => OnConfigReload(), null);

        return true;
    }

    public void PostInit()
    {
        _modules.RegisterSharpModuleInterface<IMenuManager>(this, IMenuManager.Identity, this);
    }

    public void OnAllModulesLoaded()
    {
        _localizerManager = _modules.GetRequiredSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity);

        if (_localizerManager.Instance is not { } @interface)
        {
            _logger.LogWarning("Sharp.Modules.LocalizerManager is not loaded.");

            return;
        }

        @interface.LoadLocaleFile("basemenu", true);
    }

    public void Shutdown()
    {
        _hooks.PlayerRunCommand.RemoveHookPre(OnPlayerRunCommandPre);
        RemoveCommandBindings(KeyBindings);
        _clientManager.RemoveClientListener(this);

        for (var i = 0; i < _controllers.Length; i++)
        {
            _controllers[i]?.Dispose();
            _controllers[i] = null;
        }

        SurvivalStatusMenuController.ReleaseSharedEvent();
    }

#endregion

#region IClientListener

    public void OnClientPutInServer(IGameClient client)
    {
        DisposeClientMenu(client);
    }

    public void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        DisposeClientMenu(client);
    }

    int IClientListener.ListenerVersion  => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;

#endregion

    private unsafe HookReturnValue<EmptyHookReturn> OnPlayerRunCommandPre(IPlayerRunCommandHookParams @params,
        HookReturnValue<EmptyHookReturn>                                                              @return)
    {
        if (_controllers[@params.Client.Slot] is not { } menuController)
        {
            return new HookReturnValue<EmptyHookReturn>();
        }

        var cmd = @params.BaseUserCmd;

        if (cmd is null)
        {
            return new HookReturnValue<EmptyHookReturn>();
        }

        var bindings = KeyBindings;
        var mask     = bindings.GetButtonMask();

        var changed = @params.ChangedButtons;

        var pressed = @params.KeyButtons;

        const UserCommandButtons moveButtons = UserCommandButtons.MoveLeft
                                               | UserCommandButtons.MoveRight
                                               | UserCommandButtons.Forward
                                               | UserCommandButtons.Back;

        if (!menuController.Menu.IsPlayerMovementEnabled && (mask & moveButtons) != 0 && (pressed & moveButtons) != 0)
        {
            @params.KeyButtons     &= ~moveButtons;
            @params.ChangedButtons &= ~moveButtons;

            cmd->SideMove = cmd->ForwardMove = 0f;

            var buttonState = cmd->ButtonState;
            buttonState->ButtonChanged &= ~moveButtons;
            buttonState->ButtonPressed &= ~moveButtons;
        }

        if ((changed & mask) == 0)
        {
            return new HookReturnValue<EmptyHookReturn>();
        }

        if (bindings.MoveUpCursor is { Type: MenuBindingType.Button, Button: { } moveUpBtn }
            && changed.HasFlag(moveUpBtn)
            && pressed.HasFlag(moveUpBtn))
        {
            menuController.MoveUpCursor();
        }

        if (bindings.MoveDownCursor is { Type: MenuBindingType.Button, Button: { } moveDownBtn }
            && changed.HasFlag(moveDownBtn)
            && pressed.HasFlag(moveDownBtn))
        {
            menuController.MoveDownCursor();
        }

        if (bindings.GoBack is { Type: MenuBindingType.Button, Button: { } goBackBtn }
            && changed.HasFlag(goBackBtn)
            && pressed.HasFlag(goBackBtn))
        {
            menuController.GoBack();
        }

        if (bindings.Confirm is { Type: MenuBindingType.Button, Button: { } confirmBtn }
            && changed.HasFlag(confirmBtn)
            && pressed.HasFlag(confirmBtn))
        {
            menuController.Confirm();
        }

        if (bindings.Exit is { Type: MenuBindingType.Button, Button: { } exitBtn }
            && changed.HasFlag(exitBtn)
            && pressed.HasFlag(exitBtn))
        {
            menuController.Exit();
        }

        return new HookReturnValue<EmptyHookReturn>();
    }

    public void DisplayMenu(IGameClient client, Menu menu)
    {
        DisplayMenu(client, menu, out _);
    }

    public void DisplayMenu(IGameClient client, Menu menu, out ulong sessionId)
    {
        DisposeClientMenu(client);

        ILocalizerManager? instance;

        if (_localizerManager.Instance is not { } value)
        {
            _logger.LogWarning("Sharp.Modules.LocalizerManager is not loaded.");
            instance = null;
        }
        else
        {
            instance = value;
        }

        sessionId = Interlocked.Increment(ref _nextSessionId);

        var controller
            = new SurvivalStatusMenuController(this,
                                               _modSharp,
                                               _eventManager,
                                               _entityManager,
                                               sessionId,
                                               _ => menu,
                                               client,
                                               instance);

        _controllers[client.Slot] = controller;
        controller.Render();
    }

    public void QuitMenu(IGameClient client)
    {
        if (_controllers[client.Slot] is not { } controller)
        {
            throw new InvalidOperationException("Client is not in a menu.");
        }

        controller.Exit();
    }

    public bool IsInMenu(IGameClient client)
        => _controllers[client.Slot] is not null;

    public bool IsInMenu(IGameClient client, Menu menuInstance)
    {
        ArgumentNullException.ThrowIfNull(menuInstance);

        return _controllers[client.Slot]?.IsInMenu(menuInstance) ?? false;
    }

    public bool IsInMenu(IGameClient client, ulong sessionId)
    {
        if (sessionId == 0 || _controllers[client.Slot] is not { } controller)
        {
            return false;
        }

        return controller.SessionId == sessionId;
    }

    public bool IsInCurrentMenu(IGameClient client, Menu menuInstance)
    {
        ArgumentNullException.ThrowIfNull(menuInstance);

        return _controllers[client.Slot]?.IsInCurrentMenu(menuInstance) ?? false;
    }

    public bool TryGetCurrentMenuSessionId(IGameClient client, out ulong sessionId)
    {
        if (_controllers[client.Slot] is not { } controller)
        {
            sessionId = 0;

            return false;
        }

        sessionId = controller.SessionId;

        return true;
    }

    public void CloseClientMenu(IGameClient client)
    {
        _modSharp.PushTimer(() => { DisposeClientMenu(client); },
                            0.01);
    }

    private void DisposeClientMenu(IGameClient client)
    {
        _controllers[client.Slot]?.Exit();
        _controllers[client.Slot]?.Dispose();
        _controllers[client.Slot] = null;
    }
}
