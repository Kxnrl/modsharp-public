using System;
using Microsoft.Extensions.Configuration;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Managers;

namespace LocaleExample;

public sealed class LocaleExample : IModSharpModule
{
    private readonly IModSharp           _modSharp;
    private readonly IHookManager        _hooks;
    private readonly ISharpModuleManager _modules;

    public LocaleExample(ISharedSystem sharedSystem,
        string                         dllPath,
        string                         sharpPath,
        Version                        version,
        IConfiguration                 coreConfiguration,
        bool                           hotReload)
    {
        _modSharp = sharedSystem.GetModSharp();
        _hooks    = sharedSystem.GetHookManager();
        _modules  = sharedSystem.GetSharpModuleManager();
    }

    public bool Init()
    {
        // install hook
        _hooks.PlayerSpawnPost.InstallForward(OnPlayerSpawned);

        return true;
    }

    public void Shutdown()
    {
        // must remove the hooks in Shutdown
        // otherwise you will get fucked after reloaded.
        _hooks.PlayerSpawnPost.RemoveForward(OnPlayerSpawned);
    }

    public void OnAllModulesLoaded()
    {
        // load the locale file
        GetInterface()?.LoadLocaleFile("locale-example");
    }

    private void OnPlayerSpawned(IPlayerSpawnForwardParams param)
    {
        if (GetInterface() is not { } lm)
        {
            return;
        }

        var localizer = lm.GetLocalizer(param.Client);

        // also can 
        // localizer = lm[param.Client];

        var controller = param.Controller;

        controller.Print(HudPrintChannel.Chat, $"Client culture: {localizer.Culture.Name}");

        if (localizer.TryGet("Generic.HelloWorld") is { } message)
        {
            controller.Print(HudPrintChannel.Chat, $"Key = \"Generic.HelloWorld\" Value = \"{message}\"");
        }

        var name = param.Client.Name;
        var time = _modSharp.GetGlobals().CurTime;
        var date = DateTime.Now;

        var hello = localizer.Format("Hello");          // with culture
        var world = localizer.FormatRaw("World", name); // without culture

        controller.Print(HudPrintChannel.Chat, $"Hello => {hello}");
        controller.Print(HudPrintChannel.Chat, $"World => {world}");
        controller.Print(HudPrintChannel.Chat, $"Time => {localizer.Format("Time", time)}");
        controller.Print(HudPrintChannel.Chat, $"Date => {localizer.Format("Date", date)}");
        controller.Print(HudPrintChannel.Chat, $"Generic.HelloWorld => {localizer.Format("Generic.HelloWorld")}");
    }

    private IModSharpModuleInterface<ILocalizerManager>? _cachedInterface;

    // this may have performance issue if you call it too frequently,
    // the bast way is cache with 'OnAllModulesLoaded'/'OnLibraryConnected' and clear with 'OnLibraryDisconnect' manually
    private ILocalizerManager? GetInterface()
    {
        if (_cachedInterface?.Instance is null)
        {
            _cachedInterface = _modules.GetOptionalSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity);
        }

        return _cachedInterface?.Instance;
    }

    public string DisplayName   => "Locale Example";
    public string DisplayAuthor => "ModSharp dev team";
}
