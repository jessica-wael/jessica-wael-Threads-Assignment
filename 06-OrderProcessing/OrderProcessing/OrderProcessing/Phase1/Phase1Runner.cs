using System.Diagnostics;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class Phase1Runner
{
    
    public  async Task RunAsync(CancellationToken ct=default)
    {
        await DataSeeder.ResetAsync(ct);
        var incoming = await new IncomingOrderRepository().GetAllAsync(ct);
        Log.Info("Phase1Runner started", $"Loaded {incoming.Count} incoming orders");
            
        var syncValidator = new SyncOrderValidator();
        var syncWatch = Stopwatch.StartNew();
        foreach (var order in incoming)
        {
            syncValidator.ValidateOrder(order);
        }
        syncWatch.Stop();
        Log.Info("Phase1Runner", $"Sync run finished in {syncWatch.Elapsed.TotalSeconds:F1} s");
        
        await DataSeeder.ResetAsync(ct);
        var freshOrders = await new IncomingOrderRepository().GetAllAsync(ct);

        var validator = new OrderValidation();
        var orderRepo = new OrderRepository();
        var asyncWatch = Stopwatch.StartNew();
        foreach (var order in freshOrders)
        {
            await validator.ValidateOrderAsync(order, ct);
            await orderRepo.SaveAsync(order, ct);
        }
        asyncWatch.Stop();
        Log.Info("Phase1Runner", $"Async run finished in {asyncWatch.Elapsed.TotalSeconds:F1} s");
        
        var validated = await orderRepo.GetByStatusAsync(OrderStatus.Validated, ct);
        var rejected = await orderRepo.GetByStatusAsync(OrderStatus.Rejected, ct);
        var all = await orderRepo.GetAllAsync(ct);

        Console.WriteLine();
        Console.WriteLine("===== Validation Summary =====");
        Console.WriteLine($"Total Orders: {freshOrders.Count}");
        Console.WriteLine($"Validated:    {validated.Count}");
        Console.WriteLine($"Rejected:     {rejected.Count}");
        foreach (var o in rejected.OrderBy(o => o.Id))
            Console.WriteLine($"  - Order #{o.Id}: {o.FailureReason}");
        Console.WriteLine($"Sync Time: {syncWatch.Elapsed.TotalSeconds:F1} seconds");
        Console.WriteLine($"Async Time: {asyncWatch.Elapsed.TotalSeconds:F1} seconds");
        Console.WriteLine($"orders.json now contains {all.Count} records.");
      
            
    }
        
}