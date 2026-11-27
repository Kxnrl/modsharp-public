# Admin Commands SQL Storage

**AdminCommands.SQLStorage** is an optional SQL backend for AdminCommands punishments such as bans, mutes, and gags (when disabled, AdminCommands stores punishments locally in `sharp/data/punishments.json`). Admin roles and permissions live separately in [AdminManager](admin.md).

## Enabling

Official releases ship `sharp/modules/AdminCommands.SQLStorage` with a `.disabled` marker file. Configure a working database connection in `core.json`, delete `.disabled`, and restart the server (keep `AdminCommands` and its dependencies installed).

The module creates its tables during `Init`. The database itself must already exist and the user needs permissions to create tables and read/write rows; connection or schema errors stop the module from loading.

## Connection String

In `sharp/configs/core.json`, set the literal key `"AdminCommands.SQLStorage"` (dot included, not nested) under `ConnectionStrings`:

```json
{
  "ConnectionStrings": {
    "AdminCommands.SQLStorage": "mysql://Server=localhost;Port=3306;Database=modsharp;User ID=modsharp;Password=replace-with-your-password;"
  }
}
```

Format is `{schema}://{connectionString}` (case-insensitive schema):

- `mysql`: MySQL / MariaDB
- `pgsql`, `postgres`, or `postgresql`: PostgreSQL

If `AdminCommands.SQLStorage` is omitted altogether, the module falls back to `ConnectionStrings.AdminCommands`; however, leaving `AdminCommands.SQLStorage` blank is treated as invalid rather than falling back.

LiteDB, SQLite, RESP, HTTP, and `{sharp::data}` expansion are not supported here. The connection string is read once on startup, so restart the server after changing it. See [SQLStorage.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/AdminCommands.SQLStorage/AdminCommands.SQLStorage/SQLStorage.cs).

## Storage Behavior and Custom Backends

The built-in SQL backend returns only active (unremoved, unexpired) records. Inserting a new active punishment for the same SteamID and type closes earlier active ones, and removing a punishment records removal metadata instead of deleting history. Schema and queries are in the [module source](https://github.com/Kxnrl/modsharp-public/tree/master/Sharp.Modules/AdminCommands.SQLStorage/AdminCommands.SQLStorage).

To plug in a custom backend, implement `IAdminOperationStorageService` from `Sharp.Modules.AdminCommands.Shared` and register it under `IAdminOperationStorageService.Identity`. All methods are asynchronous and must not touch game objects. See [IAdminOperationStorageService](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/AdminCommands/Shared/IAdminOperationStorageService.cs).
