using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace MenuExample;

public sealed class MenuExample : IModSharpModule
{
    private const string ProviderAssemblyName = "Sharp.Modules.MenuManager";
    private readonly ISharedSystem _shared;
    private readonly ILogger<MenuExample> _logger;
    private readonly Menu _cachedMenu;
    private readonly Menu _cachedSubMenu;
    private readonly Dictionary<IGameClient, ulong> _sessions = [];
    private IModSharpModuleInterface<IMenuManager>? _menuManager;
    private bool _useCachedMenu = true;
    private bool _showStatusMessages = true;

    public MenuExample(ISharedSystem sharedSystem,
        string                      dllPath,
        string                      sharpPath,
        Version                     version,
        IConfiguration              configuration,
        bool                        hotReload)
    {
        _shared = sharedSystem;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<MenuExample>();

        // Cache static menu definitions, not per-client controllers or native entities.
        _cachedSubMenu = Menu.Create()
                             .Title("Sub Menu")
                             .Cursor("»", "«") // Text is embedded in HTML; avoid raw angle brackets.
                             .DisabledItem("This item is not selectable")
                             .Spacer()
                             .Item("Print my slot", controller =>
                                 controller.Client.Print(HudPrintChannel.Chat, $"Your slot: {controller.Client.Slot}"))
                             .BackItem()
                             .ExitItem()
                             .Build();

        _cachedMenu = Menu.Create()
                          .Title("Main Menu (Cached)")
                          .HideIndex()
                          .SubMenu("Open Sub Menu", _cachedSubMenu)
                          .Item(_ => $"Menu status messages (all players): {(_showStatusMessages ? "ON" : "OFF")}",
                              controller =>
                              {
                                  _showStatusMessages = !_showStatusMessages;
                                  controller.Refresh(); // Re-evaluate text and state for this session.
                              })
                          .Item("Give me a deagle!", OnGiveDeagle)
                          .Item(client => client.IsAuthenticated ? "Deagle (authenticated)" : "Deagle",
                              OnGiveDeagle)
                          .Item(BuildAk47Item)
                          .OnEnter(OnEnterMenu)
                          .OnExit(OnExitMenu)
                          .Build();
    }

    public bool Init()
    {
        _shared.GetHookManager().PlayerSpawnPost.InstallForward(OnPlayerSpawned);
        _shared.GetConVarManager().CreateServerCommand("menu_type_toggle", OnToggleMenuType);
        return true;
    }

    public void OnAllModulesLoaded()
        => ResolveMenuManager();

    public void OnLibraryConnected(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            ResolveMenuManager();
        }
    }

    public void OnLibraryDisconnect(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _menuManager = null;
            _sessions.Clear();
        }
    }

    private void ResolveMenuManager()
    {
        if (_menuManager?.Instance is not null)
        {
            return;
        }

        _menuManager = _shared.GetSharpModuleManager()
                              .GetOptionalSharpModuleInterface<IMenuManager>(IMenuManager.Identity);
        if (_menuManager?.Instance is null)
        {
            _logger.LogWarning("Install {AssemblyName} to display menus.", ProviderAssemblyName);
        }
    }

    private ECommandAction OnToggleMenuType(StringCommand command)
    {
        _useCachedMenu = !_useCachedMenu;
        _logger.LogInformation("Using {Type} menus.", _useCachedMenu ? "cached" : "dynamic");
        return ECommandAction.Stopped;
    }

    private void OnPlayerSpawned(IPlayerSpawnForwardParams param)
    {
        if (param.Client.IsFakeClient || _menuManager?.Instance is not { } manager)
        {
            return;
        }

        var client = param.Client;
        var menu = _useCachedMenu ? _cachedMenu : CreateDynamicMenu();
        manager.DisplayMenu(client, menu, out var sessionId);
        _sessions[client] = sessionId;

        _logger.LogInformation("Session {Session}: in menu={InMenu}; current menu={IsCurrent}",
            sessionId, manager.IsInMenu(client, menu), manager.IsInCurrentMenu(client, menu));

        // Capture only the wrapper: never retain a provider instance in a delayed callback.
        var provider = _menuManager;
        _shared.GetModSharp().PushTimer(() =>
        {
            // An earlier spawn's timer must not close a replacement menu or a new provider's session.
            if (ReferenceEquals(provider, _menuManager)
                && _sessions.TryGetValue(client, out var currentSession) && currentSession == sessionId)
            {
                if (client.IsValid && _menuManager?.Instance is { } currentManager
                    && currentManager.IsInMenu(client, sessionId))
                {
                    currentManager.QuitMenu(client);
                }

                _sessions.Remove(client);
            }
        }, 10.0);
    }

    private Menu CreateDynamicMenu()
    {
        var menu = new Menu();
        menu.SetTitle(client => client.IsAuthenticated ? "Main Menu (authenticated)" : "Main Menu");
        menu.AddSubMenu("Open Sub Menu", _cachedSubMenu);
        menu.AddItem("Give me a deagle!", OnGiveDeagle);
        menu.AddDisabledItem("Unavailable item");
        menu.AddItem("Generate next menu", controller =>
        {
            var next = Menu.Create()
                           .Title($"Weapons for slot {controller.Client.Slot}")
                           .Item("Give me a deagle!", OnGiveDeagle)
                           .Item(BuildAk47Item)
                           .BackItem()
                           .ExitItem()
                           .Build();
            controller.Next(next);
        });
        menu.AddItem(BuildAk47Item);
        menu.OnEnter = OnEnterMenu;
        menu.OnExit = OnExitMenu;
        return menu;
    }

    private void BuildAk47Item(IGameClient client, ref MenuItemContext context)
    {
        if (client.GetPlayerController()?.GetPlayerPawn() is not { } pawn)
        {
            return; // Without a title, the item is omitted. Use Spacer() for an empty line.
        }

        context.Title = "Give me an AK47! (T only)";
        if (!pawn.IsAlive || pawn.Team != CStrikeTeam.TE)
        {
            context.State = MenuItemState.Disabled;
            return;
        }

        context.Color = "#FFCCCB";
        context.Action = controller =>
        {
            // Native wrappers can become stale between drawing and selection: resolve again here.
            if (controller.Client.GetPlayerController()?.GetPlayerPawn() is { IsAlive: true, Team: CStrikeTeam.TE } currentPawn)
            {
                if (currentPawn.GiveNamedItem(EconItemId.Ak47) is null)
                {
                    controller.Client.Print(HudPrintChannel.Chat, "Could not give you an AK47.");
                }
            }

            controller.Exit();
        };
    }

    private void OnGiveDeagle(IMenuController controller)
    {
        if (controller.Client.GetPlayerController()?.GetPlayerPawn() is { IsAlive: true } pawn)
        {
            if (pawn.GiveNamedItem(EconItemId.Deagle) is null)
            {
                controller.Client.Print(HudPrintChannel.Chat, "Could not give you a deagle.");
            }
        }
        else
        {
            controller.Client.Print(HudPrintChannel.Chat, "You must be alive to receive a weapon.");
        }

        controller.Exit();
    }

    private void OnEnterMenu(IGameClient client)
    {
        if (_showStatusMessages && client.IsValid)
        {
            client.Print(HudPrintChannel.Chat, "Menu opened. This session closes after 10 seconds.");
        }
    }

    private void OnExitMenu(IGameClient client)
    {
        if (_showStatusMessages && client.IsValid)
        {
            client.Print(HudPrintChannel.Chat, "Menu closed.");
        }
    }

    public void Shutdown()
    {
        _shared.GetHookManager().PlayerSpawnPost.RemoveForward(OnPlayerSpawned);
        _shared.GetConVarManager().ReleaseServerCommandCallback("menu_type_toggle", OnToggleMenuType);
        // Close only sessions opened by this module, releasing menu delegates before unloading.
        if (_menuManager?.Instance is { } manager)
        {
            foreach (var (client, session) in _sessions)
            {
                if (client.IsValid && manager.IsInMenu(client, session))
                {
                    manager.QuitMenu(client);
                }
            }
        }

        _sessions.Clear();
        _menuManager = null;
    }

    public string DisplayName   => "Menu Example";
    public string DisplayAuthor => "ModSharp dev team";
}
