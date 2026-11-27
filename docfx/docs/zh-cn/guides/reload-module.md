# 重新加载模块

ModSharp 会在**下一次换图**（下一张图的 `OnServerInit` 之前）或**下次重启服务器**时自动替换掉有更新的模块。把文件拷上去不会立刻生效，也没有 `ms reload` 命令。

## 放好更新文件

入口 DLL 名字不要改，沿用[首次部署](writing-a-module.md)同样的框架、RID 和 `--no-self-contained` 参数发布到模块的 `reload` 子目录里。

以 `Example` 模块为例：

# [Windows](#tab/windows)

在包含 `Example` 项目的目录里执行（把 `$CS2` 换成实际路径）：

```powershell
$CS2 = 'C:\CS2'
dotnet publish Example/Example.csproj --configuration Release --framework net10.0 --runtime win-x64 --no-self-contained --output "$CS2/game/sharp/modules/Example/reload"
```

# [Linux](#tab/linux)

在包含 `Example` 项目的目录里执行（把 `CS2` 换成实际路径）：

```bash
CS2=/srv/cs2
dotnet publish Example/Example.csproj --configuration Release --framework net10.0 --runtime linux-x64 --no-self-contained --output "$CS2/game/sharp/modules/Example/reload"
```

---

正在跑的旧文件留在上一层不动：

```text
{CS2}/game/sharp/modules/Example/
├── Example.dll                 # 当前版本
├── Example.deps.json
└── reload/
    ├── Example.dll             # 新版本
    ├── Example.deps.json
    └── ...                     # 更新后的顶层依赖
```

> [!IMPORTANT]
> 等文件**全部上传完**再换图。只要暂存目录里有任意顶层文件就会触发替换，加载器不会检查版本号，也不会等上传结束。如果服务器有自动换图，上传时注意避开换图时间，免得读到传了一半的 DLL。

除了 `reload/`，加载器也认同名子目录 `modules/<目录名>/<目录名>/`（用的是部署目录名，不一定等于 `AssemblyName`）。不过不推荐用同名子目录：只要它存在（哪怕里面是空的），优先级就会盖过 `reload/`，而且更新器只删 `reload/`、永远不删同名子目录，用完不手动删就会一直把 `reload/` 挡住。日常统一放 `reload/` 最省心。

## 换图触发更新

文件放好后，按平时的方式换张图就行，比如在服务端控制台执行：

```text
changelevel de_dust2
```

到了下一次原生 `StartupServer` 前置钩子，模块管理器会依次做四件事：

1. 卸载暂存目录里有文件的模块：先给其他运行中的模块发 `OnLibraryDisconnect`，撤销它导出的接口和 native，再调用它的 `Shutdown()`。
2. 把暂存目录里的每个顶层文件覆盖到正式目录，删掉复制完的暂存文件，再非递归地删除 `reload` 目录（如果 `reload/` 里夹了子目录，删除会失败）。
3. 以 `hotReload = true` 创建新实例，依次跑 `Init()` 和 `PostInit()`，然后用 `OnLibraryConnected` 通知其他模块。
4. 仅对本轮成功替换的模块调用 `OnAllModulesLoaded()`，没动过的模块不会重复触发（如果重载完没有任何 `Running` 模块，核心会直接报致命错误退出）。

换图后刷一下日志和 `ms modules`，确认新版本状态是 `Running`。依赖该模块的其他模块要在收到 `OnLibraryConnected` 时重新获取接口，别指望再次触发 `OnAllModulesLoaded()`。

## 已知限制

- **只复制顶层文件**：不会递归更新子目录（比如 `runtimes/`、本地化附属程序集或嵌套原生库），而且 `reload/` 里如果带了子目录，非递归删目录会失败，在服务器启动时甚至会导致该模块加载失败。改动涉及子目录时，直接停服替换整个模块目录。
- **不删旧文件**：新包里已经去掉的旧依赖不会自动清理，停服维护时手动清。
- **开服也会吃掉待更新文件**：服务器刚启动时如果 `reload/` 里有文件，同样会先覆盖再加载，但这时传给构造函数的是 `hotReload = false`。模块目录里必须原本就有正式的 `.deps.json` 和 DLL，不能只扔个 `reload/` 当首次安装。
- **换图不扫新模块**：新增模块目录或修改 `.disabled` 标记，都需要重启服务器才生效。
- **只重载普通模块**：更新 `sharp/shared` 里的共享接口、托管核心或原生引擎，都必须重启服务器。
- **自己清干净资源**：`Shutdown()` 里要释放所有外部资源和回调，热重载不是进程隔离，旧对象如果还被别处抓着引用就卸载不掉。
