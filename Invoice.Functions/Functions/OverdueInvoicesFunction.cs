using Invoice.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Invoice.Functions.Functions;

/// <summary>
/// TRIGGER: timer. No message involved: runs on a schedule (app setting OverdueSchedule, default 08:00 UTC daily).
/// NCRONTAB format has SIX fields: {second} {minute} {hour} {day} {month} {day-of-week}
/// </summary>
public sealed class OverdueInvoicesFunction
{
    private readonly ImsApiClient _api;
    private readonly IEmailSender _email;
    private readonly ILogger<OverdueInvoicesFunction> _logger;

    public OverdueInvoicesFunction(ImsApiClient api, IEmailSender email, ILogger<OverdueInvoicesFunction> logger)
    {
        _api = api;
        _email = email;
        _logger = logger;
    }

    [Function(nameof(OverdueInvoicesFunction))]
    public async Task Run([TimerTrigger("%OverdueSchedule%")] TimerInfo timer, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var overdue = (await _api.GetPostedInvoicesAsync(cancellationToken))
            .Where(i => i.DueDate.HasValue && i.DueDate.Value.Date < today)
            .OrderBy(i => i.DueDate)
            .ToList();

        _logger.LogInformation("Overdue check: {Count} posted invoice(s) past their due date.", overdue.Count);

        if (overdue.Count == 0)
        {
            return;
        }

        var lines = overdue.Select(i =>
            $"{i.InvoiceNumber}  customer {i.CustomerId}  due {i.DueDate:yyyy-MM-dd}  {i.TotalAmount:N2}");

        await _email.SendAsync(
            "accounts@example.com",
            $"{overdue.Count} overdue invoice(s)",
            string.Join(Environment.NewLine, lines),
            ct: cancellationToken);
    }
}
