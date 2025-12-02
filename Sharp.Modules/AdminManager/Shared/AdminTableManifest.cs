namespace Sharp.Modules.AdminManager.Shared;

/*{
     "permissionSets": {
       "system": ["system:role:create", "system:role:delete"],
       "pluginA": ["pluginA:getWeapon", "pluginA:fetchItems"],
       "pluginB": ["pluginB:kick", "pluginB:slay"]
     },

     "roles": [
       { "name": "global_root", "permissions": ["*"] },
       { "name": "pluginA_admin", "permissions": ["pluginA:*"] },
       { "name": "pluginB_kicker", "permissions": ["pluginB:kick"] }
     ],

     "users": [
       {
         "user_id": "u100",
         // 权限数组：@开头表示继承角色，其他表示直接权限
         "permissions": ["@global_root", "!pluginB:slay"]
         // 解析后：global_root的所有权限（*） + 直接拒绝pluginB:slay
       },
       {
         "user_id": "u104",
         "permissions": ["@pluginA_admin", "@pluginB_kicker"]
         // 解析后：pluginA:*（插件A所有） + pluginB:kick（插件B踢人）
       },
       {
         "user_id": "u105",
         "permissions": ["pluginA:getWeapon", "!pluginA:fetchItems"]
         // 解析后：仅直接允许getWeapon + 直接拒绝fetchItems（不继承任何角色）
       },
       {
         "user_id": "u106",
         "permissions": ["@pluginA_admin", "pluginB:slay"]
         // 解析后：pluginA:*（来自角色） + 直接允许pluginB:slay
       }
     ]
   }*/

public record AdminTableManifest(
    Dictionary<string, HashSet<string>> PermissionCollection,
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