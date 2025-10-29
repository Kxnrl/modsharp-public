/*
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Sharp.Core.Bridges;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.GameObjects;
using Sharp.Core.Helpers;
using Sharp.Core.Managers;
using Sharp.Core.Utilities;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Units;

[assembly: DisableRuntimeMarshalling]

namespace Sharp.Core;

public static class Bootstrap
{
    private static readonly ShutdownMonitor ShutdownMonitor = new ();

#region Unmanaged Caller

    [UnmanagedCallersOnly]
    public static int Init(nint natives, nint forwards)
    {
        Adapter.Boot(natives, forwards);

        return Initialize();
    }

    [UnmanagedCallersOnly]
    public static void Shutdown()
    {
        ShutdownMonitor.Shutdown();

        Console.WriteLine("MS: Shutdown Bootstrap!");
    }

#endregion

#region Schema List

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private unsafe struct CSchemaClassField
    {
        [FieldOffset(0)]
        public CUtlString Name;

        [FieldOffset(8)]
        public CUtlString Type;

        [FieldOffset(16)]
        public int Offset;

        [FieldOffset(20)]
        public bool Networked;

        [FieldOffset(21)]
        public SchemaTypeCategory Category;

        public string NameString => Name.Get();

        public string TypeString => Type.Get();
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    private unsafe struct CSchemaClass
    {
        [FieldOffset(0)]
        public CUtlString Name;

        [FieldOffset(8)]
        public int ChainOffset;

        [FieldOffset(16)]
        public CUtlVector<CSchemaClassField> Fields;

        [FieldOffset(40)]
        public CUtlVector<Pointer<CUtlString>> BaseClassList;

        public string NameString => Name.Get();
    }

    private static unsafe bool InitSchema()
    {
        ref var schemaList = ref Unsafe.AsRef<CUtlVector<Pointer<CSchemaClass>>>(Schema.GetSchemas().ToPointer());

        var schema = new Dictionary<string, SchemaClass>(StringComparer.OrdinalIgnoreCase);

        foreach (var pClass in schemaList)
        {
            ref var @class    = ref pClass.AsRef();
            var     fieldDict = new Dictionary<string, SchemaClassField>(StringComparer.OrdinalIgnoreCase);

            var className = @class.Name.Get();

            foreach (var field in @class.Fields)
            {
                var type    = field.TypeString;
                var isArray = type.Contains('[');

                fieldDict.Add(field.NameString,
                              new SchemaClassField
                              {
                                  Offset    = field.Offset,
                                  Networked = field.Networked,
                                  Type      = type,
                                  IsArray   = isArray,
                                  Category  = field.Category,
                              });
            }

            var baseClasses = new List<string>();

            foreach (var baseClass in @class.BaseClassList)
            {
                var baseClassName = baseClass.AsRef().Get();

                baseClasses.Add(baseClassName);
            }

            schema.Add(className,
                       new SchemaClass
                       {
                           ClassName   = className,
                           ChainOffset = (ushort) @class.ChainOffset,
                           Fields      = fieldDict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
                           BaseClasses = baseClasses, // keep order
                       });
        }

        SharedGameObject.SchemaInfo = schema.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // validate used schema field
        return SchemaValidate.SchemaValidator.Validate();
    }

#endregion

#region EconItem

    private static unsafe bool InitEconItemDefinitions()
    {
        ref var itemDefinitions = ref Unsafe.AsRef<CUtlVector<nint>>(Econ.GetItemDefinitions().ToPointer());

        var size = itemDefinitions.Size;

        if (size == 0)
        {
            return false;
        }

        var idDictionary   = new Dictionary<ushort, IEconItemDefinition>(size);
        var nameDictionary = new Dictionary<string, IEconItemDefinition>(size, StringComparer.OrdinalIgnoreCase);

        var success = true;

        foreach (var ptr in itemDefinitions)
        {
            var definition = EconItemDefinition.Create(ptr)
                             ?? throw new EntryPointNotFoundException("Failed to create EconItemDefinition");

            var index   = definition.Index;
            var defName = definition.DefinitionName;

            try
            {
                idDictionary.Add(index, definition);
                nameDictionary.Add(defName, definition);
            }
            catch (Exception e)
            {
                success = false;
                Printer.Error($"Failed to add index<{index}> as {defName}", e);
            }
        }

        SharedGameObject.EconItemDefinitionsById   = idDictionary.ToFrozenDictionary();
        SharedGameObject.EconItemDefinitionsByName = nameDictionary.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        return success;
    }

    private static unsafe bool InitPaintKits()
    {
        ref var paintKits = ref Unsafe.AsRef<CUtlVector<nint>>(Econ.GetPaintKits().ToPointer());

        if (paintKits.Size == 0)
        {
            return false;
        }

        var dictionary = new Dictionary<uint, IPaintKit>(paintKits.Size);

        foreach (var ptr in paintKits)
        {
            var paintKit = PaintKit.Create(ptr) ?? throw new EntryPointNotFoundException("Failed to create PaintKit");

            var index = paintKit.Id;

            dictionary.Add(index, paintKit);
        }

        SharedGameObject.PaintKits = dictionary.ToFrozenDictionary();

        return true;
    }

#endregion

    private static int Initialize()
    {
        try
        {
            var benchmark = Environment.GetCommandLineArgs().Any(x => x.Contains("benchmark"));

            var sw = Stopwatch.StartNew();
            var ap = Stopwatch.StartNew();

            // prepare schemas
            if (!InitSchema())
            {
                Bridges.Natives.Core.LogWarning("Failed to init schema system!\n");

                return 1;
            }

            ap.Stop();

            if (benchmark)
            {
                Bridges.Natives.Core.LogWarning($"InitSchema in {ap.ElapsedMilliseconds}ms\n");
                Debugger.Break();
            }

            ap.Restart();

            if (!InitEconItemDefinitions())
            {
                Bridges.Natives.Core.LogWarning("Failed to init econ item definitions!\n");

                return 1;
            }

            ap.Stop();

            if (benchmark)
            {
                Bridges.Natives.Core.LogWarning($"InitEconItemDefinitions in {ap.ElapsedMilliseconds}ms\n");
                Debugger.Break();
            }

            ap.Restart();

            if (!InitPaintKits())
            {
                Bridges.Natives.Core.LogWarning("Failed to init paint kits!\n");

                return 1;
            }

            ap.Stop();

            if (benchmark)
            {
                Bridges.Natives.Core.LogWarning($"InitPaintKits in {ap.ElapsedMilliseconds}ms\n");
                Debugger.Break();
            }

            ap.Restart();

            var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                       ?? throw new InvalidOperationException("Root path is null!");

            var configFile = Path.Combine(root, "..", "configs", "core.json");

            var configuration = new ConfigurationBuilder()
                                .AddJsonFile(configFile, false, true)
                                .Build();

            var services = new ServiceCollection();

            ConfigureLogging(services, configuration);
            ConfigureServices(services, configuration);

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true, ValidateScopes = true,
            });

            ap.Stop();

            if (benchmark)
            {
                Bridges.Natives.Core.LogWarning($"BuildServices in {ap.ElapsedMilliseconds}ms\n");
                Debugger.Break();
            }

            ap.Restart();

            if (!Boot(serviceProvider, configuration))
            {
                return 1;
            }

            ap.Stop();

            if (benchmark)
            {
                Bridges.Natives.Core.LogWarning($"Boot in {ap.ElapsedMilliseconds}ms\n");
                Debugger.Break();
            }

            ap.Restart();

            StartServices(serviceProvider);

            ap.Stop();

            if (benchmark)
            {
                Bridges.Natives.Core.LogWarning($"StartServices in {ap.ElapsedMilliseconds}ms\n");
                Debugger.Break();
            }

            sw.Stop();

            Bridges.Natives.Core.LogWarning($"Sharp.Core initialization completed with {sw.ElapsedMilliseconds}ms !\n");
        }
        catch (Exception ex)
        {
            Printer.Error("Failed to init ModSharp.", ex);
            Console.ReadKey(true);

            return 1;
        }
        finally
        {
            GC.Collect();
            GC.WaitForFullGCComplete();
        }

        return 0;
    }

    private static void ConfigureLogging(IServiceCollection services, IConfiguration configuration)
    {
        var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                   ?? throw new InvalidOperationException("Root path is null!");

        // 看不懂文档的人太多, 而且也不喜欢乱搞
        // 这里做配置简化然后重新导出配置
        // 所以不考虑使用Serilog.Settings.Configuration

        var logsDir = Path.Combine(root, "../logs");
        Directory.CreateDirectory(logsDir);

        const string defaultFileTemplate
            = "[{Timestamp:yyyy/MM/dd HH:mm:ss.fff}] | {Level} | {SourceContext}{Scope} {MapName} {NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";

        const string defaultConsoleTemplate
            = "L<CoreCLR> [{Timestamp:MM/dd HH:mm:ss}] | {Level} | {SourceContext}{Scope} {MapName} {NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";

        var fileTemplate    = defaultFileTemplate;
        var consoleTemplate = defaultConsoleTemplate;

        var loggerSettings = configuration.GetSection("Logger");

        if (loggerSettings.GetSection("Template") is { } template)
        {
            if (template.GetSection("File").Get<string>() is { } ft)
            {
                fileTemplate = ft;
            }

            if (template.GetSection("Console").Get<string>() is { } ct)
            {
                consoleTemplate = $"L<CoreCLR> {ct}";
            }
        }

        var loggerConfiguration = new LoggerConfiguration()
                                  .Destructure.ByTransforming<SteamID>(x => x.AsPrimitive())
                                  .Destructure.ByTransforming<EntityIndex>(x => x.AsPrimitive())
                                  .Destructure.ByTransforming<PlayerSlot>(x => x.AsPrimitive())
                                  .Destructure.ByTransforming<UserID>(x => x.AsPrimitive())
                                  .Destructure.ByTransforming<NetworkReceiver>(x => x.DestructureTransform())
                                  .Enrich.FromLogContext()
                                  .WriteTo.Logger(lc => lc
                                                        .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Information)
                                                        .WriteTo.Console(theme: AnsiConsoleTheme.Code,
                                                                         outputTemplate: consoleTemplate,
                                                                         applyThemeToRedirectedOutput: false))
                                  .WriteTo.Logger(lc => lc.Filter
                                                          .ByIncludingOnly(e => e.Level is LogEventLevel.Verbose)
                                                          .WriteTo.Async(a =>
                                                                             a.File(Path.Combine(logsDir, "Trace..log"),
                                                                                    rollingInterval: RollingInterval.Day,
                                                                                    outputTemplate: fileTemplate,
                                                                                    shared: true)))
                                  .WriteTo.Logger(lc => lc.Filter
                                                          .ByIncludingOnly(e => e.Level is LogEventLevel.Debug)
                                                          .WriteTo.Async(a =>
                                                                             a.File(Path.Combine(logsDir, "Debug..log"),
                                                                                    rollingInterval: RollingInterval.Day,
                                                                                    outputTemplate: fileTemplate,
                                                                                    shared: true)))
                                  .WriteTo.Logger(lc => lc.Filter
                                                          .ByIncludingOnly(e => e.Level is LogEventLevel.Information)
                                                          .WriteTo.File(Path.Combine(logsDir, "Info..log"),
                                                                        rollingInterval: RollingInterval.Day,
                                                                        outputTemplate: fileTemplate,
                                                                        shared: false))
                                  .WriteTo.Logger(lc => lc
                                                        .Filter
                                                        .ByIncludingOnly(e => e.Level is LogEventLevel.Warning)
                                                        .WriteTo.File(Path.Combine(logsDir, "Warn..log"),
                                                                      rollingInterval: RollingInterval.Day,
                                                                      outputTemplate: fileTemplate,
                                                                      shared: false))
                                  .WriteTo.Logger(lc => lc
                                                        .Filter
                                                        .ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                                                        .WriteTo.File(Path.Combine(logsDir, "Error..log"),
                                                                      rollingInterval: RollingInterval.Day,
                                                                      outputTemplate: fileTemplate,
                                                                      shared: false));

        if (loggerSettings.GetSection("LogLevel") is { } levels)
        {
            var defaultLevel = LogEventLevel.Verbose;

            if (levels.GetSection("Default").Get<string>() is { } defaultLevelSetting
                && Enum.TryParse(defaultLevelSetting, false, out LogEventLevel defLevel))
            {
                defaultLevel = defLevel;
            }

            // setting default level
            loggerConfiguration = loggerConfiguration.MinimumLevel.Is(defaultLevel);

            foreach (var level in levels.GetChildren())
            {
                if (level.Key.Equals("Default", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (level.Get<string>() is { } levelSetting
                    && Enum.TryParse(levelSetting, false, out LogEventLevel logLevel))
                {
                    loggerConfiguration = loggerConfiguration.MinimumLevel.Override(level.Key, logLevel);
                }
            }
        }

        var logger = loggerConfiguration.CreateLogger();

        services.AddLogging(logging =>
        {
            logging.ClearProviders();

            logging.AddSerilog(logger, true);
        });

        Log.Logger = logger;
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddSingleton<IShutdownMonitor>(ShutdownMonitor);
        services.AddSingleton<ExceptionHandler>();

        services.AddSingleton<ISharpCore, SharpCore>();
        services.AddSingleton<ICoreLibraryModuleManager, LibraryModuleManager>();
        services.AddSingleton<ICoreClientManager, ClientManager>();
        services.AddSingleton<ICoreEntityManager, EntityManager>();
        services.AddSingleton<ICoreEventManager, EventManager>();
        services.AddSingleton<ICoreConVarManager, ConVarManager>();
        services.AddSingleton<ICoreHookManager, HookManager>();
        services.AddSingleton<ICoreTransmitManager, TransmitManager>();
        services.AddSingleton<ICoreFileManager, FileManager>();
        services.AddSingleton<ICoreSchemaManager, SchemaManager>();
        services.AddSingleton<ICoreEconItemManager, EconItemManager>();
        services.AddSingleton<ICoreSoundManager, SoundManager>();
        services.AddSingleton<ICorePhysicsQueryManager, PhysicsQueryManager>();
        services.AddSingleton<ICoreSharpModuleManager, SharpModuleManager>();
        services.AddSingleton<ISharedManager, SharedManager>();
    }

    private static void StartServices(IServiceProvider services)
    {
        // ensure all services have been created!
        services.GetRequiredService<ICoreClientManager>();
        services.GetRequiredService<ICoreLibraryModuleManager>();
        services.GetRequiredService<ICoreEntityManager>();
        services.GetRequiredService<ICoreEventManager>();
        services.GetRequiredService<ICoreTransmitManager>();
        services.GetRequiredService<ICoreFileManager>();
        services.GetRequiredService<ICoreConVarManager>();
        services.GetRequiredService<ICoreSchemaManager>();
        services.GetRequiredService<ICoreEconItemManager>();
        services.GetRequiredService<ICoreSoundManager>();
        services.GetRequiredService<ICorePhysicsQueryManager>();
        services.GetRequiredService<ICoreSharpModuleManager>();

        services.GetRequiredService<ExceptionHandler>().Start();
        services.GetRequiredService<ISharpCore>().InitMainThread();
    }

    private static bool Boot(IServiceProvider services, IConfiguration _)
    {
        var manager = services.GetRequiredService<ICoreSharpModuleManager>();

        return manager.InitModules();
    }
}
