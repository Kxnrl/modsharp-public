using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.BuiltinResolvers;

public class Bots(IClientManager clientManager) : ITargetResolver
{
    public string GetTarget()
        => PredefinedTargets.Bots;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
    {
        foreach (var client in clientManager.GetGameClients(true))
        {
            if (client.IsFakeClient)
            {
                yield return client;
            }
        }
    }
}