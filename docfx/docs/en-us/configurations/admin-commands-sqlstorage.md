# Admin Commands SQL Storage

Optional SQL-backed storage provider for AdminCommands punishments (ban, mute, gag, etc.).
This module is also a reference implementation for anyone who wants to bring their own storage backend.

## Enable

The loader only scans top-level folders in `sharp/modules`.
Official builds place this module in `sharp/modules/AdminCommands.SQLStorage` with a `.disabled` marker so it is not enabled by default.
To enable it, remove the `.disabled` file.

## Connection String

Format:

```text
{schema}://{connectionString}
```

Supported schemas:

- `mysql`
- `pgsql` / `postgres` / `postgresql`

Example:

```json
"ConnectionStrings": {
  "AdminCommands.SQLStorage": "mysql://Server=localhost;Port=3306;Database=modsharp;User ID=modsharp;Password=secret;"
}
```

## Bring Your Own Storage

Implement `IAdminOperationStorageService` (in `Sharp.Modules.AdminCommands.Shared`) and register it with
`IAdminOperationStorageService.Identity`.
See the `AdminCommands.SQLStorage` module source for a complete example.
