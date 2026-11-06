using Azure.Identity;
using Azure.Messaging.ServiceBus;

namespace Invoice.CqrsAPI.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// ServiceBus:Enabled = false                      -> events are only logged (default)
    /// ServiceBus:ConnectionString = "Endpoint=..."    -> connection string (local emulator, quick demos)
    /// ServiceBus:FullyQualifiedNamespace = "x.servicebus.windows.net"
    ///                                                 -> managed identity / DefaultAzureCredential (Azure, recommended: no secrets)
    /// </summary>
    public static IServiceCollection AddEventPublishing(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("ServiceBus");

        if (!section.GetValue<bool>("Enabled"))
        {
            services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
            return services;
        }

        var connectionString = section["ConnectionString"];
        var fullyQualifiedNamespace = section["FullyQualifiedNamespace"];

        // Fail fast but not forever when the broker is unreachable: the API must stay responsive.
        var options = new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                MaxRetries = 2,
                Delay = TimeSpan.FromMilliseconds(500),
                TryTimeout = TimeSpan.FromSeconds(10)
            }
        };

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton(_ => new ServiceBusClient(connectionString, options));
        }
        else if (!string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
        {
            services.AddSingleton(_ => new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential(), options));
        }
        else
        {
            throw new InvalidOperationException(
                "ServiceBus:Enabled is true, but neither ServiceBus:ConnectionString nor ServiceBus:FullyQualifiedNamespace is set.");
        }

        services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();

        return services;
    }
}
