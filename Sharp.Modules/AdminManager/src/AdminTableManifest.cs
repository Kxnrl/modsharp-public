namespace Sharp.Modules.AdminManager;


public record AdminTableManifest(
    Dictionary<string, HashSet<string>> PermissionSets,
    List<RoleManifest> Roles,
    List<AdminManifest> Admins
);

public record RoleManifest(
    string Name,
    HashSet<string> Permissions
);

public record AdminManifest(
    string Name,
    ulong Identity,
    byte Immunity,
    HashSet<string> Permissions
);