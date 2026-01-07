using Sharp.Shared.Objects;

namespace Sharp.Modules.TargetingManager.Shared;

public interface ITargetingManager
{
    const string Identity = nameof(ITargetingManager);

    /// <summary>
    ///     Resolves a target string into a list of game clients.
    /// </summary>
    /// <param name="activator">The client who initiated the targeting (can be null).</param>
    /// <param name="target">
    ///     The target string. Examples:
    ///     <list type="bullet">
    ///         <item><c>@me</c>, <c>@t</c>, <c>@ct</c> - Standard resolvers.</item>
    ///         <item><c>76561198...</c> or <c>@76561198...</c> - SteamID64.</item>
    ///         <item>
    ///             <c>@!target</c> - Inversion (e.g., <c>@!ct</c> targets everyone who is NOT CT, also supports
    ///             <c>@!76561198...</c>).
    ///         </item>
    ///     </list>
    /// </param>
    /// <returns>A collection of matching game clients.</returns>
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