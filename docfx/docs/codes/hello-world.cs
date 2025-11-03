using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;

namespace HelloWorld;

public sealed class HelloWorld : IModSharpModule
{
    public HelloWorld(ISharedSystem sharedSystem,
        string                      dllPath,
        string                      sharpPath,
        Version                     version,
        IConfiguration              coreConfiguration,
        bool                        hotReload)
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
