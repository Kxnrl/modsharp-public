using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.Shared;

public interface ITargetingManager
{
    const string Identity = nameof(ITargetingManager);

    /// <summary>
    ///     Targeting player.
    /// </summary>
    /// <param name="activator">Who targets</param>
    /// <param name="target">the target string (e.g. "@me").</param>
    /// <returns></returns>
    public IEnumerable<IGameClient> GetByTarget(IGameClient? activator, string target);

    /// <summary>
    ///     Register a custom target resolver.
    /// </summary>
    /// <param name="ownerIdentity">
    ///     The identity of the module registering this target.
    ///     Recommended: <c>typeof(YourModule).Assembly.GetName().Name</c>
    /// </param>
    /// <param name="target">The target string (e.g. "@vip").</param>
    /// <param name="resolver">The resolver logic.</param>
    /// <returns>True if registered successfully, false if target already exists.</returns>
    public bool RegisterResolver(string ownerIdentity, string target, ITargetResolver resolver);
}