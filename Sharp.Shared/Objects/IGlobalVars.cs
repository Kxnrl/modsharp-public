// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

using Sharp.Shared.CStrike;

namespace Sharp.Shared.Objects;

public interface IGlobalVars : INativeObject
{
    /// <summary>
    ///     Real time in seconds
    /// </summary>
    float RealTime { get; }

    /// <summary>
    ///     Current frame number
    /// </summary>
    int FrameCount { get; }

    /// <summary>
    ///     Frame time based on engine time
    /// </summary>
    float AbsoluteFrameTime { get; }

    /// <summary>
    ///     Frame start time standard deviation based on engine time <br />
    ///     <remarks>
    ///         Not a standard deviation: it is the unclamped duration of the previous frame in seconds, which the engine
    ///         sends as CNETMsg_Tick.host_unfiltered_frametime; AbsoluteFrameTime is this value clamped to [0.0001, 0.1]
    ///     </remarks>
    /// </summary>
    float AbsoluteFrameStartTimeStdDev { get; }

    /// <summary>
    ///     Maximum allowed client connections
    /// </summary>
    int MaxClients { get; }

    /// <summary>
    ///     Time between frames
    /// </summary>
    float FrameTime { get; }

    /// <summary>
    ///     Current game time
    /// </summary>
    float CurTime { get; }

    /// <summary>
    ///     Render time <br />
    ///     <remarks>
    ///         Not a render time: it is the client's interpolation fraction. On a dedicated server the engine only zeroes it
    ///         at construction, so it always reads 0
    ///     </remarks>
    /// </summary>
    float RenderTime { get; }

    /// <summary>
    ///     Whether simulation is running
    /// </summary>
    bool InSimulation { get; }

    /// <summary>
    ///     Current tick count
    /// </summary>
    int TickCount { get; }

    /// <summary>
    ///     Sub-tick fraction
    /// </summary>
    float SubTickFraction { get; }

    /// <summary>
    ///     Current map name
    /// </summary>
    string MapName { get; }

    /// <summary>
    ///     Maximum number of entities <br />
    ///     <remarks>Refers to maximum Edict count</remarks>
    /// </summary>
    int MaxEntities { get; }
}
