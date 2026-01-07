using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.BuiltinResolvers;

public class Dead(IClientManager clientManager) : ITargetResolver
{
    public string GetTarget()
        => PredefinedTargets.Dead;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
    {
        foreach (var client in clientManager.GetGameClients(true))
        {
            if (client.GetPlayerController()?.GetPlayerPawn() is not { IsValidEntity: true, IsAlive: false })
            {
                continue;
            }

            yield return client;
        }
    }
}