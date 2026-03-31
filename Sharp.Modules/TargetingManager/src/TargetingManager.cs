// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable UnusedParameter.Local

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.TargetingManager.Resolvers;
using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.TargetingManager;

internal sealed class TargetingManager : IModSharpModule, ITargetingManager
{
    private static readonly string CoreIdentity
        = typeof(TargetingManager).Assembly.GetName().Name ?? "Sharp.Modules.TargetingManager";

    private readonly ILogger<TargetingManager> _logger;
    private readonly ISharedSystem             _sharedSystem;
    private readonly IClientManager            _clientManager;

    private readonly Dictionary<string, (string Owner, ITargetResolver Resolver)> _targetResolvers;

#region IModSharpModule

    public TargetingManager(ISharedSystem sharedSystem,
        string                            dllPath,
        string                            sharpPath,
        Version                           version,
        IConfiguration                    coreConfiguration,
        bool                              hotReload)
    {
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<TargetingManager>();

        _sharedSystem  = sharedSystem;
        _clientManager = sharedSystem.GetClientManager();

        _targetResolvers = new Dictionary<string, (string Owner, ITargetResolver Resolver)>(StringComparer.OrdinalIgnoreCase);

        var clientManager = sharedSystem.GetClientManager();

        RegisterResolver(CoreIdentity, new Alive(sharedSystem));
        RegisterResolver(CoreIdentity, new All(sharedSystem));
        RegisterResolver(CoreIdentity, new None(sharedSystem));
        RegisterResolver(CoreIdentity, new Bots(sharedSystem));
        RegisterResolver(CoreIdentity, new Ct(sharedSystem));
        RegisterResolver(CoreIdentity, new Dead(sharedSystem));
        RegisterResolver(CoreIdentity, new Me(sharedSystem));
        RegisterResolver(CoreIdentity, new NotMe(sharedSystem));
        RegisterResolver(CoreIdentity, new Spec(sharedSystem));
        RegisterResolver(CoreIdentity, new Te(sharedSystem));
        RegisterResolver(CoreIdentity, new Aim(sharedSystem));
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
        // escape match
        if (target.StartsWith('#'))
        {
            return GetClientLiteral(target[1..]);
        }

        if (target.StartsWith('@'))
        {
            if (_targetResolvers.TryGetValue(target, out var resolver))
            {
                return resolver.Resolver.Resolve(activator);
            }

            // invert
            if (target.StartsWith("@!"))
            {
                // "@!ct" --> "@ct"
                var positiveTarget = string.Concat("@", target.AsSpan(2));

                var allClients = _clientManager.GetGameClients(true);

                var clientsToExclude = GetByTarget(activator, positiveTarget);

                return allClients.Except(clientsToExclude);
            }

            // check for @76561198...
            if (target.Length is 18 && ulong.TryParse(target.AsSpan(1), out var steamId))
            {
                if (_clientManager.GetGameClient(new SteamID(steamId)) is { } client)
                {
                    return [client];
                }
            }

            return GetClientLiteral(target);
        }

        if (target.Length is 17 && ulong.TryParse(target, out var directSteamId))
        {
            if (_clientManager.GetGameClient(new SteamID(directSteamId)) is { } client)
            {
                return [client];
            }
        }

        return GetClientLiteral(target);
    }

    public bool RegisterResolver(string ownerIdentity, ITargetResolver resolver)
    {
        var target = resolver.GetTarget();

        if (string.IsNullOrWhiteSpace(target))
        {
            _logger.LogError("Failed to register target '{target}' for module '{owner}': empty target name",
                             target,
                             ownerIdentity);

            return false;
        }

        if (!target.StartsWith('@'))
        {
            _logger.LogError("Failed to register target '{target}' for module '{owner}': does not start with '@'",
                             target,
                             ownerIdentity);

            return false;
        }

        if (_targetResolvers.TryGetValue(target, out var existingEntry))
        {
            _logger.LogError(
                "Failed to register target '{target}'. It is already registered by '{owner}'. Request from '{newOwner}' denied.",
                target,
                existingEntry.Owner,
                ownerIdentity);

            return false;
        }

        _targetResolvers[target] = (ownerIdentity, resolver);

        return true;
    }

#endregion

    private List<IGameClient> GetClientLiteral(string name)
    {
        var gameClients = _clientManager.GetGameClients(true).ToArray();

        // Exact Matches, target ALL players with this exact name
        var exactMatches = gameClients
                           .Where(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                           .ToList();

        if (exactMatches.Count > 0)
        {
            return exactMatches;
        }

        // Partial Matches, only return if exactly ONE person matches the partial string.
        var partialMatches = gameClients
                             .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                             .ToList();

        if (partialMatches.Count == 1)
        {
            return [partialMatches[0]];
        }

        return [];
    }
}
