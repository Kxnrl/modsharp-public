# 入门

## 前言

本文将引导你完成 **ModSharp** 在全新安装的 CS2 服务器上的部署流程。

## 名词定义

为简化后续操作描述，先明确核心路径定义：​

- ``{CS2}``：*服务端目录*（后续所有步骤中，此符号均代表你的 CS2 服务器根目录）

## 下载

请前往 [action](https://github.com/Kxnrl/modsharp-public/actions) 一栏。

## 检查环境

### 1. 通用依赖

- **.NET 9**

### 2. 如果你是Windows用户，请确保你已经安装了如下内容

- **Visual Studio Redistributable**

> [!TIP]
>
> - 如果你缺少Visual Studio Redistributable，你可以前往此处下载：[下载链接](https://learn.microsoft.com/zh-cn/cpp/windows/latest-supported-vc-redist?view=msvc-170)
> - 如果你缺少.NET 9，前往此处下载：[下载链接](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)

### 3. 如果你是Linux用户

如你的运行环境是Docker，请使用Steam RT3镜像（参阅：registry.gitlab.steamos.cloud/steamrt/sniper/sdk:latest）
> [!TIP]
> 现在Linux用户应该有不少人用的是翼龙面板，你可以参阅：[1zc/CS2-Pterodactyl](https://github.com/1zc/CS2-Pterodactyl)

请参阅[.NET下载页面](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)在镜像中安装.NET 9，此处不再赘述。  
如果你没有使用Docker，请自行[安装SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD)，并下载Steam RT3。

> [!NOTE]
> 示例Steam RT3安装教程：当你启动SteamCMD以后，依次运行如下指令
>
> - force_install_dir ~/steamrt
> - login anonymous
> - app_update 1628350 validate  

> [!CAUTION]
> 由于SteamRT3的限制，我们没办法使用系统的.NET。  
> 请自行根据上文提供的[.NET下载页面](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)下载.NET的发行包，并将其解压至`{CS2}/game/sharp/runtime`中。

## 修改`gameinfo.gi`

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

## 将sharp包放置在`{CS2}/game`目录下

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
> └── thirdpartylegalnotices.txt
> ```

## 启动服务器

启动服务器之后你还是没办法运行的，因为我们需要至少 **1** 个正常运行的插件才能让服务端正常运行。
