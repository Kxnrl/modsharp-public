using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.BuiltinResolvers;

public class NotMe(IClientManager clientManager) : ITargetResolver
{
    public string GetTarget()
        => PredefinedTargets.NotMe;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
    {
        var players = clientManager.GetGameClients();

        return activator is null ? players : players.Except([activator]);
    }
}