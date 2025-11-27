using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminManager;

internal class Admin : IAdmin
{
    public string Name { get; }
    public SteamID Identity { get; }
    public byte Immunity { get; }

    private readonly HashSet<string> _permissions;

    public Admin(string name, SteamID identity, byte immunity)
    {
        Name = name;
        Identity = identity;
        Immunity = immunity;

        _permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlySet<string> Permissions => _permissions;

    public bool HasPermission(string permission)
        => _permissions.Contains(permission);

    public bool AddPermission(string permission)
        => _permissions.Add(permission);

    public bool RemovePermission(string permission)
        => _permissions.Remove(permission);
}
