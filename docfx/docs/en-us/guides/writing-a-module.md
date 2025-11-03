# Writing a Module

## Project References

If you already have some development experience, you can directly:

- Create from template: [Template](https://github.com/new?template_name=ModSharp-Module-Template&template_owner=SourceSharp)
- Clone example: [Example](https://github.com/SourceSharp/ModSharp-Module-Example)

Below we will introduce step by step how to write a module.

---

## Environment Setup

The current version of **ModSharp** uses .NET version `9.x`,  
Please ensure your .NET SDK version is at least this version or higher, otherwise you cannot proceed with the following steps.

We recommend using the following development environments:

- Visual Studio 2022/2026
- JetBrains Rider

> [!NOTE]
> We don't recommend using VS Code, because maintaining large projects becomes very painful.
> This guide primarily uses Visual Studio as the IDE.

We will use "Example" as the sample project name.  
If you want to learn how to create a Solution and Class Library, please refer directly to this tutorial: [Jump Link](https://learn.microsoft.com/en-us/dotnet/core/tutorials/library-with-visual-studio)  
After creating the project, you should be able to find **Example.csproj**, which should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

Add the following content to this file:

```diff
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
+    <AssemblyName>Example</AssemblyName>
  </PropertyGroup>

+  <ItemGroup>
+    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="all" />
+  </ItemGroup>

</Project>

```

> [!TIP]
>
> Using `*` in the `Version` field always resolves to the latest version  
> If you need to explicitly specify a version number, use a specific version, for example `Version="2.0.20"`

At this point, you are ready to write modules for ModSharp.

> [!IMPORTANT]
>
> 1. `ModSharp.Sharp.Shared` must have the `PrivateAssets="all"` tag because we need to prevent automatic copying of `Sharp.Shared.dll` and all its related dlls during Publish.  
> 2. `AssemblyName` is a required field, otherwise dependency loading issues may occur. This field should be the same as the project name. Our project name is **Example**, so we enter `Example`. Adjust accordingly during actual development.

---

## Hello, World

All code needs an entry point before execution, and this is no exception.  
When creating a new Class library, .NET automatically generates a `Class1.cs` file. Let's work with what we have and rename this file to `Example.cs`. This file will be our module entry file.  
Then write the following content:

```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;

namespace Example;

public sealed class Example : IModSharpModule
{
    public Example(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration configuration, bool hotReload)
    {

    }

    public bool Init()
    {
        Console.WriteLine("Hello, World!");
        return true;
    }

    public void Shutdown()
    {
        Console.WriteLine("Byebye, World!");
    }

    public string DisplayName   => "Example";
    public string DisplayAuthor => "YourName";
}
```

Where:

```cs
public class Example : IModSharpModule
```

The module entry must inherit from `IModSharpModule`, and only one class in a module can inherit this interface, otherwise various unexpected behaviors may occur.

> [!CAUTION]
> If you have multiple modules in one solution, please place them in different module packages, otherwise unexpected issues may arise.

```cs
    public Example(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration configuration, bool hotReload)
    {

    }
```

This is the class constructor. You must define these parameters, otherwise the module will fail to initialize directly.

```cs
    public bool Init()
        => true;

    public void Shutdown()
    {
    }
```

- `Init()` is called when the module is initialized. Must be implemented. If you don't want to write much, you can simply use `=> true;`
- `Shutdown()` is called when the module is unloaded. Must be implemented. If you don't want to write anything, you can leave it empty, i.e., an empty function with nothing inside.

```cs
    public string DisplayName   => "Example";
    public string DisplayAuthor => "YourName";
```

- DisplayName is your module's display name
- DisplayAuthor is the module author's name

> [!NOTE]
> These two items will appear in `ms modules` to identify the module.

---

## Compile and Install

You can refer to [Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tutorials/publishing-with-visual-studio)  
Although it's for console applications, the steps are exactly the same.  
We recommend using `dotnet publish`.  

Please note that the `dotnet publish` path must be placed in `{CS2}/game/sharp/modules/{your module name, i.e., AssemblyName}`
> In this tutorial: `{CS2}/game/sharp/modules/Example`

After packaging is complete:

- File path should be `{CS2}/game/sharp/modules/Example/Example.dll`
- Dependency path should be `{CS2}/game/sharp/modules/Example/Example.deps.json`

> [!NOTE]
> Please note the following points about module paths:
>
> 1. Cannot contain spaces
> 2. Use PascalCase
> 3. Module path loading rule is `{CS2}/game/sharp/modules/{AssemblyName}/{AssemblyName}.deps.json`, make sure the paths match!

If you still can't understand the above content well, let's look at pictures:

![See Image](../../../images/module-deploy-to.png)

Inside it looks like this:

![See Image](../../../images/module-inner.png)

---

## Start the Server

There's nothing more to say here. After starting, you should see `Hello World!` in the server console.  
At this point, our introductory module is complete.

![See Image](../../../images/hello-world.png)

You can enter `ms modules` after starting to query the module list.

![See Image](../../../images/ms-modules.png)
