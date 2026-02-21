using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace MenuExample;

internal class MenuExample : IModSharpModule
{
    private const string MenuManagerAssemblyName = "Sharp.Modules.MenuManager";

    private readonly ISharedSystem        _sharedSystem;
    private readonly ILogger<MenuExample> _logger;

    private readonly Menu _cachedSubMenu;
    private readonly Menu _cachedMenu;

    private bool _useCacheMenu = true;

    private IModSharpModuleInterface<IMenuManager>? _menuManager;

    public MenuExample(ISharedSystem  sharedSystem,
                       string         dllPath,
                       string         sharpPath,
                       Version        version,
                       IConfiguration configuration,
                       bool           hotReload)
    {
        _sharedSystem = sharedSystem;
        _logger       = sharedSystem.GetLoggerFactory().CreateLogger<MenuExample>();

        // Example 1: Cached Menu (Recommended for static menus)
        // You can precache menu in constructor.
        _cachedSubMenu = Menu.Create()
                             .Title("Sub Menu")
                             .Cursor("»", "«") // Custom cursor (avoid raw < > which conflict with HTML tags)
                             .DisabledItem("This item is not selectable")
                             .Spacer()
                             .Item("Do something", _ => { /* ... */ })
                             .BackItem()  // Navigates back to the parent menu, or exits if none
                             .ExitItem()  // Closes the menu entirely
                             .Build();

        _cachedMenu = Menu.Create()
                          .Title("Main Menu (Cached)")
                          .HideIndex() // Hide item indices, so it wont display "1.", "2." before the item name
                          .SubMenu("Open Sub Menu",   _cachedSubMenu)
                          .Item("Give me a deagle!", OnMenuItemGiveDeagle)
                          .Item(gameClient =>
                                    // Title factory — useful for localized titles via LocalizeManager
                                    gameClient.IsAuthenticated
                                        ? "Give me a deagle! (authenticated)"
                                        : "Give me a deagle..?",
                                OnMenuItemGiveDeagle)
                          .Item(OnMenuItemGiveAk47)
                          .OnExit(OnExitMenu)
                          .Build();
    }

    public bool Init()
    {
        _sharedSystem.GetHookManager().PlayerSpawnPost.InstallForward(OnPlayerSpawnPost);
        _sharedSystem.GetConVarManager().CreateServerCommand("menu_type_toggle", OnCommandMenuTypeToggle);

        return true;
    }

    public void PostInit()
    {
        // Also resolve here in case our module is hot-reloaded after initial startup,
        // since OnAllModulesLoaded is only called once at first load.
        TryResolveMenuManager();
    }

    public void OnLibraryConnected(string name)
    {
        if (name.Equals(MenuManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            TryResolveMenuManager();
        }
    }

    public void OnLibraryDisconnect(string name)
    {
        // No need to null out _menuManager here — the framework sets .Instance to null
        // when the module is unloaded via IModSharpModuleInterface<T>.Dispose().
    }

    public void OnAllModulesLoaded()
    {
        // Try again here and warn if MenuManager is not installed
        TryResolveMenuManager(true);
    }

    public void Shutdown()
    {
        _sharedSystem.GetHookManager().PlayerSpawnPost.RemoveForward(OnPlayerSpawnPost);
        _sharedSystem.GetConVarManager().ReleaseCommand("menu_type_toggle");
    }

    public string DisplayName   => "MenuExample";
    public string DisplayAuthor => "ModSharp Dev Team";

    private ECommandAction OnCommandMenuTypeToggle(StringCommand arg)
    {
        _useCacheMenu = !_useCacheMenu;
        Console.Write("[Menu Example] we are ");
        Console.WriteLine(_useCacheMenu ? "using cached menu" : "creating menu on the fly");

        return ECommandAction.Handled;
    }

    private void OnPlayerSpawnPost(IPlayerSpawnForwardParams obj)
    {
        if (_menuManager?.Instance is not { } menuManager)
        {
            return;
        }

        var client = obj.Client;

        // Method 1: Display the cached menu
        if (_useCacheMenu)
        {
            menuManager.DisplayMenu(client, _cachedMenu);
        }
        else
        {
            CreateAndDisplayMenuOnTheFly(client, menuManager);
        }

        obj.Controller.Print(HudPrintChannel.Chat, "Menu opened, it will be closed in 10 seconds");

        // Auto-close the menu after 10 seconds
        _sharedSystem.GetModSharp()
                     .PushTimer(() =>
                                {
                                    // Must check IsInMenu before calling QuitMenu
                                    if (menuManager.IsInMenu(client))
                                    {
                                        menuManager.QuitMenu(client);
                                    }
                                },
                                10.0);
    }

    private void CreateAndDisplayMenuOnTheFly(IGameClient client, IMenuManager menuManager)
    {
        // Example 2: Imperative menu construction (useful when items depend on runtime state)
        var menu = new Menu();

        // Title factory — useful for localized titles via LocalizeManager
        menu.SetTitle(gameClient =>
            gameClient.IsAuthenticated ? "Main menu (authenticated)" : "Main menu");

        // or if you prefer a static title
        // menu.SetTitle("My menu title");

        menu.AddSubMenu("Open sub menu",  _cachedSubMenu);
        menu.AddItem("Give me a deagle!", OnMenuItemGiveDeagle);

        // Disabled item — visible but not selectable
        menu.AddDisabledItem("Coming soon...");

        menu.AddItem(gameClient =>
                         gameClient.IsAuthenticated ? "Give me a deagle! (authenticated)" : "Give me a deagle..?",
                     // If the action is null, the item will be treated as Disabled
                     client.IsAuthenticated ? OnMenuItemGiveDeagle : null);

        // Generator-based item for full control over title, state, color, and action
        menu.AddItem(OnMenuItemGiveAk47);
        menu.OnExit = OnExitMenu;

        menuManager.DisplayMenu(client, menu);
    }

    private void OnExitMenu(IGameClient cl)
    {
        cl.GetPlayerController()?.Print(HudPrintChannel.Chat, "Menu closed.");
    }

    private void OnMenuItemGiveAk47(IGameClient client, ref MenuItemContext context)
    {
        // Generator-based item: full control over the MenuItemContext.
        // This is useful when the item's title, state, or action depends on the player's state.

        if (client.GetPlayerController()?.GetPlayerPawn() is not { } playerPawn)
        {
            // Returning without setting Title causes the item to be skipped entirely.
            // To add an empty line instead, use: context.State = MenuItemState.Spacer;
            return;
        }

        context.Title = "Give me an AK47!";

        if (playerPawn.Team != CStrikeTeam.TE)
        {
            // Disabled items are visible but cannot be selected
            context.State = MenuItemState.Disabled;
        }
        else
        {
            // Custom color (does not apply to disabled items)
            context.Color = "#FFCCCB";
        }

        if (!playerPawn.IsAlive)
        {
            // Not setting Action leaves the item disabled automatically
            return;
        }

        context.Action = menuController =>
        {
            if (playerPawn.IsAlive)
            {
                if (playerPawn.GiveNamedItem(EconItemId.Ak47) is null)
                {
                    playerPawn.Print(HudPrintChannel.Chat,
                                     "Can't give you an AK47 for some reason...?");
                }
            }
            else
            {
                playerPawn.Print(HudPrintChannel.Chat,
                                 "You are not alive, so no weapon for you haha!");
            }

            menuController.Exit();
        };
    }

    private void OnMenuItemGiveDeagle(IMenuController controller)
    {
        // Simple action example — this pattern covers most use cases.
        // Action code only runs when the player selects this item.
        if (controller.Client.GetPlayerController()?.GetPlayerPawn() is not { } playerPawn)
        {
            controller.Exit();

            return;
        }

        if (!playerPawn.IsAlive)
        {
            playerPawn.Print(HudPrintChannel.Chat,
                             "You are not alive, so no weapon for you haha!");

            controller.Exit();

            return;
        }

        if (playerPawn.GiveNamedItem(EconItemId.Deagle) is null)
        {
            playerPawn.Print(HudPrintChannel.Chat,
                             "Can't give you a deagle for some reason...?");
        }

        controller.Exit();
    }

    private void TryResolveMenuManager(bool logFailure = false)
    {
        // Re-resolve if the wrapper is null or the instance was disposed (e.g. after hot-reload)
        if (_menuManager?.Instance is not null)
        {
            return;
        }

        _menuManager = GetExternalModule<IMenuManager>(IMenuManager.Identity);

        if (_menuManager is null)
        {
            if (logFailure)
            {
                _logger.LogWarning("Failed to get MenuManager. Do you have '{AssemblyName}' installed? Target selectors will be limited.",
                                   MenuManagerAssemblyName);
            }
        }
    }

    private IModSharpModuleInterface<T>? GetExternalModule<T>(string identity) where T : class
        => _sharedSystem.GetSharpModuleManager()
                        .GetOptionalSharpModuleInterface<T>(identity);
}
