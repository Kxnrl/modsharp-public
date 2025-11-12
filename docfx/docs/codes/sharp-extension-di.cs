using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace ExampleSharpExtension;

public static class DependencyInjection
{
    // Must provide both of them!

    extension(IServiceCollection self)
    {
        public IServiceCollection AddSharpExtension(ISharedSystem shared)
            => self
                .AddSingleton(shared)
                .AddSingleton<ExampleSharpExtension>()
                .AddSingleton<ISharpExtension, ExampleSharpExtension>(x => x.GetRequiredService<ExampleSharpExtension>())
                .AddSingleton<IExampleSharpExtension, ExampleSharpExtension>(x =>
                    x.GetRequiredService<ExampleSharpExtension>());

    public IServiceCollection AddSharpExtension()
    {
        if (self.All(s => s.ServiceType != typeof(ISharedSystem)))
        {
            throw new InvalidOperationException(
                $"{typeof(ISharedSystem).FullName} is not registered in the service collection. Please register it before adding ExampleSharpExtension.");
        }
        return self
            .AddSingleton<ExampleSharpExtension>()
            .AddSingleton<ISharpExtension, ExampleSharpExtension>(x => x.GetRequiredService<ExampleSharpExtension>())
            .AddSingleton<IExampleSharpExtension, ExampleSharpExtension>(x =>
                x.GetRequiredService<ExampleSharpExtension>());
    }
}
}
