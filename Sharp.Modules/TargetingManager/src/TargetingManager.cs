// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable UnusedParameter.Local

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.TargetingManager.BuiltinResolvers;
using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.TargetingManager;

internal sealed class TargetingManager : IModSharpModule, ITargetingManager
{
    private readonly ILogger<TargetingManager> _logger;
    private readonly ISharedSystem             _sharedSystem;
    private readonly IClientManager            _clientManager;

    private readonly Dictionary<string, (string Owner, ITargetResolver Resolver)> _targetResolvers
        = new (StringComparer.OrdinalIgnoreCase);

#region IModSharpModule

    public TargetingManager(
        ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration coreConfiguration,
        bool hotReload)
    {
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<TargetingManager>();

        var coreId = typeof(TargetingManager).Assembly.GetName().Name ?? "Sharp.Modules.TargetingManager";

        _sharedSystem = sharedSystem;
        var clientManager = sharedSystem.GetClientManager();
        _clientManager = clientManager;

        RegisterResolver(coreId, PredefinedTargets.Alive, new Alive(clientManager));
        RegisterResolver(coreId, PredefinedTargets.All,   new All(clientManager));
        RegisterResolver(coreId, PredefinedTargets.Bots,  new Bots(clientManager));
        RegisterResolver(coreId, PredefinedTargets.Ct,    new Ct(clientManager));
        RegisterResolver(coreId, PredefinedTargets.Dead,  new Dead(clientManager));
        RegisterResolver(coreId, PredefinedTargets.Me,    new Me(clientManager));
        RegisterResolver(coreId, PredefinedTargets.NotMe, new NotMe(clientManager));
        RegisterResolver(coreId, PredefinedTargets.Spec,  new Spec(clientManager));
        RegisterResolver(coreId, PredefinedTargets.T,     new T(clientManager));
    }


    public bool Init()
    {
        return true;
    }

    public void PostInit()
    {
        _sharedSystem.GetSharpModuleManager()
            .RegisterSharpModuleInterface<ITargetingManager>(this, ITargetingManager.Identity, this);
    }

    public void OnLibraryDisconnect(string moduleIdentity)
    {
        var keys = _targetResolvers
                   .Where(x => x.Value.Owner == moduleIdentity)
                   .Select(x => x.Key)
                   .ToList();

        if (keys.Count == 0)
        {
            return;
        }

        foreach (var key in keys)
        {
            _targetResolvers.Remove(key);
        }

        _logger.LogInformation("Removed {Count} target resolvers registered by '{Module}'.", keys.Count, moduleIdentity);
    }

    public void Shutdown()
    {
    }

    string IModSharpModule.DisplayName   => "Sharp.Modules.TargetingManager";
    string IModSharpModule.DisplayAuthor => "laper32";

    #endregion

    #region ITargetingManager

    public IEnumerable<IGameClient> GetByTarget(IGameClient? activator, string target)
    {
        if (_targetResolvers.TryGetValue(target, out var resolver))
        {
            return resolver.Resolver.Resolve(activator);
        }

        if (target.Length == 17 && SteamID.TryParse(target, out var steamId))
        {
            if (_clientManager.GetGameClient(steamId) is { } client)
            {
                return [client];
            }
        }

        return [];
    }

    public bool RegisterResolver(string ownerIdentity, string target, ITargetResolver resolver)
    {
        if (_targetResolvers.TryGetValue(target, out var existingEntry))
        {
            _logger.LogError("Failed to register target '{target}'. It is already registered by '{owner}'. Request from '{newOwner}' denied.",
                             target,
                             existingEntry.Owner,
                             ownerIdentity);

            return false;
        }

        _targetResolvers[target] = (ownerIdentity, resolver);

        return true;
    }

    #endregion
}