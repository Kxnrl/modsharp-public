# Admin Permissions and Commands

This article demonstrates how to manage commands and permissions programmatically using the `Sharp.Modules.AdminManager`.

While the module is named `AdminManager`, this system is the standard way to handle **any restricted access**, including but not limited to:
*   Server Administration (Kick/Ban)
*   VIP / Donor perks (Custom skins, tracers)

This approach allows you to bypass static JSON files and load permissions from databases (MySQL, Redis), external APIs, or complex internal logic.

## Prerequisites

Your module must reference and resolve `Sharp.Modules.AdminManager`.

> [!WARNING]
> Ensure you handle the dependency injection safely. Refer to the `TryResolveAdminManager` method in the full source code to see how to handle module loading/unloading gracefully.

> [!TIP]
> Use one stable `moduleIdentity` value for both `MountAdminManifest` and `GetCommandRegistry`, and keep it unchanged across calls.
> Prefer your module `AssemblyName`:
> `private static readonly string ModuleIdentity = typeof(MyModule).Assembly.GetName().Name ?? "MyModule";`

## 1. Defining Permissions

ModSharp uses a `group:action` string format for permissions. This structure is vital for **Wildcard** support.

In this example, we define two distinct categories:

*   **Offensive:** Actions that harm players (`slay`, `kill`).
*   **Medic:** Actions that help players (`heal`).

```csharp
// Category 1: Offensive
private const string SlayPermission = "admin_offensive:slay";
private const string KillPermission = "admin_offensive:kill";

// Category 2: Medic
// We use a different prefix ('admin_medic') to separate it from offensive wildcards.
private const string HealPermission = "admin_medic:heal";
```

## 2. Registering Commands

Link your permission strings to chat commands using the `CommandRegistry`.

```csharp
var registry = _adminManager.GetCommandRegistry(ModuleIdentity);

// Command string, Handler function, Required Permissions
registry.RegisterAdminCommand("slay", OnCommandSlay,   [SlayPermission]);
registry.RegisterAdminCommand("kill", OnCommandKill,   [KillPermission]);
registry.RegisterAdminCommand("heal", OnCommandHealth, [HealPermission]);
// Example: registry.RegisterAdminCommand("vip_gold", OnGoldGun, ["vip:gold_gun"]);
```

## 3. Building the Manifest

The `BuildAdminManifest` function is where you define the hierarchy of your server. This relies on `AdminTableManifest`, which consists of three parts:

### A. Permission Collections
Collections allow you to group multiple permission strings under a single name (a shortcut).

```csharp
var permissionCollection = new Dictionary<string, HashSet<string>>
{
    // Grouping Slay and Kill logic together
    ["admin_group_offensive"] = [SlayPermission, KillPermission],

    // Grouping Medic logic
    ["admin_group_medic"] = [HealPermission],
};
```

### B. Roles
Roles are named entities (e.g., "VIP", "Moderator", "Admin") that contain a set of permissions and an immunity level.

```csharp
// Create a "GeneralAdmin" role that has access to EVERYTHING
HashSet<string> allPermissions = [SlayPermission, KillPermission, HealPermission];

var roles = new List<RoleManifest> 
{
    new RoleManifest("GeneralAdmin", immunity: 1, allPermissions) 
};
```

### C. Admin Assignment Strategies
There are four primary ways to assign permissions to a specific SteamID.

| ID Example | Strategy | Syntax | Result | Logic |
| :--- | :--- | :--- | :--- | :--- |
| **User 1** | **Role** | `["@GeneralAdmin"]` | **Full Access** | Inherits Slay, Kill, and Heal from the role definition. |
| **User 2** | **Raw** | `["admin_medic:heal"]` | **Heal Only** | Granted a specific permission string directly. Cannot Slay. |
| **User 3** | **Negation** | `["@GeneralAdmin", "!admin_medic:heal"]` | **Slay/Kill Only** | Inherits the Role, but `!` explicitly **removes** the medic permission. |
| **User 4** | **Wildcard** | `["admin_offensive:*"]` | **Slay/Kill Only** | Matches `admin_offensive:slay` and `:kill`. Does *not* match `admin_medic:heal`. |

## 4. Using External Sources (Database / API)

While the example above hardcodes users into lists for simplicity, the primary use case for this module is loading data dynamically.

You can replace the hardcoded Lists/Dictionaries in `BuildAdminManifest` with data fetched from your own services (MySQL, HTTP Request).

### Using Existing Roles
If you only wish to assign admins to roles that are already defined (e.g., in `admins.jsonc` or other modules), you do **not** need to repopulate the permissions or roles lists. You can simply pass them as empty collections.

```csharp
private AdminTableManifest BuildAdminManifest()
{
    // 1. Permissions & Roles:
    // OR leave these empty if you are using roles/permissions defined elsewhere.
    Dictionary<string, HashSet<string>>() myPermissions = _requestManager.GetPermissions();
    List<RoleManifest>() myRoles = _requestManager.GetRoles();

    // 2. Admins:
    // Fetch admins from your database
    List<AdminManifest> myAdmins = _requestManager.GetAdmins(); 

    return new AdminTableManifest(myPermissions, myRoles, myAdmins);
}
```

> [!CAUTION]
> If you omit `myRoles` but assign a user the role `["@SuperAdmin"]` in `myAdmins`, you must ensure that `@SuperAdmin` is defined **globally** by another source. If the role does not exist, the user will not receive those permissions.

## Full Source Code

[!code-csharp[Main](../../codes/admin-example.cs)]
