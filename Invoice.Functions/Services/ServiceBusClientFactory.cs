using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Invoice.Functions.Services;

/// <summary>
/// Builds a ServiceBusClient from the SAME app setting the triggers use ("ServiceBusConnection"):
///   ServiceBusConnection                         = "Endpoint=sb://..."          -> connection string (emulator / demo)
///   ServiceBusConnection__fullyQualifiedNamespace = "x.servicebus.windows.net"   -> managed identity (Azure)
/// </summary>
public static class ServiceBusClientFactory
{
    public const string ConnectionName = "ServiceBusConnection";

    public static ServiceBusClient Create(IConfiguration configuration)
    {
        var connectionString = configuration[ConnectionName];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return new ServiceBusClient(connectionString);
        }

        var fullyQualifiedNamespace = configuration[$"{ConnectionName}:fullyQualifiedNamespace"];

        if (!string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
        {
            return new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
        }

        throw new InvalidOperationException(
            $"Neither '{ConnectionName}' nor '{ConnectionName}__fullyQualifiedNamespace' is configured.");
    }
}
