using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace ExampleSharpExtension;

internal class ExampleSharpExtension : ISharpExtension, IExampleSharpExtension
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
