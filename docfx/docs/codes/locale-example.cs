using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Extensions.CommandManager;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Abstractions;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace LocaleExample;

public class LocaleExample : IModSharpModule
{
    private ILocalizerManager _localizerManager = null!;

    private readonly ISharedSystem _sharedSystem;
    private readonly IServiceProvider _provider;
    private readonly ICommandManager _commandManager;
    public LocaleExample(
        ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration coreConfiguration,
        bool hotReload)
    {

        var services = new ServiceCollection();
        _sharedSystem = sharedSystem;
        services.AddCommandManager(sharedSystem);
        services.AddLogging(p => { p.ClearProviders(); });

        _provider = services.BuildServiceProvider();
        _commandManager = _provider.GetRequiredService<ICommandManager>();
    }

    public string DisplayName => "LocaleExample";

    public string DisplayAuthor => "1";

    public bool Init() => true;

    public void PostInit()
    {
        _provider.LoadAllSharpExtensions();

        _commandManager.RegisterClientCommand("locale", OnCommandLocale);
    }

    private void OnCommandLocale(IGameClient client, StringCommand command)
    {
        var clientLocalizer = _localizerManager.GetLocalizer(client);

        var message = clientLocalizer.TryGet("Generic.HelloWorld");

        var controller = _sharedSystem.GetEntityManager().FindPlayerControllerBySlot(client.Slot);

        controller?.Print(HudPrintChannel.Chat, $"Client culture: {clientLocalizer.Culture.Name}");
        controller?.Print(HudPrintChannel.Chat, $"{message}");
        var paramA = clientLocalizer.TryGet("PhraseA.ParamA") ?? string.Empty;
        var phraseA = clientLocalizer.Format("PhraseA", [paramA]);
        controller?.Print(HudPrintChannel.Chat, $"{phraseA}");
    }

    public void OnAllModulesLoaded()
    {
        _localizerManager = _sharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity).Instance!;

        _localizerManager.LoadLocaleFile("locale-example");
    }

    public void Shutdown()
    {
        _provider.ShutdownAllSharpExtensions();
    }
}
