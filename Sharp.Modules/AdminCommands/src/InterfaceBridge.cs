using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Sharp.Modules.AdminCommands;

internal class InterfaceBridge
{
    public InterfaceBridge(
        string        sharpPath,
        ISharedSystem sharedSystem)
    {
        SharpPath = sharpPath;

        ModSharp             = sharedSystem.GetModSharp();
        ConVarManager        = sharedSystem.GetConVarManager();
        EventManager         = sharedSystem.GetEventManager();
        ClientManager        = sharedSystem.GetClientManager();
        EntityManager        = sharedSystem.GetEntityManager();
        FileManager          = sharedSystem.GetFileManager();
        HookManager          = sharedSystem.GetHookManager();
        SchemaManager        = sharedSystem.GetSchemaManager();
        TransmitManager      = sharedSystem.GetTransmitManager();
        EconItemManager      = sharedSystem.GetEconItemManager();
        SoundManager         = sharedSystem.GetSoundManager();
        LibraryModuleManager = sharedSystem.GetLibraryModuleManager();
        LoggerFactory        = sharedSystem.GetLoggerFactory();

        SteamAPi = ModSharp.GetSteamGameServer();
    }

    public string SharpPath { get; }

    public IModSharp             ModSharp             { get; }
    public IConVarManager        ConVarManager        { get; }
    public IEventManager         EventManager         { get; }
    public IClientManager        ClientManager        { get; }
    public IEntityManager        EntityManager        { get; }
    public IFileManager          FileManager          { get; }
    public IHookManager          HookManager          { get; }
    public ISchemaManager        SchemaManager        { get; }
    public ITransmitManager      TransmitManager      { get; }
    public ISteamApi             SteamAPi             { get; }
    public IEconItemManager      EconItemManager      { get; }
    public ISoundManager         SoundManager         { get; }
    public ILibraryModuleManager LibraryModuleManager { get; }
    public ILoggerFactory        LoggerFactory        { get; }

    public IGameRules     GameRules  => ModSharp.GetGameRules();
    public IGlobalVars    GlobalVars => ModSharp.GetGlobals();
    public INetworkServer Server     => ModSharp.GetIServer();
}
