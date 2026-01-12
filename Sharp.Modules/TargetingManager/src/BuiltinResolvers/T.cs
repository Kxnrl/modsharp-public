using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.BuiltinResolvers;

public class T(IClientManager clientManager) : ITargetResolver
{
    public string GetTarget()
        => PredefinedTargets.T;

    public IEnumerable<IGameClient> Resolve(IGameClient? handler)
    {
        foreach (var client in clientManager.GetGameClients(true))
        {
            if (client.GetPlayerController()?.GetPlayerPawn() is not { IsValidEntity: true } pawn)
            {
                continue;
            }

            if (pawn.Team == CStrikeTeam.TE)
            {
                yield return client;
            }
        }
    }
}