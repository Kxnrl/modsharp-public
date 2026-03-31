# Command Manager

本教程将演示如何使用CommandManager扩展。

## Extension 与 Module 的区别

ModSharp 提供两种注册命令的方式，理解两者的区别很重要：

| | Extension (`Sharp.Extensions.CommandManager`) | Module (`Sharp.Modules.CommandCenter`) |
|---|---|---|
| **适用场景** | Monorepo — 所有功能集中在一个模块里 | 多模块 — 多个模块共享命令系统 |
| **安装方式** | 通过 NuGet 包安装 | 由 ModSharp 模块系统加载 |
| **接入方式** | 基于 DI（`IServiceCollection.AddCommandManager()`） | 通过 `ISharedSystem.GetSharpModuleManager()` 获取 |
| **管理员命令** | 内置 `RegisterAdminCommand`（已弃用，请使用 AdminManager） | 通过 `AdminManager.GetCommandRegistry()` 注册带权限检查的命令 |
| **生命周期** | 由插件自身的 `IServiceProvider` 管理 | 由 ModSharp 模块生命周期管理 |

> [!TIP]
> 如果你的项目是 **monorepo**（单个模块处理所有功能），使用 **Extension**（`Sharp.Extensions.CommandManager`）。
> 如果你的项目由**多个模块**组成且需要共享命令系统，使用 **Module**（`Sharp.Modules.CommandCenter`）。

## 示例

[!code-csharp[CommandManagerExample.cs](../../codes/command-manager.cs)]
