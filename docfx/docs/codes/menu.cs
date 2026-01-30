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

    private IMenuManager? _menuManager;

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
                             .Cursor(">>", "<<") // Custom cursor
                             .Item("Back", controller => controller.GoBack())
                             .Build();

        _cachedMenu = Menu.Create()
                          .Title("Main Menu (Cached)")
                          .HideIndex() // Hide item indices, so it wont display "1.", "2." before the item name
                          .Item("Open Sub Menu",     OnMenuItemOpenSubMenu)
                          .Item("Give me a deagle!", OnMenuItemGiveDeagle)
                          .Item(gameClient =>
                                {
                                    // you can use LocalizeManager to get the localized title if you want to
                                    if (gameClient.IsAuthenticated)
                                    {
                                        return "Give me a deagle! (authenticated)";
                                    }

                                    return "Give me a deagle..?";
                                },
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
        // we call it here just to prevent it fails to find **MenuManager** after our module is reloaded
        // this is because OnAllModulesLoaded is only called once when every module is loaded at start up
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
        if (name.Equals(MenuManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _menuManager = null;
        }
    }

    public void OnAllModulesLoaded()
    {
        // we also call it here and see if the end user(module consumer) has menu manager installed, so we can inform them if they don't.
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
        if (_menuManager is not { } menuManager)
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

        // we stop displaying the menu after 10 seconds
        _sharedSystem.GetModSharp()
                     .PushTimer(() =>
                                {
                                    // Must validate before calling QuitMenu, otherwise it will throw exception.
                                    if (menuManager.IsInMenu(client))
                                    {
                                        menuManager.QuitMenu(client);
                                    }
                                },
                                10.0);
    }

    private void CreateAndDisplayMenuOnTheFly(IGameClient client, IMenuManager menuManager)
    {
        // example 2
        var menu = Menu.Create().Build();

        // or this way
        // var menu = new Menu();
        menu.SetTitle(gameClient =>
        {
            // you can use LocalizeManager to get the localized title if you want to
            return gameClient.IsAuthenticated ? "Main menu (authenticated)" : "Main menu";
        });

        // or if you prefer the simplest way
        // menu.SetTitle("My menu title");

        menu.AddItem("Open sub menu",     OnMenuItemOpenSubMenu);
        menu.AddItem("Give me a deagle!", OnMenuItemGiveDeagle);

        menu.AddItem(gameClient =>
                     {
                         // you can use LocalizeManager to get the localized title if you want to
                         return gameClient.IsAuthenticated ? "Give me a deagle! (authenticated)" : "Give me a deagle..?";
                     },

                     // if the action, OnMenuItemGiveDeagle in this case, is null,
                     // this item will be treated as Disabled
                     client.IsAuthenticated ? OnMenuItemGiveDeagle : null);

        menu.AddItem(OnMenuItemGiveAk47);
        menu.OnExit = OnExitMenu;

        menuManager.DisplayMenu(client, menu);
    }

    private void OnMenuItemOpenSubMenu(IMenuController controller)
    {
        controller.Next(_cachedSubMenu);
    }

    private void OnExitMenu(IGameClient cl)
    {
        cl.GetPlayerController()?.Print(HudPrintChannel.Chat, "Menu closed.");
    }

    private void OnMenuItemGiveAk47(IGameClient client, ref MenuItemContext context)
    {
        // a more flexible way to add a menu item

        if (client.GetPlayerController()?.GetPlayerPawn() is not { } playerPawn)
        {
            // if we return here, this item won't be added, because .Title is null
            // and .State is MenuItemState.Default; the purpose of this is to prevent ghost item...
            // if you want to add a spacer/padding item please do as the following code
            // context.State = MenuItemState.Spacer;
            return;
        }

        // you can use LocalizeManager to get the localized value
        context.Title = "Give me an AK47!";

        // player is not on Terrorist team, we don't allow that
        if (playerPawn.Team != CStrikeTeam.TE)
        {
            context.State = MenuItemState.Disabled;
        }
        else
        {
            // we can change the color here.
            // PS: it does not work for disabled item
            context.Color = "#FFCCCB";
        }

        if (!playerPawn.IsAlive)
        {
            // or we just don't set the action here, so this item
            // can be treated as disabled
            // note: if the player respawns and want to use this item they have to reopen it again
            return;
        }

        context.Action = menuController =>
        {
            if (!playerPawn.IsAlive)
            {
                playerPawn.Print(HudPrintChannel.Chat,
                                 "You are not alive, so no weapon for you haha!");

                goto exit;
            }

            if (playerPawn.GiveNamedItem(EconItemId.Ak47) is null)
            {
                playerPawn.Print(HudPrintChannel.Chat,
                                 "Can't give you an AK47 for some reason...?");
            }

        exit:
            menuController.Exit();
        };
    }

    private void OnMenuItemGiveDeagle(IMenuController controller)
    {
        // simple action example, this should cover most of the use cases
        // noted that this action code is only called when the player selects this item
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
        if (_menuManager is not null)
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

    private T? GetExternalModule<T>(string identity) where T : class
        => _sharedSystem.GetSharpModuleManager()
                        .GetOptionalSharpModuleInterface<T>(identity)
                        ?.Instance;
}
