using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase4;

public class Phase4Runner
{
    private static (OrderProcessor Processor, CustomerService Customers) BuildCheckoutServices()
    {
        var customers = new CustomerService();

        var processor = new OrderProcessor(
            customers,
            new InventoryService(new ProductRepository()),
            new PricingService(Array.Empty<int>()),
            new PaymentGateway(timeoutOrderIds: new[] { 5 }, declineOrderIds: new[] { 8 }),
            new OrderRepository(failSaveForOrderIds: new[] { 13 }),
            new EmailService(new Dictionary<int, int> { [3] = 2, [17] = int.MaxValue }));

        return (processor, customers);
    }

    private static IEmailService BuildCleanEmailService()
    {
        return new EmailService(new Dictionary<int, int>());
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var (processor, customers) = BuildCheckoutServices();
        var incomingRepo = new IncomingOrderRepository();

        Section("Task 10.1 - Live checkout progress");
        await DataSeeder.ResetAsync(ct);
        var incoming = await incomingRepo.GetAllAsync(ct);
        await new LiveCheckout(processor).RunAsync(incoming, ct);

        Section("Task 10.2 - Customer dashboard");
        var dashboard = new CustomerDashboardService(customers, new[] { 99 });
        await ShowDashboardAsync(dashboard, 3, ct);
        await ShowDashboardAsync(dashboard, 99, ct);

        Section("Task 10.3 - Fastest shipping quote");
        await RunShippingAsync(incomingRepo, ct);

        Section("Task 10.4 A - Fraud scoring");
        new FraudScorer().Run(ct);

        Section("Task 10.4 B - Email re-send");
        await new EmailResender(BuildCleanEmailService()).RunAsync(ct);
    }

    private static void Section(string title) => Console.WriteLine($"\n===== {title} =====");

    private static async Task ShowDashboardAsync(CustomerDashboardService service, int customerId, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var d = await service.LoadAsync(customerId, ct);
            sw.Stop();

            Console.WriteLine($"{d.Customer.Name} - {d.Orders.Count} orders ({string.Join(", ", d.Orders.Select(o => $"#{o.Id} {o.Status}"))}), " +
                              $"{d.Payments.Count} payments ({string.Join(", ", d.Payments.Select(p => $"#{p.OrderId} {p.Status}"))}), " +
                              $"{d.Emails.Count} email(s) ({string.Join(", ", d.Emails.Select(e => $"#{e.OrderId}"))})");
        }
        catch (AggregateException ex)
        {
            sw.Stop();
            Console.WriteLine($"Customer #{customerId}: {ex.InnerExceptions.Count} failure(s)");
            foreach (var inner in ex.InnerExceptions)
                Console.WriteLine($"  - {inner.Message}");
        }
        Console.WriteLine($"Dashboard time: {sw.ElapsedMilliseconds} ms");
    }

    private static async Task RunShippingAsync(IncomingOrderRepository repo, CancellationToken ct)
    {
        var orders = await repo.GetAllAsync(ct);
        var service = new ShippingQuoteService(new IShippingCarrier[] { new FastShip(), new CairoExpress(), new NileCourier() });

        foreach (var order in orders.Where(o => o.Id <= 5))
        {
            var q = await service.GetFastestQuoteAsync(order, ct);
            string cancelled = q.Cancelled.Count == 0 ? "none" : string.Join(", ", q.Cancelled);
            Console.WriteLine($"Order #{order.Id} -> {q.Carrier}, {q.Price:F2}, {q.ElapsedMs} ms, cancelled: {cancelled}");
        }

        var allFail = new ShippingQuoteService(new IShippingCarrier[]
        {
            new FastShip(new[] { 6 }), new CairoExpress(new[] { 6 }), new NileCourier(new[] { 6 })
        });

        try
        {
            await allFail.GetFastestQuoteAsync(orders.First(o => o.Id == 6), ct);
        }
        catch (ShippingProviderUnavailableException ex)
        {
            Console.WriteLine($"Order #6 -> {ex.Message}");
        }
    }
}