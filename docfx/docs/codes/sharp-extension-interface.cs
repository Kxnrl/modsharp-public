using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace ExampleSharpExtension;

public interface IExampleSharpExtension
{
    void CallMe();
}