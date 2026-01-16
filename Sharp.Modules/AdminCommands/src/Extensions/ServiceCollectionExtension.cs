using Microsoft.Extensions.DependencyInjection;
using Sharp.Modules.AdminCommands.Shared;

namespace Sharp.Modules.AdminCommands.Extensions;

internal static class ServiceCollectionExtension
{
    public static void AddCommandService<TImpl, TInterface>(this IServiceCollection services)
        where TImpl : class, TInterface, ICommandCategory
        where TInterface : class
    {
        services.AddSingleton<TImpl>();
        services.AddSingleton<TInterface>(sp => sp.GetRequiredService<TImpl>());
        services.AddSingleton<ICommandCategory>(sp => sp.GetRequiredService<TImpl>());
    }

    public static void AddOperationHandler<TH>(this IServiceCollection services) where TH : class, IAdminOperationHandler
    {
        services.AddSingleton<TH>();
        services.AddSingleton<IAdminOperationHandler>(sp => sp.GetRequiredService<TH>());
    }
}
