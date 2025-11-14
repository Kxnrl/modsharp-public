using System;
using Sharp.Shared.Abstractions;

namespace ExampleSharpExtension;

internal sealed class ExampleSharpExtension : ISharpExtension, IExampleSharpExtension
{
    public void Load()
    {
        Console.WriteLine("[]");
    }

    public void Shutdown()
    {
    }

    public void CallMe()
    {
        Console.WriteLine("Call me.");
    }
}
