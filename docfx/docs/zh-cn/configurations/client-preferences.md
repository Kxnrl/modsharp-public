# Cleint Preferences

在ModSharp第一方模块中, 我们提供了玩家设置持久化保存的功能.  
它使用了维护核心配置文件中的 ``ConnectionStrings`` 节点,  
你需要在其中添加 ``ClientPreferences`` 键值对作为连接字符串.  

## 格式

我们使用自定义格式来确保``ClientPreferences``可以使用不同的存储协议.

```text
{schema}://{connectionString}
```

支持的schema类型如下:

# [litedb](#tab/litedb)

使用存储于本地文件系统的数据库  
[连接字符串文档](https://github.com/litedb-org/LiteDB/wiki/Connection-String)

> [!TIP]
> 如果你在同一台物理服务器上拥有多个服务端, 你可以使用同一个共享数据库  
> 只需要在路径中使用绝对路径即可, 例如 ``/user/.shared/litedb/114514.db``  
> 在Docker环境下你可能还需要进行映射
> [!TIP]
> 我们还提供了对于当前服务器`data`文件夹的映射, 你只需要使用占位符``{sharp::data}``

# [resp](#tab/resp)

基于RESP协议的KV Store, 例如Redis和Garnet  
[连接字符串文档](https://stackexchange.github.io/StackExchange.Redis/Configuration#configuration-options)  

# [mysql](#tab/mysql)

兼容MySql语法的关系型数据库, 例如MySql/PolarDB以及MariaDB  
[连接字符串文档](https://mysqlconnector.net/connection-options/)  

# [http](#tab/http)

使用RestfulApi进行Http交互的后端  

|Key|Type|Description|
|---|---|---|
|`host`|string|后端服务器地址|
|`authorization`|string|请求中的``Authorization``头|

> [!TIP]
> 后端实现请参考`RestStorage.cs`源代码或官方开源实现

示例:

```text
http://Host=https://clientprefs.modsharp.net;Authorization=abc114514
```

---

## 默认值

```json
"ConnectionStrings": {
  // ... otheres
  "ClientPreferences": "litedb://Filename={sharp::data}/client-preferences.db;Mode=Exclusive;Flush=true"
}
```
