namespace CVar;

// ReSharper disable once UnusedMember.Global
internal class CVar : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;
    public CVar(ISharedSystem sharedSystem, string? dllPath, string? sharpPath, Version? version, IConfiguration? coreConfiguration, bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);

        _sharedSystem = sharedSystem;
    }

    public bool Init()
    {
        // That's it. Very easy.
        _sharedSystem.GetConVarManager().CreateConVar("my_cvar", 0, "This is my cvar.");
        return true;
    }

    public void Shutdown()
    {
    }

    public string DisplayName => "CVar Example";
    public string DisplayAuthor => "ModSharp dev team";
}
