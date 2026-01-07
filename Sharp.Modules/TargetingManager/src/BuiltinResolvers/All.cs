using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.BuiltinResolvers;

public class All(IClientManager clientManager) : ITargetResolver
{
    public string GetTarget()
        => PredefinedTargets.All;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
    {
        return clientManager.GetGameClients();
    }
}