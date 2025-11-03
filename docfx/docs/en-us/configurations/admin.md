# Admin FlatFile

In ModSharp's first-party modules, we provide simple administrator configuration reading.  
The configuration file is located at `{CS2}/sharp/configs/admins.json`  

We still use key-value pairs for configuration,  
Key is the player's Steam64 ID,  
Value contains the following:

- `name` (required): Name
- `immunity` (optional): Immunity level
- `permissions` (optional): This is a `string` array containing the permissions you want to grant

Example:

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
