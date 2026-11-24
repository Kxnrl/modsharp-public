using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace LibraryModuleExample;

public sealed class LibraryModuleExample : IModSharpModule
{
    private const string CommandName = "ms_library_module_info";
    private readonly ISharedSystem _shared;
    private ILibraryModule? _tier0;
    private bool _commandRegistered;

    public LibraryModuleExample(ISharedSystem sharedSystem,
        string                               dllPath,
        string                               sharpPath,
        Version                              version,
        IConfiguration                       coreConfiguration,
        bool                                 hotReload)
        => _shared = sharedSystem;

    public bool Init()
    {
        _tier0 = _shared.GetLibraryModuleManager().Tier0;
        PrintLibraryInfo();
        _shared.GetConVarManager().CreateServerCommand(CommandName,
            OnInfoCommand,
            "Inspect tier0 and its logging export without calling or modifying it.",
            ConVarFlags.Release);
        _commandRegistered = true;
        return true;
    }

    private ECommandAction OnInfoCommand(StringCommand command)
    {
        PrintLibraryInfo();
        return ECommandAction.Stopped;
    }

    private void PrintLibraryInfo()
    {
        var module = _tier0 ?? throw new InvalidOperationException("The module is not initialized.");

        // This real export is also used by the host's log.games.jsonc.
        // ILibraryModule takes the export name WITHOUT GameData's '@' prefix.
        var address = module.GetExportFunction("LoggingSystem_IsChannelEnabled");
        if (address == 0)
        {
            throw new EntryPointNotFoundException("tier0: LoggingSystem_IsChannelEnabled");
        }

        var imageBase = (nuint) module.BaseAddress;
        var absolute = (nuint) address;
        if (absolute < imageBase || absolute - imageBase >= module.ImageSize)
        {
            throw new InvalidOperationException("The export is outside this module's image.");
        }

        var rva = absolute - imageBase;
        Console.WriteLine($"{module.ModuleName}: base=0x{imageBase:X}, size=0x{module.ImageSize:X}");
        Console.WriteLine($"LoggingSystem_IsChannelEnabled: address=0x{absolute:X}, RVA=0x{rva:X}");

        var readable = false;
        foreach (var segment in module.GetSegments())
        {
            Console.WriteLine($"Segment: address=0x{segment.Address:X}, size=0x{segment.Size:X}, access={segment.Access}");
            var start = (nuint) segment.Address;
            if (absolute >= start
                && absolute - start < segment.Size
                && (segment.Access & MemoryAccess.Read) != 0)
            {
                readable = true;
            }
        }

        if (!readable)
        {
            throw new InvalidOperationException("No readable module segment contains the export.");
        }

        // One live byte from a known loaded export, not an arbitrary user-supplied pointer.
        // Segment metadata is not an OS permission probe or a general memory-safety guarantee.
        // Existing hooks may have changed this byte from the scanner's original image.
        var firstByte = Marshal.ReadByte(address);
        Console.WriteLine($"Live first byte: 0x{firstByte:X2}");

        foreach (var export in module.FindExportFunctions("LoggingSystem_IsChannelEnabled"))
        {
            Console.WriteLine($"Export: {export.RawName}, signature={export.Signature}, address=0x{export.Address:X}");
        }

        if (module.GetFunctionRange(address, out var startAddress, out var endAddress))
        {
            Console.WriteLine($"Known function range: [0x{startAddress:X}, 0x{endAddress:X})");
        }
        else
        {
            Console.WriteLine("This address has no known function range in the native index.");
        }
    }

    public void Shutdown()
    {
        if (_commandRegistered)
        {
            _shared.GetConVarManager().ReleaseServerCommandCallback(CommandName, OnInfoCommand);
            _commandRegistered = false;
        }

        // This is a borrowed view; do not free the library or any returned address.
        _tier0 = null;
    }

    public string DisplayName => "Library Module Example";
    public string DisplayAuthor => "ModSharp dev team";
}
