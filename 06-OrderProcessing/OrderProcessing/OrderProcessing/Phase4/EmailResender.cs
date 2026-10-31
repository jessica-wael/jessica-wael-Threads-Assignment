using System.Collections.Concurrent;
using System.Diagnostics;
using OrderProcessing.Phase3;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase4;

public class EmailResender
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private readonly IEmailService _email;
    private int _running;
    private int _maxRunning;

    public EmailResender(IEmailService email)
    {
        _email = email;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var customer = new Customer { Id = 1, Name = "Test Customer", Email = "test@example.com", IsActive = true };

        var orders = Enumerable.Range(1001, 30)
            .Select(id => new Order
            {
                Id = id,
                CustomerId = customer.Id,
                Items = new List<OrderItem>(),
                Status = OrderStatus.Completed
            })
            .ToList();

        var threads = new ConcurrentDictionary<int, byte>();
        var sw = Stopwatch.StartNew();

        var options = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct };

        await Parallel.ForEachAsync(orders, options, async (order, token) =>
        {
            int now = Interlocked.Increment(ref _running);
            int seen;
            do { seen = _maxRunning; }
            while (now > seen && Interlocked.CompareExchange(ref _maxRunning, now, seen) != seen);

            threads.TryAdd(Environment.CurrentManagedThreadId, 0);

            try
            {
                await _email.SendConfirmationAsync(order, customer, token);
                await AppendAsync(order, customer, token);
            }
            finally
            {
                Interlocked.Decrement(ref _running);
            }
        });

        sw.Stop();

        var saved = await JsonFileStore.ReadAsync<EmailMessage>(DataFiles.Emails, ct);
        var mine = saved.Where(e => e.OrderId >= 1001 && e.OrderId <= 1030).ToList();

        Console.WriteLine($"Sent 30 emails in {sw.Elapsed.TotalSeconds:F1} s");
        Console.WriteLine($"Highest sends at the same time: {_maxRunning}");
        Console.WriteLine($"Emails in emails.json for orders 1001-1030: {mine.Count} (distinct: {mine.Select(e => e.OrderId).Distinct().Count()})");
        Console.WriteLine($"Distinct threads: {threads.Count}, ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");
    }

    private static async Task AppendAsync(Order order, Customer customer, CancellationToken ct)
    {
        var message = new EmailMessage
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            To = customer.Email,
            Subject = $"Order #{order.Id} confirmed",
            Body = $"Thank you {customer.Name}, your order #{order.Id} is confirmed.",
            SentAt = DateTime.Now,
            Attempts = 1,
            LastError = null
        };

        await FileLock.WaitAsync(ct);
        try
        {
            var list = await JsonFileStore.ReadAsync<EmailMessage>(DataFiles.Emails, ct);
            list.Add(message);
            await JsonFileStore.WriteAsync(DataFiles.Emails, list, ct);
        }
        finally
        {
            FileLock.Release();
        }
    }
}