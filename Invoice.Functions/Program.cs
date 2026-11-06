using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Invoice.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights: logs, traces and failures show up in the Azure portal.
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// ---------- Service Bus client (used by the stock-watch and resend functions) ----------
builder.Services.AddSingleton(sp => ServiceBusClientFactory.Create(sp.GetRequiredService<IConfiguration>()));

// ---------- Blob storage (PDF archive + audit log) ----------
builder.Services.AddSingleton(sp =>
    new BlobServiceClient(sp.GetRequiredService<IConfiguration>()["AzureWebJobsStorage"]));

// ---------- IMS API (the Functions never touch the database: they call the API) ----------
builder.Services.AddSingleton<ImsTokenCache>();
builder.Services.AddHttpClient<ImsApiClient>((sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>()["ImsApi:BaseUrl"]
        ?? throw new InvalidOperationException("ImsApi:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ---------- Email ----------
// LogEmailSender only writes to the log. In production replace it with Azure Communication Services Email.
builder.Services.AddSingleton<IEmailSender, LogEmailSender>();

builder.Build().Run();
