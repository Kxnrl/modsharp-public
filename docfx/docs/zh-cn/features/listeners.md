# Listeners

当前版本有如下Listener:

- `IGameListener`: 游戏流程 [示例](../examples/game-listener.md)
- `IClientListener`: 客户端事件[示例](../examples/client-listener.md)
- `IEventListener`: 游戏事件[示例](../examples/event-listener.md)
- `ISteamListener`: Steam事件/回调 [示例](../examples/steam-listener.md)
- `IEntityListener`: 实体事件 [示例](../examples/entity-listener.md)

> [!Caution]
> 安装监听器之后，当你的模块卸载时需要进行移除操作，否则可能出现意料之外的错误。

```c#
public void PostInit()
    => _modSharp.InstallGameListener(this);

public void Shutdown()
    => _modSharp.RemoveGameListener(this);
```
