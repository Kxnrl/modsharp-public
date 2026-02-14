using System.Collections.Immutable;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Services.Internal.Permissions;

/// <summary>
///     Tracks permissions registered by this module so we can emit a manifest.
/// </summary>
internal sealed class PermissionTracker
{
    private readonly HashSet<string> _permissions = new (StringComparer.OrdinalIgnoreCase);

    public void Track(IEnumerable<string> permissions)
    {
        foreach (var perm in permissions)
        {
            if (!string.IsNullOrWhiteSpace(perm))
            {
                _permissions.Add(perm);
            }
        }
    }

    public void Clear()
    {
        _permissions.Clear();
    }

    public IReadOnlyCollection<string> Permissions => _permissions;
}

internal sealed class TrackingPermissionCommandRegistry : IAdminCommandRegistry
{
    private readonly IAdminCommandRegistry _inner;
    private readonly PermissionTracker     _tracker;

    public TrackingPermissionCommandRegistry(IAdminCommandRegistry inner, PermissionTracker tracker)
    {
        _inner   = inner;
        _tracker = tracker;
    }

    public void RegisterAdminCommand(string                              command,
                                     Action<IGameClient?, StringCommand> call,
                                     ImmutableArray<string>              permissions)
    {
        _tracker.Track(permissions);
        _inner.RegisterAdminCommand(command, call, permissions);
    }

    public void RegisterPermissions(ImmutableArray<string> permissions)
    {
    }
}
