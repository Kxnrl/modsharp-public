using System;
using Sharp.Shared.Abstractions;

namespace ExampleSharpExtension;

internal sealed class ExampleSharpExtension : ISharpExtension, IExampleSharpExtension
{
    public void Load()
    {
        Console.WriteLine("Example extension loaded.");
    }

    public void Shutdown()
    {
        Console.WriteLine("Example extension shut down.");
    }

    public void CallMe()
    {
        Console.WriteLine("Call me.");
    }
}
