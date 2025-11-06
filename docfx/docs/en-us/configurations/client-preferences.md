# Client Preferences

In ModSharp's first-party modules, we provide a feature for persistent storage of player preferences.  
It uses the `ConnectionStrings` node in the core configuration file,  
where you need to add a `ClientPreferences` key-value pair as the connection string.  

## Format

We use a custom format to ensure that `ClientPreferences` can use different storage protocols.

```text
{schema}://{connectionString}
```

The supported schema types are as follows:

# [litedb](#tab/litedb)

Use a database stored on the local file system  
[Connection String Documentation](https://github.com/litedb-org/LiteDB/wiki/Connection-String)

> [!TIP]
> If you have multiple server instances on the same physical server, you can use the same shared database  
> Simply use an absolute path in the path, for example `/user/.shared/litedb/114514.db`  
> In a Docker environment, you may also need to configure volume mapping
> [!TIP]
> We also provide mapping path for current server's `data` folder. You can use the placeholder `{sharp::data}`

# [resp](#tab/resp)

RESP protocol-based KV Store, such as Redis and Garnet  
[Connection String Documentation](https://stackexchange.github.io/StackExchange.Redis/Configuration#configuration-options)  

# [mysql](#tab/mysql)

MySQL syntax-compatible relational databases, such as MySQL/PolarDB and MariaDB  
[Connection String Documentation](https://mysqlconnector.net/connection-options/)  

# [http](#tab/http)

Backend using Restful API for HTTP interaction  

|Key|Type|Description|
|---|---|---|
|`host`|string|Backend server address|
|`authorization`|string|`Authorization` header in the request|

> [!TIP]
> For backend implementation, please refer to the source code in `RestStorage.cs` or the official open-source implementation

Example:

```text
http://Host=https://clientprefs.modsharp.net;Authorization=abc114514
```

---

## Default Value

```json
"ConnectionStrings": {
  // ... others
  "ClientPreferences": "litedb://Filename={sharp::data}/client-preferences.db;Mode=Exclusive;Flush=true"
}
```
