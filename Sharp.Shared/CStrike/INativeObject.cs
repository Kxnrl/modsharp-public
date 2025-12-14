// ReSharper disable InconsistentNaming

// consider move these definitions below to Sharp.Shared/Natives

using System;

namespace Sharp.Shared.CStrike;

public interface IContextObject
{
    /// <summary>
    ///     判断指针是否已经离开Context <br />
    ///     该功能仅为避免被释放后仍然调用Native
    /// </summary>
    bool IsDisposed { get; }
}

public interface INativeObject : IContextObject, IEquatable<INativeObject>
{
    /// <summary>
    ///     Get pointer
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
