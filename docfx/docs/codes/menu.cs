using System;
using Microsoft.Extensions.Configuration;
using Sharp.Modules.MenuManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;

namespace MenuExample;

internal class MenuExample : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;
    private readonly Menu          _subMenu2;
    private readonly Menu          _menu;

    private IModSharpModuleInterface<IMenuManager>? _menuManager;

    public MenuExample(ISharedSystem sharedSystem,
        string                       dllPath,
        string                       sharpPath,
        Version                      version,
        IConfiguration               configuration,
        bool                         hotReload)
    {
        _sharedSystem = sharedSystem;

        // You can precache menu.
        _subMenu2 = Menu.Create()
                        .Title("Menu Title2")
                        .Description("Desc 2")
                        .Item("SubItem1",
                              controller =>
                              {
                                  controller.Client.GetPlayerController()?.Print(HudPrintChannel.Chat, "SubItem1 selected.");
                              })
                        .OnExit(cl => { })
                        .Build();

        _menu = Menu.Create()
                    .Title("Menu title")
                    .Description("Desc")
                    .Item("Item1",
                          controller =>
                          {
                              controller.Client.GetPlayerController()?.Print(HudPrintChannel.Chat, "Item1 selected.");
                              controller.Exit();
                          })
                    .Item("Item2",
                          controller =>
                          {
                              // Call Next will let you invoke next menu.
                              controller.Next(_subMenu2);
                              controller.Client.GetPlayerController()?.Print(HudPrintChannel.Chat, "Item2 Selected.");
                          })
                    .OnEnter(cl => { cl.GetPlayerController()?.Print(HudPrintChannel.Chat, "OnEnter"); })
                    .OnExit(cl => { cl.GetPlayerController()?.Print(HudPrintChannel.Chat,  "OnExit"); })
                    .Build();
    }

    public bool Init()
    {
        _sharedSystem.GetHookManager().PlayerSpawnPost.InstallForward(OnPlayerSpawnPost);

        return true;
    }

    private void OnPlayerSpawnPost(IPlayerSpawnForwardParams obj)
    {
        if (_menuManager?.Instance is not { } menuManager)
        {
            return;
        }

        var client           = obj.Client;
        var playerController = obj.Controller;

        menuManager.DisplayMenu(client, _menu);

        // Or create menu on the fly.
        //var subMenu2 = Menu.Create()
        //    .Title("Menu Title2")
        //    .Description("Desc 2")
        //    .Item("SubItem1", controller =>
        //    {
        //        controller.Client.PrintToChat("SubItem1 selected.");
        //    })
        //    .OnExit(cl =>
        //    {

        //    })
        //    .Build();
        //var menu = Menu.Create()
        //    .Title("Menu title")
        //    .Description("Desc")
        //    .Item("Item1", controller =>
        //    {
        //        controller.Client.GetPlayerController()?.Print(HudPrintChannel.Chat, "Item1 selected.");
        //        controller.Exit();
        //    })
        //    .Item("Item2", controller =>
        //    {
        //        // Call Next will let you invoke next menu.
        //        controller.Next(subMenu2);
        //        controller.Client.GetPlayerController()?.Print(HudPrintChannel.Chat, "Item2 Selected.");
        //    })
        //    .OnEnter(cl =>
        //    {
        //        cl.PrintToChat("OnEnter");
        //    })
        //    .OnExit(cl =>
        //    {
        //        cl.PrintToChat("OnExit");
        //    })
        //    .Build();
        //menuManager.DisplayMenu(client, menu);

        if (menuManager.IsInMenu(client))
        {
            playerController.Print(HudPrintChannel.Chat, "You now should be in menu.");
        }

        playerController.Print(HudPrintChannel.Chat, "Will quit menu after 10 seconds.");

        // This will quit menu after 10 seconds.
        _sharedSystem.GetModSharp()
                     .PushTimer(() =>
                                {
                                    // Must validate before you quit, otherwise will throw exception.
                                    if (menuManager.IsInMenu(client))
                                    {
                                        menuManager.QuitMenu(client);
                                    }
                                },
                                10.0);
    }

    public void OnAllModulesLoaded()
    {
        _menuManager = _sharedSystem.GetSharpModuleManager()
                                    .GetRequiredSharpModuleInterface<IMenuManager>(IMenuManager.Identity);
    }

    public void Shutdown()
    {
        _sharedSystem.GetHookManager().PlayerSpawnPost.RemoveForward(OnPlayerSpawnPost);
    }

    public string DisplayName   => "MenuExample";
    public string DisplayAuthor => "ModSharp Dev Team";
}
