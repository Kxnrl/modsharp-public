# 从CS#/SourceMod迁移

如果你是CS#/SourceMod，想迁移到这来，有一些事情你需要提前了解。

1. OnMapStart/OnMapEnd

一句话概括的话，
- `OnMapStart`: `OnServerActivate` | `OnGameInit`，选哪个都行，看你的实际需求，我个人习惯用`OnServerActivate`
- `OnMapEnd`: `OnGameShutdown`

这三个forward在`IGameListner`中。
如何使用？
> [!NOTE]
> 我们会预先注册一个辅助类`InterfaceBridge`来包装`ISharedSystem`中常用的工具函数
```cs
class GameListener : IGameListener 
{
    private readonly InterfaceBridge _bridge;

    public GameListener(InterfaceBridge bridge)
    {
        _bridge = bridge;
    }

    int IGameListener.ListenerVersion => IGameListener.ApiVersion;

    int ListenerPriority => 0;

    public void OnServerActivate()
    {
        // 这里添加你的逻辑
    }

    public void OnGameInit()
    {
        // 这里添加你的逻辑，建议与OnServerActivate二选一
    }

    public void OnGameShutdown()
    {

    }

    public void Init()
    {
        _bridge.ModSharp.InstallGameListener(this);
    }

    public void Shutdown()
    {
        _bridge.ModSharp.RemoveGameListener(this);
    }
}
```