# Admin FlatFile

在ModSharp第一方模块中, 我们提供了简易的管理员配置读取  
配置文件位于``{CS2}/sharp/configs/admins.json``  

我们依旧使用键值对进行配置,  
Key为玩家的Steam64 ID,  
Value包含如下内容:

- `name`(必填): 名字
- `immunity`(选填): 级别
- `permissions`(选填): 这是一个``string``数组, 包含你要赋予的权限

示例:

```json
{
  "76561198048432253": {
    "name": "Kxnrl",
    "immunity": 100,
    "permissions": [
      "map",
      "kick",
      "ban",
      "root"
    ]
  }
}
```
