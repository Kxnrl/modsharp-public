# ModSharp

Source 2 modding framework for Counter-Strike 2, with a native engine and a .NET 10 module SDK.

[![Release](https://img.shields.io/github/v/release/Kxnrl/modsharp-public?style=for-the-badge&label=Release)](https://github.com/Kxnrl/modsharp-public/releases/)
[![NuGet](https://img.shields.io/nuget/v/ModSharp.Sharp.Shared?style=for-the-badge&label=NuGet)](https://www.nuget.org/packages/ModSharp.Sharp.Shared/)

[Documentation](https://docs.modsharp.net/) · [Download](https://github.com/Kxnrl/modsharp-public/releases/) · [Write a module](https://docs.modsharp.net/docs/en-us/guides/writing-a-module.html) · [Discord](https://discord.gg/wKarAjHm2G)

## Getting Started

- [English](https://docs.modsharp.net/docs/en-us/guides/getting-started.html)
- [简体中文](https://docs.modsharp.net/docs/zh-cn/guides/getting-started.html)

Module development targets `net10.0` and x64 (except `ModSharp.Sharp.Generator.Sdk`, which is a `netstandard2.0` compile-time source generator). Use SDK and module-interface package versions
that match the ModSharp release installed on your server.
The server supplies `Sharp.Shared.dll` (in `sharp/core/`) and `Sharp.Modules.*.Shared.dll` contracts (in `sharp/shared/`), so exclude their runtime assets (`ExcludeAssets="runtime"`) and do not deploy private copies with your module. `ModSharp.Sharp.Extensions.*` packages are deployed alongside your module.
The module-writing guide covers package-reference and publish settings.

## Documentation

- [Documentation website](https://docs.modsharp.net/)

## Community

- [Discord](https://discord.gg/wKarAjHm2G)

## Authors

- [Kxnrl](https://github.com/Kxnrl)
- [BoneTM](https://github.com/BoneTM)
- [laper32](https://github.com/laper32)
- [Nukoooo](https://github.com/Nukoooo)

## License

ModSharp is licensed under the AGPL-3.0 License, with special exceptions outlined in the [LICENSE](./LICENSE) file.
