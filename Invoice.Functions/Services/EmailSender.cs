using Microsoft.Extensions.Logging;

namespace Invoice.Functions.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, string? attachmentUrl = null, CancellationToken ct = default);
}

/// <summary>
/// Teaching version: "sends" the email by writing it to the log.
/// For real email swap this class for one that calls Azure Communication Services Email
/// (package Azure.Communication.Email, EmailClient.SendAsync). Nothing else in the project changes.
/// </summary>
public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, string? attachmentUrl = null, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL to {To} | {Subject} | {Body} | attachment: {Attachment}",
            to, subject, body, attachmentUrl ?? "(none)");

        return Task.CompletedTask;
    }
}
