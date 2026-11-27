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
    ///     Unclamped duration of the previous frame in seconds.
    /// </summary>
    /// <remarks>
    ///     Despite its Source engine field name, this is not a standard deviation: it is the unclamped duration of the previous frame in seconds,
    ///     sent as <c>CNETMsg_Tick.host_unfiltered_frametime</c>; <see cref="AbsoluteFrameTime" /> is this value clamped to <c>[0.0001, 0.1]</c>.
    /// </remarks>
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
    ///     Client interpolation fraction (always <c>0</c> on a dedicated server).
    /// </summary>
    /// <remarks>
    ///     Not a render time: it is the client's interpolation fraction. On a dedicated server the engine only zeroes it
    ///     at construction, so it always reads <c>0</c>.
    /// </remarks>
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
    ///     Maximum number of entities.
    /// </summary>
    /// <remarks>Refers to maximum Edict count.</remarks>
    int MaxEntities { get; }
}
