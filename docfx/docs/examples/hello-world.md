# Hello World

该示例演示了如何编写最简单的插件。

HelloWorld.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>HelloWorld</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="*" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

HelloWorld.cs

```cs
using Microsoft.Extensions.Configuration;
using Sharp.Shared;

namespace HelloWorld;

public sealed class HelloWorld : IModSharpModule
{
    public HelloWorld(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
    {
    }

    public bool Init()
    {
        Console.WriteLine("Hello World!");
        return true;
    }

    public void Shutdown()
    {
        Console.WriteLine("Byebye World!");
    }

    public string DisplayName   => "Hello World";
    public string DisplayAuthor => "ModSharp dev team";
}
```
