using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;

public class Phase3Runner
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        await DataSeeder.ResetAsync(ct);
        
        var processor = new OrderProcessor(
            new CustomerService(),
            new InventoryService(new ProductRepository()),
            new PricingService(Array.Empty<int>()),
            new PaymentGateway(timeoutOrderIds: new[] { 5 }, declineOrderIds: new[] { 8 }),
            new OrderRepository(failSaveForOrderIds: new[] { 13 }),
            new EmailService(new Dictionary<int, int> { [3] = 2, [17] = int.MaxValue }));

        var incoming = await new IncomingOrderRepository().GetAllAsync(ct);

        var tasks = new List<Task<bool>>();
        foreach (var order in incoming)
            tasks.Add(processor.ProcessOrderAsync(order, ct));

        for (int i = 0; i < tasks.Count; i++)
        {
            try
            {
                await tasks[i];
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error("Phase3Runner", $"Order #{incoming[i].Id} crashed: {ex.Message}");
            }
        }

        Log.Info("Phase3Runner", "===== Final table ====");
        foreach (var o in incoming)
            Log.Info("Phase3Runner", $"{o.Id,3} | {o.Status,-14} | {o.TotalAmount,10:F2} | {o.FailureReason}");
    }
}