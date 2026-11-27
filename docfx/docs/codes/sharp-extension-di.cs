using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace ExampleSharpExtension;

public static class DependencyInjection
{
    public static IServiceCollection AddExampleSharpExtension(this IServiceCollection services, ISharedSystem shared)
    {
        services.AddSingleton(shared);
        return services.AddExampleSharpExtension();
    }

    public static IServiceCollection AddExampleSharpExtension(this IServiceCollection services)
    {
        if (services.All(s => s.ServiceType != typeof(ISharedSystem)))
        {
            throw new InvalidOperationException(
                "Register ISharedSystem before adding ExampleSharpExtension.");
        }

        // Both interfaces must resolve the SAME instance so Load/Shutdown manage the service being used.
        return services
               .AddSingleton<ExampleSharpExtension>()
               .AddSingleton<ISharpExtension>(x => x.GetRequiredService<ExampleSharpExtension>())
               .AddSingleton<IExampleSharpExtension>(x => x.GetRequiredService<ExampleSharpExtension>());
    }
}
