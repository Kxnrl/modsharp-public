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
        GetLocalization()?.LoadLocaleFile("locale-example");
    }

    private void OnPlayerSpawned(IPlayerSpawnForwardParams param)
    {
        if (GetLocalization() is not { } loc)
        {
            return;
        }

        // or ues loc.ForMany(clients); if you want to print to multiple players
        var locale     = loc.For(param.Client);
        var controller = param.Controller;

        controller.Print(HudPrintChannel.Chat, $"Client culture: {locale.Culture.Name}");

        var name = param.Client.Name;
        var time = _modSharp.GetGlobals().CurTime;
        var date = DateTime.Now;

        controller.Print(HudPrintChannel.Chat, $"Hello => {locale.Text("Hello")}");
        controller.Print(HudPrintChannel.Chat, $"World => {locale.Text("World", name)}");
        controller.Print(HudPrintChannel.Chat, $"Time => {locale.Text("Time", time)}");
        controller.Print(HudPrintChannel.Chat, $"Date => {locale.Text("Date", date)}");
        controller.Print(HudPrintChannel.Chat, $"Generic.HelloWorld => {locale.Text("Generic.HelloWorld")}");

        // Builder with prefix & post-processing (example: alternating upper/lower to show transform)
        locale.Localized("Generic.HelloWorld")
              .WithPrefix("[Example]")
              .Transform(ToAlternatingCase)
              .Print();
    }

    private IModSharpModuleInterface<ILocalizerManager>? _cachedInterface;

    private ILocalizerManager? GetLocalization()
    {
        if (_cachedInterface?.Instance is null)
        {
            _cachedInterface = _modules.GetOptionalSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity);
        }

        return _cachedInterface?.Instance;
    }

    private static string ToAlternatingCase(string text)
    {
        var chars = text.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            chars[i] = i % 2 == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
        }

        return new string(chars);
    }

    public string DisplayName   => "Locale Example";
    public string DisplayAuthor => "ModSharp dev team";
}
