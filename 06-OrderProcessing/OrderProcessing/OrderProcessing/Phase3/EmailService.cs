using System.Collections.Concurrent;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public interface IEmailService
{
    Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct = default);
}

public class EmailService: IEmailService
{
    private readonly Dictionary<int, int> _failAttempts;
    private readonly ConcurrentDictionary<int, int> _attempts = new();

    public EmailService(Dictionary<int, int>? failAttempts = null)
    {
        _failAttempts = failAttempts ?? new Dictionary<int, int>();
    }

    public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct = default)
    {
        int attempt = _attempts.AddOrUpdate(order.Id, 1, (_, n) => n + 1);

        Log.Info("EmailService", $"Order #{order.Id} -> Render template...");
        await Task.Delay(200, ct);

        Log.Info("EmailService", $"Order #{order.Id} -> Connect to SMTP (attempt {attempt})...");
        await Task.Delay(300, ct);

        if (_failAttempts.TryGetValue(order.Id, out int failCount) && attempt <= failCount)
            throw new SmtpUnavailableException($"SMTP unavailable (attempt {attempt})");

        Log.Info("EmailService", $"Order #{order.Id} -> Send...");
        await Task.Delay(500, ct);
    }
}