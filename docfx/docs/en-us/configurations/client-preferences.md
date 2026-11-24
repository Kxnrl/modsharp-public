# Client Preferences

The first-party **ClientPreferences** module persists player cookies. Whenever this module is loaded, `ConnectionStrings.ClientPreferences` must be configured in `sharp/configs/core.json`.

## Connection Format

```text
{schema}://{connectionString}
```

Schemas are case-insensitive; four backends are supported:

| Schema | Backend | Value |
| --- | --- | --- |
| `litedb` | Local LiteDB file | LiteDB connection string; `{sharp::data}` expands to this server's `sharp/data` folder |
| `resp` | Redis, Garnet, or other RESP store | StackExchange.Redis configuration string |
| `mysql` | MySQL / MariaDB | MySqlConnector connection string |
| `http` | Custom HTTP service | Semicolon-separated `Host=...;Authorization=...` |

Driver selection lives in [ClientPreferences.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/ClientPreferences/src/ClientPreferences.cs), and backends live under [Storages](https://github.com/Kxnrl/modsharp-public/tree/master/Sharp.Modules/ClientPreferences/src/Storages). Note that PostgreSQL schemas from `AdminCommands.SQLStorage` are not supported here.

## Bundled Default

```json
{
  "ConnectionStrings": {
    "ClientPreferences": "litedb://Filename={sharp::data}/client-preferences.db;Mode=Exclusive;Flush=true"
  }
}
```

This comes from the bundled `core.json` (deleting the key does not fall back to it). The LiteDB driver creates `sharp/data` automatically and is the only driver that expands `{sharp::data}`.

Because `Mode=Exclusive` locks the file, multiple servers cannot share the same LiteDB path. Give each server its own file or switch to Redis/MySQL/HTTP, and mount a persistent volume when running in containers.

Editing the connection string at runtime only logs a warning; restart the server to switch backends.

## HTTP Backend

`Host` is required; `Authorization` is optional (defaults to `modsharp`). Option names are case-insensitive. The leading `http://` only selects the HTTP driver, so `Host` can use `https://`:

```text
http://Host=https://preferences.example.com;Authorization=replace-with-your-value
```

Implement the two endpoints expected by [RestStorage.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/ClientPreferences/src/Storages/RestStorage.cs):

- `GET /cookie/{identity}`: returns the player's cookie array.
- `PUT /cookie/{identity}`: accepts the player's cookie array as the request body.

`identity` is the player's SteamID64, and the JSON schema must match [CookieModel.cs](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Modules/ClientPreferences/src/Models/CookieModel.cs).

## Module Usage

Reference `Sharp.Modules.ClientPreferences.Shared` and acquire `IClientPreference`. Cookies load asynchronously, so check `IsLoaded` or subscribe via `ListenOnLoad` before reading or writing (before load completes, `GetCookie` returns `null` and `SetCookie` throws), and dispose the subscription in `Shutdown`. See the [Client Preferences example](../examples/client-preferences.md).
