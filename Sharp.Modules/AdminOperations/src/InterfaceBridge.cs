using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.AdminOperations;


/// <summary>
/// 不能删！纯开洞行为！
/// </summary>
internal class InterfaceBridge
{
    private readonly ISharedSystem _sharedSystem;

    public InterfaceBridge(ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration coreConfiguration,
        bool hotReload)
    {
        _sharedSystem = sharedSystem;
        DllPath = dllPath;
        SharpPath = sharpPath;
        Version = version;
        CoreConfiguration = coreConfiguration;
        IsHotReload = hotReload;
        ModuleIdentity = Path.GetFileName(dllPath);
    }

    public string ModuleIdentity { get; }

    public ISteamApi SteamApi => ModSharp.GetSteamGameServer();

    public IEntityManager EntityManager => _sharedSystem.GetEntityManager();
    public IClientManager ClientManager => _sharedSystem.GetClientManager();
    public IConVarManager ConVarManager => _sharedSystem.GetConVarManager();
    public ITransmitManager TransmitManager => _sharedSystem.GetTransmitManager();
    public IHookManager HookManager => _sharedSystem.GetHookManager();
    public IEventManager EventManager => _sharedSystem.GetEventManager();
    public IFileManager FileManager => _sharedSystem.GetFileManager();
    public ISchemaManager SchemaManager => _sharedSystem.GetSchemaManager();
    public IEconItemManager EconItemManager => _sharedSystem.GetEconItemManager();
    public ILibraryModuleManager LibraryModuleManager => _sharedSystem.GetLibraryModuleManager();
    public ISoundManager SoundManager => _sharedSystem.GetSoundManager();
    public IPhysicsQueryManager PhysicsQueryManager => _sharedSystem.GetPhysicsQueryManager();

    public IModSharp ModSharp => _sharedSystem.GetModSharp();

    /// <summary>
    ///     CGlobalVars* gpGlobals，没什么好说的。<br />
    ///     注意，一定要在地图加载之后调用！不然服务器第一次加载的时候是拿不到的！
    /// </summary>
    public IGlobalVars GlobalVars => ModSharp.GetGlobals();

    /// <summary>
    ///     CGameRules* g_pGameRules <br />
    ///     注意，一定要在地图加载之后调用！不然服务器第一次加载的时候是拿不到的！
    /// </summary>
    public IGameRules GameRules => ModSharp.GetGameRules();

    public INetworkServer Server => ModSharp.GetIServer();
    public IGameData GameData => ModSharp.GetGameData();
    public ISharpModuleManager SharpModuleManager => _sharedSystem.GetSharpModuleManager();

    public string DllPath { get; init; }

    public string SharpPath { get; init; }

    public Version? Version { get; init; }

    public IConfiguration? CoreConfiguration { get; init; }

    public bool IsHotReload { get; init; }

}
