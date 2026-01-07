using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.Shared;

public interface ITargetResolver
{
    string GetTarget();

    IEnumerable<IGameClient> Resolve(IGameClient? activator);
}