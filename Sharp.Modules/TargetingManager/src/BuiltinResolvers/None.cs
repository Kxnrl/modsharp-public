using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

#pragma warning disable CS9113 // Parameter is unread.

namespace Sharp.Modules.TargetingManager.BuiltinResolvers;

public class None(IClientManager clientManager) : ITargetResolver
{
    public string GetTarget()
        => PredefinedTargets.None;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
        => [];
}