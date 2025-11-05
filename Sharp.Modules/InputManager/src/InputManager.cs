using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.InputManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.InputManager.Core;

internal class InputManager : IModSharpModule, IInputManager
{
    public string DisplayName   => "InputManager";
    public string DisplayAuthor => "Bone";

    private readonly ILogger<InputManager> _logger;
    private readonly IModSharp             _modSharp;
    private readonly ISharpModuleManager   _modules;
    private readonly IClientManager        _clients;
    private readonly IHookManager          _hooks;

    private readonly List<Action<IGameClient>> _keyWListeners     = [];
    private readonly List<Action<IGameClient>> _keySListeners     = [];
    private readonly List<Action<IGameClient>> _keyAListeners     = [];
    private readonly List<Action<IGameClient>> _keyDListeners     = [];
    private readonly List<Action<IGameClient>> _keyEListeners     = [];
    private readonly List<Action<IGameClient>> _keyGListeners     = [];
    private readonly List<Action<IGameClient>> _keyTabListeners   = [];
    private readonly List<Action<IGameClient>> _keySpaceListeners = [];
    private readonly List<Action<IGameClient>> _keyShiftListeners = [];

    public InputManager(ISharedSystem sharedSystem,
        string                        dllPath,
        string                        sharpPath,
        Version                       version,
        IConfiguration                configuration,
        bool                          hotReload)
    {
        var loggerFactory = sharedSystem.GetLoggerFactory();

        _logger   = loggerFactory.CreateLogger<InputManager>();
        _modSharp = sharedSystem.GetModSharp();
        _modules  = sharedSystem.GetSharpModuleManager();
        _clients  = sharedSystem.GetClientManager();
        _hooks    = sharedSystem.GetHookManager();
    }

#region IModSharpModule

    public bool Init()
    {
        _hooks.PlayerRunCommand.InstallHookPost(OnPlayerRunCommandPost);

        return true;
    }

    public void PostInit()
    {
        _modules.RegisterSharpModuleInterface<IInputManager>(this, IInputManager.Identity, this);
    }

    public void Shutdown()
    {
        _hooks.PlayerRunCommand.RemoveHookPost(OnPlayerRunCommandPost);
    }

#endregion

#region IInputManager

    public IDisposable AddInputListener(InputKey key, Action<IGameClient> callback)
    {
        if (key == InputKey.W)
        {
            _keyWListeners.Add(callback);

            return new DisposeAction(() => _keyWListeners.Remove(callback));
        }

        if (key == InputKey.S)
        {
            _keySListeners.Add(callback);

            return new DisposeAction(() => _keySListeners.Remove(callback));
        }

        if (key == InputKey.A)
        {
            _keyAListeners.Add(callback);

            return new DisposeAction(() => _keyAListeners.Remove(callback));
        }

        if (key == InputKey.D)
        {
            _keyDListeners.Add(callback);

            return new DisposeAction(() => _keyDListeners.Remove(callback));
        }

        if (key == InputKey.E)
        {
            _keyEListeners.Add(callback);

            return new DisposeAction(() => _keyEListeners.Remove(callback));
        }

        if (key == InputKey.Space)
        {
            _keySpaceListeners.Add(callback);

            return new DisposeAction(() => _keySpaceListeners.Remove(callback));
        }

        if (key == InputKey.Shift)
        {
            _keyShiftListeners.Add(callback);

            return new DisposeAction(() => _keyShiftListeners.Remove(callback));
        }

        throw new ArgumentException("Unsupported input key: " + key);
    }

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

#endregion

    private void OnPlayerRunCommandPost(IPlayerRunCommandHookParams @params, HookReturnValue<EmptyHookReturn> @return)
    {
        if (_keyWListeners.Count > 0
            && @params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Forward)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Forward))
        {
            ProcessKeyListeners(_keyWListeners, @params.Client);
        }

        if (_keySListeners.Count > 0
            && @params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Back)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Back))
        {
            ProcessKeyListeners(_keySListeners, @params.Client);
        }

        if (_keyAListeners.Count > 0
            && @params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.MoveLeft)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.MoveLeft))
        {
            ProcessKeyListeners(_keyAListeners, @params.Client);
        }

        if (_keyDListeners.Count > 0
            && @params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.MoveRight)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.MoveRight))
        {
            ProcessKeyListeners(_keyDListeners, @params.Client);
        }

        if (_keyShiftListeners.Count > 0
            && @params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Speed)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Speed))
        {
            ProcessKeyListeners(_keyShiftListeners, @params.Client);
        }

        if (_keySpaceListeners.Count > 0
            && @params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Jump)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Jump))
        {
            ProcessKeyListeners(_keySpaceListeners, @params.Client);
        }

        if (_keyEListeners.Count > 0
            && @params.Service.KeyChangedButtons.HasFlag(UserCommandButtons.Use)
            && @params.Service.KeyButtons.HasFlag(UserCommandButtons.Use))
        {
            ProcessKeyListeners(_keyEListeners, @params.Client);
        }
    }

    private void ProcessKeyListeners(List<Action<IGameClient>> listeners, IGameClient client)
    {
        foreach (var callback in listeners)
        {
            try
            {
                callback(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing input listener for client {clientId}", client.SteamId);
            }
        }
    }
}
