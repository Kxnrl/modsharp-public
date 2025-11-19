using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.InputManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.InputManager.Core;

internal class InputManager : IModSharpModule, IClientListener, IInputManager
{
    public string DisplayName   => "InputManager";
    public string DisplayAuthor => "Bone";

    private readonly ILogger<InputManager> _logger;
    private readonly IModSharp             _modSharp;
    private readonly ISharpModuleManager   _modules;
    private readonly IClientManager        _clients;
    private readonly IHookManager          _hooks;

    // Key state tracking: <(ClientId, InputKey), (PressedTime, HasTriggered)>
    private readonly Dictionary<(ulong ClientId, InputKey Key), (DateTime PressedTime, bool HasTriggered)> _keyPressStates
        = new ();

    // Listener info class
    private class ListenerInfo
    {
        public Action<IGameClient> Callback     { get; }
        public float               HoldDuration { get; }

        public ListenerInfo(Action<IGameClient> callback, float holdDuration)
        {
            Callback     = callback;
            HoldDuration = holdDuration;
        }
    }

    // W key listeners
    private readonly List<ListenerInfo> _keyWJustPressedListeners = [];
    private readonly List<ListenerInfo> _keyWPressedListeners     = [];
    private readonly List<ListenerInfo> _keyWReleasedListeners    = [];

    // S key listeners
    private readonly List<ListenerInfo> _keySJustPressedListeners = [];
    private readonly List<ListenerInfo> _keySPressedListeners     = [];
    private readonly List<ListenerInfo> _keySReleasedListeners    = [];

    // A key listeners
    private readonly List<ListenerInfo> _keyAJustPressedListeners = [];
    private readonly List<ListenerInfo> _keyAPressedListeners     = [];
    private readonly List<ListenerInfo> _keyAReleasedListeners    = [];

    // D key listeners
    private readonly List<ListenerInfo> _keyDJustPressedListeners = [];
    private readonly List<ListenerInfo> _keyDPressedListeners     = [];
    private readonly List<ListenerInfo> _keyDReleasedListeners    = [];

    // E key listeners
    private readonly List<ListenerInfo> _keyEJustPressedListeners = [];
    private readonly List<ListenerInfo> _keyEPressedListeners     = [];
    private readonly List<ListenerInfo> _keyEReleasedListeners    = [];

    // G key listeners
    private readonly List<ListenerInfo> _keyGJustPressedListeners = [];
    private readonly List<ListenerInfo> _keyGPressedListeners     = [];
    private readonly List<ListenerInfo> _keyGReleasedListeners    = [];

    // Tab key listeners
    private readonly List<ListenerInfo> _keyTabJustPressedListeners = [];
    private readonly List<ListenerInfo> _keyTabPressedListeners     = [];
    private readonly List<ListenerInfo> _keyTabReleasedListeners    = [];

    // Space key listeners
    private readonly List<ListenerInfo> _keySpaceJustPressedListeners = [];
    private readonly List<ListenerInfo> _keySpacePressedListeners     = [];
    private readonly List<ListenerInfo> _keySpaceReleasedListeners    = [];

    // Shift key listeners
    private readonly List<ListenerInfo> _keyShiftJustPressedListeners = [];
    private readonly List<ListenerInfo> _keyShiftPressedListeners     = [];
    private readonly List<ListenerInfo> _keyShiftReleasedListeners    = [];

    // Combination key listener info
    private class CombinationListenerInfo
    {
        public InputKey[]          Keys     { get; }
        public Action<IGameClient> Callback { get; }
        public InputState          State    { get; }

        public CombinationListenerInfo(InputKey[] keys, Action<IGameClient> callback, InputState state)
        {
            Keys     = keys;
            Callback = callback;
            State    = state;
        }
    }

    private readonly List<CombinationListenerInfo> _combinationListeners = [];

    private readonly List<(IGameClient Client, Action<IGameClient, string> Callback, bool HandleChat)> _nextChatListeners = [];

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
        _clients.InstallClientListener(this);

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

    public IDisposable AddInputListener(InputKey key, Action<IGameClient> callback, InputState state = InputState.JustPressed,
        float                                    holdDuration = 0f)
    {
        var listenerInfo = new ListenerInfo(callback, holdDuration);
        var listeners    = GetListenerList(key, state);
        listeners.Add(listenerInfo);

        return new DisposeAction(() => listeners.Remove(listenerInfo));
    }

    public IDisposable AddCombinationListener(InputKey[] keys, Action<IGameClient> callback,
        InputState                                       state = InputState.JustPressed)
    {
        if (keys == null || keys.Length == 0)
            throw new ArgumentException("Keys array cannot be null or empty");

        var listenerInfo = new CombinationListenerInfo(keys, callback, state);
        _combinationListeners.Add(listenerInfo);

        return new DisposeAction(() => _combinationListeners.Remove(listenerInfo));
    }

    private List<ListenerInfo> GetListenerList(InputKey key, InputState state)
    {
        return (key, state) switch
        {
            (InputKey.W, InputState.JustPressed) => _keyWJustPressedListeners,
            (InputKey.W, InputState.Pressed)     => _keyWPressedListeners,
            (InputKey.W, InputState.Released)    => _keyWReleasedListeners,

            (InputKey.S, InputState.JustPressed) => _keySJustPressedListeners,
            (InputKey.S, InputState.Pressed)     => _keySPressedListeners,
            (InputKey.S, InputState.Released)    => _keySReleasedListeners,

            (InputKey.A, InputState.JustPressed) => _keyAJustPressedListeners,
            (InputKey.A, InputState.Pressed)     => _keyAPressedListeners,
            (InputKey.A, InputState.Released)    => _keyAReleasedListeners,

            (InputKey.D, InputState.JustPressed) => _keyDJustPressedListeners,
            (InputKey.D, InputState.Pressed)     => _keyDPressedListeners,
            (InputKey.D, InputState.Released)    => _keyDReleasedListeners,

            (InputKey.E, InputState.JustPressed) => _keyEJustPressedListeners,
            (InputKey.E, InputState.Pressed)     => _keyEPressedListeners,
            (InputKey.E, InputState.Released)    => _keyEReleasedListeners,

            (InputKey.G, InputState.JustPressed) => _keyGJustPressedListeners,
            (InputKey.G, InputState.Pressed)     => _keyGPressedListeners,
            (InputKey.G, InputState.Released)    => _keyGReleasedListeners,

            (InputKey.Tab, InputState.JustPressed) => _keyTabJustPressedListeners,
            (InputKey.Tab, InputState.Pressed)     => _keyTabPressedListeners,
            (InputKey.Tab, InputState.Released)    => _keyTabReleasedListeners,

            (InputKey.Space, InputState.JustPressed) => _keySpaceJustPressedListeners,
            (InputKey.Space, InputState.Pressed)     => _keySpacePressedListeners,
            (InputKey.Space, InputState.Released)    => _keySpaceReleasedListeners,

            (InputKey.Shift, InputState.JustPressed) => _keyShiftJustPressedListeners,
            (InputKey.Shift, InputState.Pressed)     => _keyShiftPressedListeners,
            (InputKey.Shift, InputState.Released)    => _keyShiftReleasedListeners,

            _ => throw new ArgumentException($"Unsupported input key or state: {key}, {state}")
        };
    }

    public IDisposable GetClientNextChat(IGameClient client, Action<IGameClient, string> callback, bool handleChat = true)
    {
        _nextChatListeners.Add((client, callback, handleChat));

        return new DisposeAction(() => _nextChatListeners.RemoveAll(x => x.Client.Equals(client) && x.Callback == callback));
    }

    public void CancelClientNextChat(IGameClient client)
    {
        _nextChatListeners.RemoveAll(x => x.Client.Equals(client));
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
        // W key
        ProcessKeyStates(@params.Client,
                         @params.Service.KeyButtons,
                         @params.Service.KeyChangedButtons,
                         UserCommandButtons.Forward,
                         InputKey.W,
                         _keyWJustPressedListeners,
                         _keyWPressedListeners,
                         _keyWReleasedListeners);

        // S key
        ProcessKeyStates(@params.Client,
                         @params.Service.KeyButtons,
                         @params.Service.KeyChangedButtons,
                         UserCommandButtons.Back,
                         InputKey.S,
                         _keySJustPressedListeners,
                         _keySPressedListeners,
                         _keySReleasedListeners);

        // A key
        ProcessKeyStates(@params.Client,
                         @params.Service.KeyButtons,
                         @params.Service.KeyChangedButtons,
                         UserCommandButtons.MoveLeft,
                         InputKey.A,
                         _keyAJustPressedListeners,
                         _keyAPressedListeners,
                         _keyAReleasedListeners);

        // D key
        ProcessKeyStates(@params.Client,
                         @params.Service.KeyButtons,
                         @params.Service.KeyChangedButtons,
                         UserCommandButtons.MoveRight,
                         InputKey.D,
                         _keyDJustPressedListeners,
                         _keyDPressedListeners,
                         _keyDReleasedListeners);

        // E key
        ProcessKeyStates(@params.Client,
                         @params.Service.KeyButtons,
                         @params.Service.KeyChangedButtons,
                         UserCommandButtons.Use,
                         InputKey.E,
                         _keyEJustPressedListeners,
                         _keyEPressedListeners,
                         _keyEReleasedListeners);

        // Shift key
        ProcessKeyStates(@params.Client,
                         @params.Service.KeyButtons,
                         @params.Service.KeyChangedButtons,
                         UserCommandButtons.Speed,
                         InputKey.Shift,
                         _keyShiftJustPressedListeners,
                         _keyShiftPressedListeners,
                         _keyShiftReleasedListeners);

        // Space key
        ProcessKeyStates(@params.Client,
                         @params.Service.KeyButtons,
                         @params.Service.KeyChangedButtons,
                         UserCommandButtons.Jump,
                         InputKey.Space,
                         _keySpaceJustPressedListeners,
                         _keySpacePressedListeners,
                         _keySpaceReleasedListeners);

        // Process combination keys
        ProcessCombinationKeys(@params.Client, @params.Service.KeyButtons, @params.Service.KeyChangedButtons);
    }

    private void ProcessKeyStates(
        IGameClient        client,
        UserCommandButtons keyButtons,
        UserCommandButtons keyChangedButtons,
        UserCommandButtons targetButton,
        InputKey           inputKey,
        List<ListenerInfo> justPressedListeners,
        List<ListenerInfo> pressedListeners,
        List<ListenerInfo> releasedListeners)
    {
        var isPressed  = keyButtons.HasFlag(targetButton);
        var hasChanged = keyChangedButtons.HasFlag(targetButton);

        var stateKey = (client.SteamId, inputKey);

        // JustPressed: key state just changed and is now pressed
        if (hasChanged && isPressed)
        {
            // Record press time
            _keyPressStates[stateKey] = (DateTime.UtcNow, false);

            if (justPressedListeners.Count > 0)
            {
                ProcessKeyListeners(justPressedListeners, client, inputKey, 0f);
            }
        }

        // Pressed: key is being held down (regardless of whether it just changed)
        if (isPressed && pressedListeners.Count > 0)
        {
            var holdTime = 0f;

            if (_keyPressStates.TryGetValue(stateKey, out var state))
            {
                holdTime = (float) (DateTime.UtcNow - state.PressedTime).TotalSeconds;
            }

            ProcessKeyListeners(pressedListeners, client, inputKey, holdTime);
        }

        // Released: key state just changed and is now released
        if (hasChanged && !isPressed)
        {
            // Clear key state
            _keyPressStates.Remove(stateKey);

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
                // Check if hold duration is required
                if (listenerInfo.HoldDuration > 0f)
                {
                    // Key must be held for specified duration
                    if (currentHoldTime >= listenerInfo.HoldDuration)
                    {
                        var stateKey = (client.SteamId, inputKey);

                        // Check if already triggered
                        if (_keyPressStates.TryGetValue(stateKey, out var state) && !state.HasTriggered)
                        {
                            listenerInfo.Callback(client);

                            // Mark as triggered
                            _keyPressStates[stateKey] = (state.PressedTime, true);
                        }
                    }
                }
                else
                {
                    // No hold duration required, trigger immediately
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
            return;

        foreach (var listener in _combinationListeners)
        {
            try
            {
                var allKeysMatch  = true;
                var anyKeyChanged = false;

                // Check if all keys meet the conditions
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
                        anyKeyChanged = true;

                    switch (listener.State)
                    {
                        case InputState.JustPressed:
                            // All keys must be pressed, and at least one key state just changed
                            if (!isPressed)
                            {
                                allKeysMatch = false;
                            }

                            break;

                        case InputState.Pressed:
                            // All keys must be held down continuously
                            if (!isPressed)
                            {
                                allKeysMatch = false;
                            }

                            break;

                        case InputState.Released:
                            // At least one key just released, and all other keys are also released
                            if (isPressed)
                            {
                                allKeysMatch = false;
                            }

                            break;
                    }

                    if (!allKeysMatch)
                        break;
                }

                // For JustPressed and Released states, at least one key state must have changed
                if (allKeysMatch && (listener.State == InputState.Pressed || anyKeyChanged))
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

    private bool TryGetUserCommandButton(InputKey key, out UserCommandButtons button)
    {
        button = key switch
        {
            InputKey.W     => UserCommandButtons.Forward,
            InputKey.S     => UserCommandButtons.Back,
            InputKey.A     => UserCommandButtons.MoveLeft,
            InputKey.D     => UserCommandButtons.MoveRight,
            InputKey.E     => UserCommandButtons.Use,
            InputKey.Space => UserCommandButtons.Jump,
            InputKey.Shift => UserCommandButtons.Speed,
            _              => default,
        };

        return button != default;
    }

#region IClientListener

    int IClientListener.ListenerVersion  => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;

    public ECommandAction OnClientSayCommand(IGameClient client,
        bool                                             teamOnly,
        bool                                             isCommand,
        string                                           commandName,
        string                                           message)
    {
        var needHandle = false;

        if (!isCommand)
        {
            for (var i = 0; i < _nextChatListeners.Count; i++)
            {
                var (c, callback, handleChat) = _nextChatListeners[i];

                if (c.Equals(client))
                {
                    try
                    {
                        callback(client, message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while processing next chat listener for client {clientId}", client.SteamId);
                    }

                    _nextChatListeners.RemoveAt(i);

                    if (handleChat)
                    {
                        needHandle = true;
                    }
                }
            }
        }

        if (needHandle)
        {
            return ECommandAction.Stopped;
        }

        return ECommandAction.Skipped;
    }

#endregion
}
