using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.InputManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;
using Sharp.Shared.HookParams;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.InputManager;

internal sealed class InputManager : IModSharpModule, IInputManager
{
    private readonly ILogger<InputManager> _logger;
    private readonly IModSharp             _modSharp;
    private readonly ISharpModuleManager   _modules;
    private readonly IClientManager        _clients;
    private readonly IHookManager          _hooks;

    private readonly Dictionary<(IGameClient Client, InputKey Key), (DateTime PressedTime, bool HasTriggered)> _keyPressStates;
    private readonly Dictionary<InputKey, InputListenerInfo> _inputListeners;
    private readonly List<CombinationListenerInfo> _combinationListeners;
    private readonly Dictionary<string, InputListenerRegistry> _registries;

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

        _keyPressStates       = [];
        _combinationListeners = [];
        _registries           = [];

        _inputListeners = new Dictionary<InputKey, InputListenerInfo>
        {
            [InputKey.W]       = new (),
            [InputKey.S]       = new (),
            [InputKey.A]       = new (),
            [InputKey.D]       = new (),
            [InputKey.E]       = new (),
            [InputKey.Tab]     = new (),
            [InputKey.Space]   = new (),
            [InputKey.Shift]   = new (),
            [InputKey.Attack1] = new (),
            [InputKey.Attack2] = new (),
            [InputKey.R]       = new (),
            [InputKey.F]       = new (),
        };
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

    public void OnLibraryDisconnect(string name)
    {
        if (_registries.TryGetValue(name, out var registry))
        {
            registry.Cleanup();
        }

        _registries.Remove(name);
    }

    public void Shutdown()
    {
        _hooks.PlayerRunCommand.RemoveHookPost(OnPlayerRunCommandPost);
    }

    string IModSharpModule.DisplayName   => "InputManager";
    string IModSharpModule.DisplayAuthor => "Bone";

#endregion

#region IInputManager

    public IInputListenerRegistry GetInputListenerRegistry(string moduleIdentity)
    {
        if (_registries.TryGetValue(moduleIdentity, out var registry))
        {
            return registry;
        }

        registry                    = new InputListenerRegistry(this);
        _registries[moduleIdentity] = registry;

        return registry;
    }

#endregion

#region Internal Methods for Registry

    internal void AddInputListener(InputKey key, Action<IGameClient> callback, InputState state, float holdDuration)
    {
        var listenerInfo = new ListenerInfo(callback, holdDuration);
        var listeners    = GetListenerList(key, state);
        listeners.Add(listenerInfo);
    }

    internal void AddCombinationListener(InputKey[] keys, Action<IGameClient> callback, InputState state)
    {
        if (keys == null || keys.Length == 0)
        {
            throw new ArgumentException("Keys array cannot be null or empty");
        }

        var listenerInfo = new CombinationListenerInfo(keys, callback, state);
        _combinationListeners.Add(listenerInfo);
    }

    internal void RemoveInputListener(InputKey key, Action<IGameClient> callback, InputState state)
    {
        var listeners = GetListenerList(key, state);
        listeners.RemoveAll(x => x.Callback == callback);
    }

    internal void RemoveCombinationListener(InputKey[] keys, Action<IGameClient> callback, InputState state)
    {
        _combinationListeners.RemoveAll(x =>
                                            x.Keys.SequenceEqual(keys) && x.Callback == callback && x.State == state);
    }

    private List<ListenerInfo> GetListenerList(InputKey key, InputState state)
        => !_inputListeners.TryGetValue(key, out var listenerInfo)
            ? throw new NotSupportedException($"Unsupported input key: {key}")
            : listenerInfo.GetListeners(state);

#endregion

    private void OnPlayerRunCommandPost(IPlayerRunCommandHookParams @params, HookReturnValue<EmptyHookReturn> @return)
    {
        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.Forward,
                         InputKey.W);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.Back,
                         InputKey.S);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.MoveLeft,
                         InputKey.A);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.MoveRight,
                         InputKey.D);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.LookAtWeapon,
                         InputKey.F);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.Use,
                         InputKey.E);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.Speed,
                         InputKey.Shift);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.Jump,
                         InputKey.Space);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.Scoreboard,
                         InputKey.Tab);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.Attack,
                         InputKey.Attack1);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.Attack2,
                         InputKey.Attack2);

        ProcessKeyStates(@params.Client,
                         @params.Service,
                         UserCommandButtons.Reload,
                         InputKey.R);

        ProcessCombinationKeys(@params.Client, @params.Service.KeyButtons, @params.Service.KeyChangedButtons);
    }

    private void ProcessKeyStates(
        IGameClient        client,
        IMovementService   movementService,
        UserCommandButtons targetButton,
        InputKey           inputKey)
    {
        if (!_inputListeners.TryGetValue(inputKey, out var listenerInfo))
        {
            return;
        }

        var keyButtons        = movementService.KeyButtons;
        var keyChangedButtons = movementService.KeyChangedButtons;

        var isPressed  = keyButtons.HasFlag(targetButton);
        var hasChanged = keyChangedButtons.HasFlag(targetButton);

        var stateKey = (client, inputKey);

        if (hasChanged && isPressed)
        {
            _keyPressStates[stateKey] = (DateTime.UtcNow, false);

            var justPressedListeners = listenerInfo.GetListeners(InputState.KeyDown);

            if (justPressedListeners.Count > 0)
            {
                ProcessKeyListeners(justPressedListeners, client, inputKey, 0f);
            }
        }

        if (isPressed)
        {
            var pressedListeners = listenerInfo.GetListeners(InputState.KeyHold);

            if (pressedListeners.Count > 0)
            {
                var holdTime = 0f;

                if (_keyPressStates.TryGetValue(stateKey, out var state))
                {
                    holdTime = (float) (DateTime.UtcNow - state.PressedTime).TotalSeconds;
                }

                ProcessKeyListeners(pressedListeners, client, inputKey, holdTime);
            }
        }

        if (hasChanged && !isPressed)
        {
            _keyPressStates.Remove(stateKey);

            var releasedListeners = listenerInfo.GetListeners(InputState.KeyUp);

            if (releasedListeners.Count > 0)
            {
                ProcessKeyListeners(releasedListeners, client, inputKey, 0f);
            }
        }
    }

    private void ProcessKeyListeners(List<ListenerInfo> listeners, IGameClient client, InputKey inputKey, float currentHoldTime)
    {
        foreach (var listenerInfo in listeners)
        {
            try
            {
                if (listenerInfo.HoldDuration > 0f)
                {
                    if (currentHoldTime >= listenerInfo.HoldDuration)
                    {
                        var stateKey = (client, inputKey);

                        if (_keyPressStates.TryGetValue(stateKey, out var state) && !state.HasTriggered)
                        {
                            listenerInfo.Callback(client);
                            _keyPressStates[stateKey] = (state.PressedTime, true);
                        }
                    }
                }
                else
                {
                    listenerInfo.Callback(client);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing input listener for client {clientId}", client.SteamId);
            }
        }
    }

    private void ProcessCombinationKeys(IGameClient client, UserCommandButtons keyButtons, UserCommandButtons keyChangedButtons)
    {
        if (_combinationListeners.Count == 0)
        {
            return;
        }

        foreach (var listener in _combinationListeners)
        {
            try
            {
                var allKeysMatch  = true;
                var anyKeyChanged = false;

                foreach (var key in listener.Keys)
                {
                    if (!TryGetUserCommandButton(key, out var button))
                    {
                        allKeysMatch = false;

                        break;
                    }

                    var isPressed  = keyButtons.HasFlag(button);
                    var hasChanged = keyChangedButtons.HasFlag(button);

                    if (hasChanged)
                    {
                        anyKeyChanged = true;
                    }

                    switch (listener.State)
                    {
                        case InputState.KeyDown:
                            if (!isPressed)
                            {
                                allKeysMatch = false;
                            }

                            break;

                        case InputState.KeyHold:
                            if (!isPressed)
                            {
                                allKeysMatch = false;
                            }

                            break;

                        case InputState.KeyUp:
                            if (isPressed)
                            {
                                allKeysMatch = false;
                            }

                            break;
                    }

                    if (!allKeysMatch)
                    {
                        break;
                    }
                }

                if (allKeysMatch && (listener.State == InputState.KeyHold || anyKeyChanged))
                {
                    listener.Callback(client);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing combination listener for client {clientId}", client.SteamId);
            }
        }
    }

    private static bool TryGetUserCommandButton(InputKey key, out UserCommandButtons button)
    {
        const UserCommandButtons none = 0;

        button = key switch
        {
            InputKey.W       => UserCommandButtons.Forward,
            InputKey.S       => UserCommandButtons.Back,
            InputKey.A       => UserCommandButtons.MoveLeft,
            InputKey.D       => UserCommandButtons.MoveRight,
            InputKey.E       => UserCommandButtons.Use,
            InputKey.Space   => UserCommandButtons.Jump,
            InputKey.Shift   => UserCommandButtons.Speed,
            InputKey.Tab     => UserCommandButtons.Scoreboard,
            InputKey.Attack1 => UserCommandButtons.Attack,
            InputKey.Attack2 => UserCommandButtons.Attack2,
            InputKey.R       => UserCommandButtons.Reload,
            InputKey.F       => UserCommandButtons.LookAtWeapon,
            _                => none,
        };

        return button != none;
    }
}
