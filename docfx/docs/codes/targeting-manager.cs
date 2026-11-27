using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace TargetingManagerExample;

internal sealed class AimTargetResolver : ITargetResolver
{
    // @aim is already built in; use a unique name for a custom resolver.
    public const string TargetString = "@example_aim";
    private readonly ISharedSystem _shared;

    public AimTargetResolver(ISharedSystem shared)
        => _shared = shared;

    public string GetTarget()
        => TargetString;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
    {
        if (activator?.GetPlayerController()?.GetPlayerPawn() is not { IsAlive: true } pawn)
        {
            return [];
        }

        var start = pawn.GetEyePosition();
        var end = start + (pawn.GetEyeAngles().AnglesToVectorForward() * 8192.0f);
        var attributes = RnQueryShapeAttr.Bullets();
        attributes.SetEntityToIgnore(pawn, 0);

        var trace = _shared.GetPhysicsQueryManager().TraceLine(start, end, attributes);
        if (!trace.DidHit() || trace.Entity == 0)
        {
            return [];
        }

        var entity = _shared.GetEntityManager().MakeEntityFromPointer<IBaseEntity>(trace.Entity);
        if (entity.AsPlayerPawn() is { IsAlive: true } target
            && target.GetControllerAuto()?.GetGameClient() is { } client)
        {
            return [client];
        }

        return [];
    }
}

public sealed class TargetingManagerExample : IModSharpModule
{
    private const string ProviderAssemblyName = "Sharp.Modules.TargetingManager";
    private static readonly string ModuleIdentity = typeof(TargetingManagerExample).Assembly.GetName().Name
                                                     ?? "TargetingManagerExample";

    private readonly ISharedSystem _shared;
    private readonly ILogger<TargetingManagerExample> _logger;
    private IModSharpModuleInterface<ITargetingManager>? _targetingManager;

    public TargetingManagerExample(ISharedSystem sharedSystem,
        string                                  dllPath,
        string                                  sharpPath,
        Version                                 version,
        IConfiguration                          configuration,
        bool                                    hotReload)
    {
        _shared = sharedSystem;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<TargetingManagerExample>();
    }

    public bool Init()
        => true;

    public void OnAllModulesLoaded()
        => ResolveTargetingManager();

    public void OnLibraryConnected(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            ResolveTargetingManager();
        }
    }

    public void OnLibraryDisconnect(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            _targetingManager = null;
        }
    }

    private void ResolveTargetingManager()
    {
        if (_targetingManager?.Instance is not null)
        {
            return;
        }

        _targetingManager = _shared.GetSharpModuleManager()
                                  .GetOptionalSharpModuleInterface<ITargetingManager>(ITargetingManager.Identity);
        if (_targetingManager?.Instance is not { } manager)
        {
            _logger.LogWarning("Install {AssemblyName} to enable {Target}.", ProviderAssemblyName, AimTargetResolver.TargetString);
            return;
        }

        if (!manager.RegisterResolver(ModuleIdentity, new AimTargetResolver(_shared)))
        {
            _logger.LogWarning("Target {Target} is already registered.", AimTargetResolver.TargetString);
        }
    }

    public void Shutdown()
    {
        // TargetingManager removes resolvers owned by this AssemblyName on library disconnect.
        _targetingManager = null;
    }

    public string DisplayName   => "Targeting Manager Example";
    public string DisplayAuthor => "ModSharp dev team";
}
