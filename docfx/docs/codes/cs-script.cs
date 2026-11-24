using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Helpers;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace ModSharpDocs.CheckpointBridge;

public sealed class CheckpointBridge : IModSharpModule
{
    private readonly IScriptManager _scripts;

    public CheckpointBridge(ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration configuration,
        bool hotReload)
    {
        _scripts = sharedSystem.GetScriptManager();
    }

    public string DisplayName => "ModSharp Docs Checkpoint Bridge";
    public string DisplayAuthor => "ModSharp Dev Team";

    public bool Init()
    {
        _scripts.RegisterMethod("ModSharpDocs_CheckpointAwardV1", Award);
        Console.WriteLine("[CheckpointBridge] Registered checkpoint award method.");
        return true;
    }

    public void Shutdown()
    {
        // The host removes this module's method delegate. No JS callback is retained.
        Console.WriteLine("[CheckpointBridge] Unloaded.");
    }

    private ScriptReturnValue? Award(IScriptCallContext call)
    {
        // call and every ScriptArray/ScriptObject below are borrowed for this invocation.
        if (call.ArgCount != 3)
        {
            call.ThrowError("Expected (checkpoint: string, points: number[], notify: function).");
        }

        var checkpoint = call.GetString(0);
        if (string.IsNullOrWhiteSpace(checkpoint))
        {
            call.ThrowError("checkpoint must be a non-empty string.");
        }

        var points = call.GetArray(1);
        if (points is not { } awards || awards.Length is < 1 or > 16)
        {
            throw new ScriptThrowException("points must contain between 1 and 16 numbers.");
        }

        var awarded = 0.0;
        for (var index = 0; index < awards.Length; index++)
        {
            if (awards.GetNumber(index) is not { } value || !double.IsFinite(value) || value <= 0)
            {
                throw new ScriptThrowException("Each point value must be a finite, positive number.");
            }

            awarded += value;
        }

        // GetFunction creates a persistent native handle; even an immediate call needs Dispose.
        using var notify = call.GetFunction(2);
        if (notify is null)
        {
            call.ThrowError("notify must be a JavaScript function.");
        }

        if (!_scripts.TryReadVariable(call.Script, "checkpointState.total", out var current)
            || current?.Number is not { } total || !double.IsFinite(total) || total < 0)
        {
            throw new ScriptThrowException("checkpointState.total is not an initialized, non-negative number.");
        }

        if (!_scripts.TryReadVariable(call.Script, "scoreLimit", out var limitValue)
            || limitValue?.Number is not { } limit || !double.IsFinite(limit) || limit <= 0)
        {
            throw new ScriptThrowException("scoreLimit is not an initialized, positive number.");
        }

        var nextTotal = total + awarded;
        if (!double.IsFinite(nextTotal) || nextTotal > limit)
        {
            call.ThrowError("This award would exceed scoreLimit.");
        }

        if (!_scripts.TryWriteVariable(call.Script, "checkpointState.total", new ScriptReturnValue(nextTotal)))
        {
            call.ThrowError("Could not update checkpointState.total.");
        }

        // Synchronous JS re-entry. The score is already committed; notification is not a transaction.
        var acknowledged = notify.Invoke(checkpoint, nextTotal)?.Bool == true;

        var receipt = call.NewObject()
            .SetString("checkpoint", checkpoint)
            .SetNumber("awarded", awarded)
            .SetNumber("total", nextTotal)
            .SetBool("acknowledged", acknowledged)
            .SetArray("awards", awards);

        // The native trampoline resolves this handle before reclaiming the call's handle frame.
        return new ScriptReturnValue(receipt);
    }
}
