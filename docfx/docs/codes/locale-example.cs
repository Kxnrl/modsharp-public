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
    private const string ProviderAssemblyName = "Sharp.Modules.LocalizerManager";
    private readonly IModSharp _modSharp;
    private readonly IHookManager _hooks;
    private readonly ISharpModuleManager _modules;
    private IModSharpModuleInterface<ILocalizerManager>? _cachedInterface;

    public LocaleExample(ISharedSystem sharedSystem,
        string                         dllPath,
        string                         sharpPath,
        Version                        version,
        IConfiguration                 coreConfiguration,
        bool                           hotReload)
    {
        _modSharp = sharedSystem.GetModSharp();
        _hooks = sharedSystem.GetHookManager();
        _modules = sharedSystem.GetSharpModuleManager();
    }

    public bool Init()
    {
        _hooks.PlayerSpawnPost.InstallForward(OnPlayerSpawned);
        return true;
    }

    public void OnAllModulesLoaded()
        => ResolveLocalizer();

    public void OnLibraryConnected(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            ResolveLocalizer();
        }
    }

    public void OnLibraryDisconnect(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _cachedInterface = null;
        }
    }

    private void ResolveLocalizer()
    {
        if (_cachedInterface?.Instance is not null)
        {
            return;
        }

        _cachedInterface = _modules.GetOptionalSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity);
        // Load again when the provider is replaced: its new instance has a fresh locale dictionary.
        _cachedInterface?.Instance?.LoadLocaleFile("locale-example");
    }

    public void Shutdown()
    {
        _hooks.PlayerSpawnPost.RemoveForward(OnPlayerSpawned);
        _cachedInterface = null;
    }

    private void OnPlayerSpawned(IPlayerSpawnForwardParams param)
    {
        if (param.Client.IsFakeClient || _cachedInterface?.Instance is not { } localizer)
        {
            return;
        }

        // Use localizer.ForMany(clients) to render separately for multiple clients.
        // Locale views and builders are short-lived; do not cache them across provider reloads.
        var locale = localizer.For(param.Client);
        var controller = param.Controller;
        controller.Print(HudPrintChannel.Chat, $"Client culture: {locale.Culture.Name}");
        controller.Print(HudPrintChannel.Chat, $"Hello => {locale.Text("Hello")}");
        controller.Print(HudPrintChannel.Chat, $"World => {locale.Text("World", param.Client.Name)}");
        controller.Print(HudPrintChannel.Chat, $"Time => {locale.Text("Time", _modSharp.GetGlobals().CurTime)}");
        controller.Print(HudPrintChannel.Chat, $"Date => {locale.Text("Date", DateTime.Now)}");

        locale.Localized("Generic.HelloWorld")
              .Prefix("[Example]")
              .Transform(ToAlternatingCase)
              .Print();
    }

    private static string ToAlternatingCase(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = i % 2 == 0 ? char.ToUpperInvariant(chars[i]) : char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }

    public string DisplayName   => "Locale Example";
    public string DisplayAuthor => "ModSharp dev team";
}
