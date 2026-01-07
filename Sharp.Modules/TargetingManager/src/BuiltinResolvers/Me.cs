using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.BuiltinResolvers;

public class Me(IClientManager clientManager) : ITargetResolver
{
    public string GetTarget()
        => PredefinedTargets.Me;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
    {
        if (activator is null)
        {
            return [];
        }

        return [activator];
    }
}