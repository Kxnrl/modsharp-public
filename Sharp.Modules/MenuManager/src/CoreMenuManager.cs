using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.InputManager.Shared;
using Sharp.Modules.MenuManager.Core.Controllers;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
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
    private readonly List<IDisposable>        _disposables;
    private readonly IEntityManager           _entityManager;
    private readonly IEventManager            _eventManager;

    private IModSharpModuleInterface<IInputManager>? _inputInterface;

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
    }

    private ECommandAction OnAA(IGameClient client, StringCommand command)
    {
        this.DisplayMenu(client,
                         Menu.Create()
                             .Title("Test Menu")
                             .Item("Option 1", controller => { })
                             .Item("Option 2", controller => { })
                             .Item("Option 3", controller => { })
                             .Spacer()
                             .Item("Exit", controller => controller.Exit())
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
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        _disposables.Clear();
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

    public void OnAllModulesLoaded()
    {
        _inputInterface = _modules.GetOptionalSharpModuleInterface<IInputManager>(IInputManager.Identity);

        // set the listener of cookie load event
        if (_inputInterface?.Instance is { } instance)
        {
            RegisterInput(instance);
        }
    }

    public void OnLibraryConnected(string name)
    {
        if (name.Equals("InputManager"))
        {
            _inputInterface = _modules.GetRequiredSharpModuleInterface<IInputManager>(IInputManager.Identity);

            if (_inputInterface?.Instance is { } instance)
            {
                RegisterInput(instance);
            }
        }
    }

    public void RegisterInput(IInputManager inputManager)
    {
        _disposables.Add(inputManager.AddInputListener(InputKey.W,
                                                       client =>
                                                       {
                                                           _controllers[client.Slot]
                                                               ?.MoveUpCursor();
                                                       }));

        _disposables.Add(inputManager.AddInputListener(InputKey.S,
                                                       client =>
                                                       {
                                                           _controllers[client.Slot]
                                                               ?.MoveDownCursor();
                                                       }));

        _disposables.Add(inputManager.AddInputListener(InputKey.A,
                                                       client =>
                                                       {
                                                           _controllers[client.Slot]
                                                               ?.GoToPreviousPage();
                                                       }));

        _disposables.Add(inputManager.AddInputListener(InputKey.D,
                                                       client =>
                                                       {
                                                           _controllers[client.Slot]
                                                               ?.GoToNextPage();
                                                       }));

        _disposables.Add(inputManager.AddInputListener(InputKey.Shift,
                                                       client =>
                                                       {
                                                           _controllers[client.Slot]
                                                               ?.GoBack();
                                                       }));

        _disposables.Add(inputManager.AddInputListener(InputKey.Space,
                                                       client =>
                                                       {
                                                           _controllers[client.Slot]
                                                               ?.Exit();
                                                       }));

        _disposables.Add(inputManager.AddInputListener(InputKey.F,
                                                       client =>
                                                       {
                                                           _controllers[client.Slot]
                                                               ?.Confirm();
                                                       }));
    }

    public void OnLibraryDisconnect(string name)
    {
        // match name
        if (name.Equals("InputManager"))
        {
            _inputInterface = null;
        }
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
            = new MoveableMenuController(this, _modSharp, _eventManager, _entityManager, _ => menu, client);
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
