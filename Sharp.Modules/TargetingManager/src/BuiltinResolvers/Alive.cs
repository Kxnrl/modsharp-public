using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.BuiltinResolvers;

public class Alive(IClientManager clientManager) : ITargetResolver
{
    public string GetTarget()
        => PredefinedTargets.Alive;

    public IEnumerable<IGameClient> Resolve(IGameClient? activator)
    {
        foreach (var client in clientManager.GetGameClients(true))
        {
            if (client.GetPlayerController()?.GetPlayerPawn() is not { IsValidEntity: true, IsAlive: true })
            {
                continue;
            }

            yield return client;
        }
    }
}