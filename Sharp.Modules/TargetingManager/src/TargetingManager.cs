// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable UnusedParameter.Local

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.TargetingManager.BuiltinResolvers;
using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager;

internal sealed class TargetingManager : IModSharpModule, ITargetingManager
{
    private readonly ILogger<TargetingManager> _logger;
    private readonly ISharedSystem             _sharedSystem;
    private readonly IClientManager            _clientManager;

    private readonly Dictionary<string, (string Owner, ITargetResolver Resolver)> _targetResolvers
        = new (StringComparer.OrdinalIgnoreCase);

    private static readonly string CoreIdentity
        = typeof(TargetingManager).Assembly.GetName().Name ?? "Sharp.Modules.TargetingManager";

#region IModSharpModule

    public TargetingManager(
        ISharedSystem  sharedSystem,
        string         dllPath,
        string         sharpPath,
        Version        version,
        IConfiguration coreConfiguration,
        bool           hotReload)
    {
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<TargetingManager>();

        _sharedSystem = sharedSystem;
        var clientManager = sharedSystem.GetClientManager();
        _clientManager = clientManager;

        RegisterResolver(CoreIdentity, new Alive(clientManager));
        RegisterResolver(CoreIdentity, new All(clientManager));
        RegisterResolver(CoreIdentity, new None(clientManager));
        RegisterResolver(CoreIdentity, new Bots(clientManager));
        RegisterResolver(CoreIdentity, new Ct(clientManager));
        RegisterResolver(CoreIdentity, new Dead(clientManager));
        RegisterResolver(CoreIdentity, new Me(clientManager));
        RegisterResolver(CoreIdentity, new NotMe(clientManager));
        RegisterResolver(CoreIdentity, new Spec(clientManager));
        RegisterResolver(CoreIdentity, new T(clientManager));
        RegisterResolver(CoreIdentity, new Aim(_sharedSystem));
    }

    public bool Init()
        => true;

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

        // invert
        if (target.StartsWith("@!"))
        {
            // "@!ct" --> "@ct"
            var positiveTarget = target.Remove(1, 1);

            var allClients = _clientManager.GetGameClients(true);

            var clientsToExclude = GetByTarget(activator, positiveTarget);

            return allClients.Except(clientsToExclude);
        }

        // check for 76561198... and @76561198...
        if (target.Length is 17 or 18)
        {
            var span = target.AsSpan();

            if (span[0] == '@')
            {
                span = span[1..];
            }

            if (span.Length == 17 && ulong.TryParse(span, out var steamId))
            {
                if (_clientManager.GetGameClient(steamId) is { } client)
                {
                    return [client];
                }
            }
        }

        return [];
    }

    public bool RegisterResolver(string ownerIdentity, ITargetResolver resolver)
    {
        var target = resolver.GetTarget();

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