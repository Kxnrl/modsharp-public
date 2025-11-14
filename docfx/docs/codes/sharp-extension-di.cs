using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared;
using Sharp.Shared.Abstractions;

namespace ExampleSharpExtension;

public static class DependencyInjection
{
    // Must provide both of them!

    extension(IServiceCollection services)
    {
        public IServiceCollection AddExampleSharpExtension(ISharedSystem shared)
            => services
               .AddSingleton(shared)
               .AddSingleton<ExampleSharpExtension>()
               .AddSingleton<ISharpExtension, ExampleSharpExtension>(x => x.GetRequiredService<ExampleSharpExtension>())
               .AddSingleton<IExampleSharpExtension, ExampleSharpExtension>(x => x.GetRequiredService<ExampleSharpExtension>());

        public IServiceCollection AddExampleSharpExtension()
        {
            if (services.All(s => s.ServiceType != typeof(ISharedSystem)))
            {
                throw new InvalidOperationException(
                    $"{typeof(ISharedSystem).FullName} is not registered in the service collection. Please register it before adding ExampleSharpExtension.");
            }

            return services
                   .AddSingleton<ExampleSharpExtension>()
                   .AddSingleton<ISharpExtension, ExampleSharpExtension>(x => x.GetRequiredService<ExampleSharpExtension>())
                   .AddSingleton<IExampleSharpExtension, ExampleSharpExtension>(x => x.GetRequiredService<ExampleSharpExtension>());
        }
    }
}
