# 入门

## 前言

本文将引导你完成 **ModSharp** 在全新安装的 CS2 服务器上的部署流程。

## 名词定义

为简化后续操作描述，先明确核心路径定义：​

- ``{CS2}``：*服务端目录*（后续所有步骤中，此符号均代表你的 CS2 服务器根目录）

## 下载

请前往 [Release](https://github.com/Kxnrl/modsharp-public/releases) 页面，下载最新版本即可。

---

## 检查环境

# [Windows](#tab/windows)

- **Visual Studio Redistributable** [下载链接](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist?view=msvc-170)
- **.NET 9** [下载链接](https://dotnet.microsoft.com/download/dotnet/9.0)

> [!TIP]
>
> 推荐使用Windows Server 2022 DataCenter或更新版本

# [Linux](#tab/linux)

- **.NET 9** [安装教程](https://learn.microsoft.com/dotnet/core/install/linux)

如你的运行Docker，请使用Steam RT3镜像（参阅：registry.gitlab.steamos.cloud/steamrt/sniper/sdk:latest）
> [!TIP]
> 如果你使用的是翼龙面板，你可以参阅：[1zc/CS2-Pterodactyl](https://github.com/1zc/CS2-Pterodactyl)

如果你没有使用Docker，请自行[安装SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD)，并下载Steam RT3。

> [!NOTE]
> 示例Steam RT3安装教程：当你启动SteamCMD以后，依次运行如下指令
>
> - force_install_dir ~/steamrt
> - login anonymous
> - app_update 1628350 validate  

> [!CAUTION]
> 由于SteamRT3的限制，我们没办法使用系统的.NET。  
> 请自行根据上文提供的[.NET下载页面](https://dotnet.microsoft.com/download/dotnet/9.0)下载.NET的发行包，并将其解压至`{CS2}/game/sharp/runtime`中。

---

---

## 启动配置

请前往`{CS2}/game/csgo/gameinfo.gi`，做出如下修改：

```diff
    // ...Ignore
    FileSystem
    {
        SearchPaths
        {
            Game_LowViolence    csgo_lv // Perfect World content override

+            Game    sharp

            Game    csgo
            Game    csgo_imported
            Game    csgo_core
            Game    core

            Mod        csgo
            Mod        csgo_imported
            Mod        csgo_core

            AddonRoot            csgo_addons
            OfficialAddonRoot    csgo_community_addons

            LayeredGameRoot        "../game_otherplatforms/etc" [$MOBILE || $ETC_TEXTURES] //Some platforms do not support DXT compression. ETC is a well-supported alternative.
            LayeredGameRoot        "../game_otherplatforms/low_bitrate" [$MOBILE]
        }

        "UserSettingsPathID"    "USRLOCAL"
        "UserSettingsFileEx"    "cs2_"
    }

    // ...Ignore
```

> [!TIP]
> 此处操作和你安装Metamod是一样的，只不过这里的路径由 `csgo/addons/metamod` 换成 `sharp`

---

## 安装ModSharp

将sharp包放置在`{CS2}/game`目录下

> [!NOTE]
> 示例结构
>
> ```text
> .
> ├── bin
> ├── core 
> ├── cs2.sh
> ├── csgo
> ├── csgo_community_addons
> ├── csgo_core 
> ├── csgo_imported 
> ├── csgo_lv
> ├── sharp
> │   ├── bin
> │   ├── configs
> │   ├── core
> │   ├── gamedata
> │   ├── modules
> │   └── shared
> └── thirdpartylegalnotices.txt
> ```

---

## 启动服务器

启动服务器之后你还是没办法运行的，因为我们需要至少 **1** 个正常运行的模块才能让服务端正常运行。
