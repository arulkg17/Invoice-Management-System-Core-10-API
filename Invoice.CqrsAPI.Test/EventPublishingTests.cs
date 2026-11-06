using Invoice.CqrsAPI.Messaging;
using Invoice.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Invoice.CqrsAPI.Test;

public class EventPublishingTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventPublishing(config);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Disabled_by_default_uses_the_no_op_publisher()
    {
        using var sp = Build(new() { ["ServiceBus:Enabled"] = "false" });

        Assert.IsType<NoOpEventPublisher>(sp.GetRequiredService<IEventPublisher>());
    }

    [Fact]
    public async Task No_op_publisher_never_throws()
    {
        using var sp = Build(new());

        await sp.GetRequiredService<IEventPublisher>()
            .PublishAsync(new SalesInvoicePostedEvent { InvoiceId = 1 });
    }

    [Fact]
    public async Task Enabled_with_a_connection_string_uses_the_service_bus_publisher()
    {
        // await using: the Service Bus client is IAsyncDisposable only.
        await using var sp = Build(new()
        {
            ["ServiceBus:Enabled"] = "true",
            ["ServiceBus:ConnectionString"] =
                "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;"
        });

        // Creating the publisher does not connect to anything yet.
        Assert.IsType<ServiceBusEventPublisher>(sp.GetRequiredService<IEventPublisher>());
    }

    [Fact]
    public void Enabled_without_any_connection_setting_fails_fast_at_startup()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ServiceBus:Enabled"] = "true" })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddEventPublishing(config));
    }
}
