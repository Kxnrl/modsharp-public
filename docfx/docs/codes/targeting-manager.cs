using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace TargetingManagerExample;

internal class AimTargetResolver : ITargetResolver
{
    public const     string        TargetString = "@aim";
    private readonly ISharedSystem _shared;

    // you can pass the class/interface you need for your resolver here
    public AimTargetResolver(ISharedSystem shared)
        => _shared = shared;

    public string GetTarget()
        => TargetString;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
    {
        // the issuer is console, ignore!
        if (activator is null)
        {
            return [];
        }

        // the issuer is a player, but we also return empty if we
        // 1. can't get its player controller
        // 2. can't get its player pawn
        // 3. is not alive
        if (activator.GetPlayerController()?.GetPlayerPawn() is not { IsAlive: true } pawn)
        {
            return [];
        }

        var start = pawn.GetEyePosition();
        var fwd   = pawn.GetEyeAngles().AnglesToVectorForward();
        var end   = start + (fwd * 8192.0f);

        var attr = RnQueryShapeAttr.Bullets();

        // NOTE: we must ignore the issuer, otherwise the trace will hit themselves!!
        attr.SetEntityToIgnore(pawn, 0);

        var trace = _shared.GetPhysicsQueryManager().TraceLine(start, end, attr);

        // return false if we didn't hit anything
        if (!trace.DidHit())
        {
            return [];
        }

        // IsPlayerPawn will check if the entity is player
        if (_shared.GetEntityManager().MakeEntityFromPointer<IPlayerPawn>(trace.Entity) is not { IsPlayerPawn: true } tracePawn)
        {
            return [];
        }

        if (tracePawn.GetControllerAuto() is not { IsValidEntity: true } traceController)
        {
            return [];
        }

        // NOTE: even if controller is a valid entity, if the corresponding IGameClient does not exist, it will still return null 
        if (traceController.GetGameClient() is { } traceClient)
        {
            return [traceClient];
        }

        return [];
    }
}

internal class TargetingManagerExample : IModSharpModule
{
    private static readonly string AssemblyName = typeof(TargetingManagerExample).Assembly.GetName().Name
                                                  ?? "TargetingManagerExample";

    private const string TargetingManagerAssemblyName = "Sharp.Modules.TargetingManager";

    public string DisplayName   => "Targeting manager example";
    public string DisplayAuthor => "Modsharp dev team";

    private readonly ISharedSystem                    _shared;
    private readonly ILogger<TargetingManagerExample> _logger;

    private ITargetingManager? _targetingManager;
    private bool               _registered = false;

    public TargetingManagerExample(ISharedSystem  shared,
                                   string         dllPath,
                                   string         sharpPath,
                                   Version        version,
                                   IConfiguration configuration,
                                   bool           hotReload)
    {
        _shared = shared;
        _logger = shared.GetLoggerFactory().CreateLogger<TargetingManagerExample>();
    }

    public bool Init()
        => true;

    public void PostInit()
    {
        // we call it here just to prevent it fails to find TargetingManager after our module is reloaded
        // this is because OnAllModulesLoaded is only called once when every module is loaded at start up
        TryResolveTargetingManager();
    }

    public void OnLibraryConnected(string name)
    {
        if (name.Equals(TargetingManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            TryResolveTargetingManager();
        }
    }

    public void OnLibraryDisconnect(string name)
    {
        if (name.Equals(TargetingManagerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _targetingManager = null;
            _registered       = false;
        }
    }

    public void OnAllModulesLoaded()
    {
        // we also call it here and see if the end user(module consumer) has targeting manager installed, so we can inform them about this.
        TryResolveTargetingManager(true);
    }

    public void Shutdown()
    {
    }

    private void TryResolveTargetingManager(bool logFailure = false)
    {
        if (_targetingManager is not null)
        {
            return;
        }

        _targetingManager = GetExternalModule<ITargetingManager>(ITargetingManager.Identity);

        if (_targetingManager is null)
        {
            if (logFailure)
            {
                _logger.LogWarning("Failed to get TargetingManager. Do you have '{AssemblyName}' installed? Target selectors will be limited.",
                                   TargetingManagerAssemblyName);
            }
        }
        else
        {
            RegisterTargetResolver();
        }
    }

    private void RegisterTargetResolver()
    {
        if (_targetingManager is null || _registered)
        {
            return;
        }

        // stop if the target string is already registered
        if (!_targetingManager.RegisterResolver(AssemblyName, new AimTargetResolver(_shared)))
        {
            return;
        }

        _registered = true;
    }

    private T? GetExternalModule<T>(string identity) where T : class
        => _shared.GetSharpModuleManager()
                  .GetOptionalSharpModuleInterface<T>(identity)
                  ?.Instance;
}
