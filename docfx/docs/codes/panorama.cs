using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace PanoramaExample;

public sealed class PanoramaModule : IModSharpModule, IGameListener, IClientListener, IEntityListener
{
    private const string LayoutResource = "panorama/layout/custom_game/modsharp_docs/panorama-layout.vxml";
    private const string DialogId = "DocsDialog";
    private const string DismissedClass = "Dismissed";
    private const string AcknowledgeButtonId = "DocsAcknowledge";

    private readonly IModSharp _modSharp;
    private readonly IPanoramaManager _panorama;
    private readonly IEntityManager _entities;
    private readonly IClientManager _clients;
    private readonly HashSet<PlayerSlot> _openPlayers = [];
    private CEntityHandle<ICustomHudLayout>? _layoutHandle;
    private bool _mapEnding;
    private bool _gameListenerInstalled;
    private bool _entityListenerInstalled;
    private bool _clientListenerInstalled;
    private bool _commandInstalled;

    public PanoramaModule(ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration coreConfiguration,
        bool hotReload)
    {
        _modSharp = sharedSystem.GetModSharp();
        _panorama = sharedSystem.GetPanoramaManager();
        _entities = sharedSystem.GetEntityManager();
        _clients = sharedSystem.GetClientManager();
    }

    public string DisplayName => "Panorama Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    public bool Init()
    {
        _modSharp.InstallGameListener(this);
        _gameListenerInstalled = true;
        _entities.InstallEntityListener(this);
        _entityListenerInstalled = true;
        _clients.InstallClientListener(this);
        _clientListenerInstalled = true;
        _clients.InstallCommandCallback("panorama", OnPanoramaCommand);
        _commandInstalled = true;
        return true;
    }

    public void Shutdown()
    {
        if (_commandInstalled)
        {
            _clients.RemoveCommandCallback("panorama", OnPanoramaCommand);
            _commandInstalled = false;
        }

        RemoveLayout();

        if (_clientListenerInstalled)
        {
            _clients.RemoveClientListener(this);
            _clientListenerInstalled = false;
        }

        if (_entityListenerInstalled)
        {
            _entities.RemoveEntityListener(this);
            _entityListenerInstalled = false;
        }

        if (_gameListenerInstalled)
        {
            _modSharp.RemoveGameListener(this);
            _gameListenerInstalled = false;
        }
    }

    public void OnServerActivate() => _mapEnding = false;

    public void OnGameDeactivate()
    {
        _mapEnding = true;
        RemoveLayout();
    }

    public void OnGameShutdown()
    {
        // Native entities are no longer ours to access at this point.
        _mapEnding = true;
        _layoutHandle = null;
        _openPlayers.Clear();
    }

    public void OnEntityDeleted(IBaseEntity entity)
    {
        if (_layoutHandle is { } handle && entity.Handle.GetValue() == handle.GetValue())
        {
            // PanoramaManager removes the entity's callbacks on deletion.
            _layoutHandle = null;
            _openPlayers.Clear();
        }
    }

    public void OnClientPutInServer(IGameClient client)
    {
        if (client.IsFakeClient || client.IsHltv)
        {
            return;
        }

        ResetPlayer(client.Slot);
    }

    public void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
        if (client.IsFakeClient || client.IsHltv)
        {
            return;
        }

        ResetPlayer(client.Slot);
    }

    public void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
        => _openPlayers.Remove(client.Slot);

    private ECommandAction OnPanoramaCommand(IGameClient client, StringCommand command)
    {
        if (_mapEnding || !client.IsInGame || client.IsFakeClient || client.IsHltv)
        {
            return ECommandAction.Stopped;
        }

        var layout = FindLayout();
        if (layout is null)
        {
            RemoveLayout();
            layout = _panorama.CreateLayout(LayoutResource, "modsharp_docs_panorama");
            if (layout is null)
            {
                client.ConsolePrint("The custom HUD entity could not be created.");
                return ECommandAction.Stopped;
            }

            _layoutHandle = layout.Handle.As<ICustomHudLayout>();
            layout.Observable = false;
            _panorama.InstallClickCallback(layout, OnHudClicked);
        }

        if (_openPlayers.Contains(client.Slot))
        {
            ResetPlayer(client.Slot);
            return ECommandAction.Stopped;
        }

        client.ConsolePrint("Click Acknowledge to close the HUD, or run ms_panorama again.");
        _openPlayers.Add(client.Slot);
        layout.SetClassOverrideForPlayer(client.Slot, DialogId, DismissedClass, HudPanelClassStatus.ForceDisable);
        layout.SetInputCaptureEnabled(client.Slot, true);
        return ECommandAction.Stopped;
    }

    private void OnHudClicked(IPlayerController player, ICustomHudLayout layout, string buttonId)
    {
        if (_mapEnding || !layout.IsValid() || layout.IsMarkedForDeletion()
            || _layoutHandle is not { } handle || layout.Handle.GetValue() != handle.GetValue()
            || !player.IsValid() || !player.IsConnected()
            || !_openPlayers.Contains(player.PlayerSlot)
            || !string.Equals(buttonId, AcknowledgeButtonId, StringComparison.Ordinal))
        {
            return;
        }

        ResetPlayer(player.PlayerSlot);
        player.Print(HudPrintChannel.Chat, "[Panorama] HUD acknowledged.");
    }

    private ICustomHudLayout? FindLayout()
    {
        if (_layoutHandle is not { } handle)
        {
            return null;
        }

        var layout = _entities.FindEntityByHandle(handle);
        return layout is not null && layout.IsValid() && !layout.IsMarkedForDeletion() ? layout : null;
    }

    private void ResetPlayer(PlayerSlot slot)
    {
        _openPlayers.Remove(slot);
        if (FindLayout() is not { } layout)
        {
            return;
        }

        layout.SetInputCaptureEnabled(slot, false);
        layout.SetClassOverrideForPlayer(slot, DialogId, DismissedClass, HudPanelClassStatus.Undefined);
    }

    private void RemoveLayout()
    {
        var handle = _layoutHandle;
        _layoutHandle = null;
        if (handle is not { } value
            || _entities.FindEntityByHandle(value) is not { } layout
            || !layout.IsValid())
        {
            _openPlayers.Clear();
            return;
        }

        _panorama.RemoveClickCallback(layout, OnHudClicked);
        if (!layout.IsMarkedForDeletion())
        {
            foreach (var slot in _openPlayers)
            {
                layout.SetInputCaptureEnabled(slot, false);
            }

            _openPlayers.Clear();
            layout.Kill();
        }
        else
        {
            _openPlayers.Clear();
        }
    }

    int IGameListener.ListenerVersion => IGameListener.ApiVersion;
    int IGameListener.ListenerPriority => 0;
    int IClientListener.ListenerVersion => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;
    int IEntityListener.ListenerVersion => IEntityListener.ApiVersion;
    int IEntityListener.ListenerPriority => 0;
}
