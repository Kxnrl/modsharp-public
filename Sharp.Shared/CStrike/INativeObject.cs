// ReSharper disable InconsistentNaming

// consider move these definitions below to Sharp.Shared/Natives

using System;

namespace Sharp.Shared.CStrike;

public interface IContextObject
{
    /// <summary>
    ///     Check if the pointer is no longer valid within the current context. <br />
    ///     Used to prevent calling native methods on objects that are already released.
    /// </summary>
    bool IsDisposed { get; }
}

public interface INativeObject : IContextObject, IEquatable<INativeObject>
{
    /// <summary>
    ///     Get pointer address
    /// </summary>
    nint GetAbsPtr();

    int GetHashCode();

    bool Equals(object? obj);
}

public interface INativeCreatable<out T>
{
    static abstract T? Create(nint ptr);
}

public interface INativeSizeable
{
    static abstract uint NativeSize { get; }
}
