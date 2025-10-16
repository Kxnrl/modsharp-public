namespace HelloWorld;

// ReSharper disable once UnusedMember.Global
internal class HelloWorld : IModSharpModule
{
    public HelloWorld(ISharedSystem sharedSystem, string? dllPath, string? sharpPath, Version? version, IConfiguration? coreConfiguration, bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);
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

    public string DisplayName => "Hello World";
    public string DisplayAuthor => "ModSharp dev team";
}
