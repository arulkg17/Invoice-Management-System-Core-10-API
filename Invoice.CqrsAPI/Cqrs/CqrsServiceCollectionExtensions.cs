using System.Reflection;

namespace Invoice.CqrsAPI.Cqrs;

public static class CqrsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the dispatcher, the pipeline behaviors, and every handler / validator
    /// found in the given assembly. Adding a new feature needs NO change in Program.cs.
    /// </summary>
    public static IServiceCollection AddCqrs(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        // Order matters: first registered = outermost. Logging sees validation failures too.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        var concreteTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var type in concreteTypes)
        {
            foreach (var contract in type.GetInterfaces().Where(i => i.IsGenericType))
            {
                var definition = contract.GetGenericTypeDefinition();

                if (definition == typeof(IRequestHandler<,>) || definition == typeof(IValidator<>))
                {
                    services.AddScoped(contract, type);
                }
            }
        }

        return services;
    }
}
