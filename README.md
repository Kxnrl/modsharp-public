<p align="center">
  <a href="https://docs.modsharp.net/">
    <img src="docfx/images/modsharp.svg" width="184" height="184" alt="modsharp">
  </a>
</p>

<div align="center">

# ModSharp

Source 2 modding framework for Counter-Strike 2, with a native engine and a .NET 10 module SDK.

[![Release](https://img.shields.io/github/v/release/Kxnrl/modsharp-public?style=for-the-badge&label=Release)](https://github.com/Kxnrl/modsharp-public/releases/)
[![NuGet](https://img.shields.io/nuget/v/ModSharp.Sharp.Shared?style=for-the-badge&label=NuGet)](https://www.nuget.org/packages/ModSharp.Sharp.Shared/)

<p>
  <a href="https://docs.modsharp.net/" style="width: 168px">Documentation</a>
  ·
  <a href="https://github.com/Kxnrl/modsharp-public/releases/" style="width: 168px">Download</a>
  ·
  <a href="https://docs.modsharp.net/docs/en-us/guides/getting-started.html" style="width: 168px">Quick Start</a>
  ·
  <a href="https://discord.gg/wKarAjHm2G" style="width: 168px">Discord</a>
</p>

</div>

## Getting Started

- [English](https://docs.modsharp.net/docs/en-us/guides/getting-started.html)
- [简体中文](https://docs.modsharp.net/docs/zh-cn/guides/getting-started.html)

Servers require the **x64 .NET 10 runtime**, and module development requires the **.NET 10 SDK**.
Download the [ModSharp server package](https://github.com/Kxnrl/modsharp-public/releases/) matching your server's operating system and follow the installation guide before deploying any modules.

## Documentation

- [Documentation website](https://docs.modsharp.net/)
- [Write a module — English](docfx/docs/en-us/guides/writing-a-module.md) / [编写模块 — 简体中文](docfx/docs/zh-cn/guides/writing-a-module.md)
- [Configuration — English](docfx/docs/en-us/configurations/core.md) / [配置 — 简体中文](docfx/docs/zh-cn/configurations/core.md)
- [API reference](https://docs.modsharp.net/api/Sharp.Shared.html)
- GameData: [English](docfx/docs/en-us/features/gamedata.md) / [简体中文](docfx/docs/zh-cn/features/gamedata.md)
- Native libraries (`ILibraryModule`): [English](docfx/docs/en-us/features/library-modules.md) / [简体中文](docfx/docs/zh-cn/features/library-modules.md)
- Panorama custom HUD: [English](docfx/docs/en-us/features/panorama.md) / [简体中文](docfx/docs/zh-cn/features/panorama.md)
- `cs_script` JavaScript: [English](docfx/docs/en-us/features/cs-script.md) / [简体中文](docfx/docs/zh-cn/features/cs-script.md)

### Build the documentation

From the repository root, with the .NET 10 SDK installed:

```sh
dotnet tool install docfx --tool-path .build/docfx --version 2.81.0
./.build/docfx/docfx docfx/docfx.json --serve --port 8080
```

Open `http://localhost:8080`. API pages are generated from the current `Sharp.Shared`,
`Sharp.Extensions`, and module `Shared` projects and are not checked in.
When updating a guide, edit both `docfx/docs/en-us` and `docfx/docs/zh-cn`.
Code samples shared by both languages live in `docfx/docs/codes`.

## Community

- [Discord](https://discord.gg/wKarAjHm2G)

## Authors

- [Kxnrl](https://github.com/Kxnrl)
- [BoneTM](https://github.com/BoneTM)
- [laper32](https://github.com/laper32)
- [Nukoooo](https://github.com/Nukoooo)

## License

ModSharp is licensed under the AGPL-3.0 License, with special exceptions outlined in the [LICENSE](./LICENSE) file.
