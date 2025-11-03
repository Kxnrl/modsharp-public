````markdown
# Getting Started

## Foreword

This guide will walk you through deploying **ModSharp** on a fresh CS2 server installation.

## Terminology

To simplify the following instructions, let's define the core path:

- `{CS2}`: *Server directory* (In all subsequent steps, this symbol represents your CS2 server root directory)

## Download

Please visit the [Release](https://github.com/Kxnrl/modsharp-public/releases) page and download the latest version.

## Check Environment

### 1. Common Dependencies

- **.NET 9**

### 2. For Windows Users, please ensure you have installed the following

- **Visual Studio Redistributable**

> [!TIP]
>
> - If you are missing Visual Studio Redistributable, you can download it here: [Download Link](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist?view=msvc-170)
> - If you are missing .NET 9, download it here: [Download Link](https://dotnet.microsoft.com/download/dotnet/9.0)

### 3. For Linux Users

If your runtime environment is Docker, please use the Steam RT3 image (see: registry.gitlab.steamos.cloud/steamrt/sniper/sdk:latest)
> [!TIP]
> Many Linux users now use Pterodactyl panel, you can refer to: [1zc/CS2-Pterodactyl](https://github.com/1zc/CS2-Pterodactyl)

Please refer to the [.NET Download Page](https://dotnet.microsoft.com/download/dotnet/9.0) to install .NET 9 in your image, which won't be detailed here.  
If you are not using Docker, please [install SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD) yourself and download Steam RT3.

> [!NOTE]
> Example Steam RT3 installation tutorial: After starting SteamCMD, run the following commands in sequence:
>
> - force_install_dir ~/steamrt
> - login anonymous
> - app_update 1628350 validate

> [!CAUTION]
> Due to Steam RT3 limitations, we cannot use the system's .NET.  
> Please download the .NET distribution package from the [.NET Download Page](https://dotnet.microsoft.com/download/dotnet/9.0) mentioned above and extract it to `{CS2}/game/sharp/runtime`.

## Modify `gameinfo.gi`

Please go to `{CS2}/game/csgo/gameinfo.gi` and make the following modifications:

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
> This operation is the same as installing Metamod, except the path is changed from `csgo/addons/metamod` to `sharp`.

## Place the sharp package in the `{CS2}/game` directory

> [!NOTE]
> Example structure
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
> │   └── gamedata
> └── thirdpartylegalnotices.txt
> ```

## Start the Server

After starting the server, you still won't be able to run it because we need at least **1** properly running plugin for the server to function normally.

````
