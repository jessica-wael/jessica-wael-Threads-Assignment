using System.Diagnostics;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2;

public class Phase2Runner
{
    
    private readonly ProductRepository _products = new();
    private readonly OrderRepository _orders = new();
    private readonly IncomingOrderRepository _incoming = new();
    private readonly OrderValidation _validator = new();
    private readonly OrderPricer _pricer;
    private readonly OrderProcessorV2 _processor;
    private readonly ReportGenerator _reportGenerator = new();

    public Phase2Runner()
    {
        _pricer = new OrderPricer(new PricingService(new[] { 7 }));
        _processor = new OrderProcessorV2(_products, _orders, _pricer, new InvoiceGenerator());
    }

    public  async Task RunAsync(CancellationToken ct = default)
    {

        await DataSeeder.ResetAsync(ct);
        await new RaceConditionDemo().RunAsync(ct);

        await DataSeeder.ResetAsync(ct);
        await ComparePricingAsync(ct);

        await DataSeeder.ResetAsync(ct);
        var total = Stopwatch.StartNew();
        var incoming = await _incoming.GetAllAsync(ct);

        var validations = new List<Task>();
        foreach (var order in incoming)
            validations.Add(_validator.ValidateOrderAsync(order, ct));

        foreach (var v in validations)
            await v;

        var validated = incoming.Where(o => o.Status == OrderStatus.Validated).ToList();
        Log.Info("Phase2Runner", $"Validated: {validated.Count}, Rejected: {incoming.Count - validated.Count}");

        var tasks = new List<Task<bool>>();
        foreach (var order in validated)
            tasks.Add(_processor.ProcessValidatedOrderAsync(order, ct));

        if (tasks.Count > 0)
            Log.Info("Phase2Runner", $"task[0] status right after start: {tasks[0].Status}");

        await Task.Delay(1000, ct);

        for (int i = 0; i < tasks.Count; i++)
            Log.Info("Phase2Runner", $"task[{i}] status while running: {tasks[i].Status}");

        
        for (int i = 0; i < tasks.Count; i++)
        {
            var order = validated[i];
            try
            {
                await tasks[i];
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error("Phase2Runner", $"Order #{order.Id} failed: {ex.Message}");

                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;

                foreach (var item in order.Items!)
                    await _products.ReleaseStockAsync(item.ProductId, item.Quantity, ct);

                await _orders.SaveAsync(order, ct);
            }
        }

        for (int i = 0; i < tasks.Count; i++)
            Log.Info("Phase2Runner", $"Order #{validated[i].Id} -> task: {tasks[i].Status}, order: {validated[i].Status}");

        Log.Info("Phase2Runner", $"Total time: {total.Elapsed.TotalSeconds:F2}s");

        
        var saved = await _orders.GetAllAsync(ct);
        Log.Info("Phase2Runner", $"orders.json now has {saved.Count} records");
        Log.Info("Phase2Runner", $"Completed: {saved.Count(o => o.Status == OrderStatus.Completed)}, " +
                                 $"Rejected: {saved.Count(o => o.Status == OrderStatus.Rejected)}, " +
                                 $"Failed: {saved.Count(o => o.Status == OrderStatus.Failed)}");
        
        var reportTask = _reportGenerator.GenerateAsync(ct);
        Log.Info("Phase2Runner", "Generating report...");

        var report = await reportTask;
        Log.Info("Phase2Runner", $"TotalOrders: {report.TotalOrders}");
        Log.Info("Phase2Runner", $"Completed: {report.CompletedCount}, Rejected: {report.RejectedCount}, Failed: {report.FailedCount}, PaymentFailed: {report.PaymentFailedCount}");
        Log.Info("Phase2Runner", $"TotalRevenue: {report.TotalRevenue:F2}, AverageOrderValue: {report.AverageOrderValue:F2}");
        Log.Info("Phase2Runner", $"TopSellingProduct: {report.TopSellingProduct} ({report.TopSellingQuantity} units)");
    }

    private async Task ComparePricingAsync(CancellationToken ct)
    {
        var orders = await _incoming.GetAllAsync(ct);
        var order = orders.First(o => o.Id == 1);

        var parallelWatch = Stopwatch.StartNew();
        await _pricer.PriceOrderAsync(order, ct);
        parallelWatch.Stop();
        Log.Info("Phase2Runner", $"Parallel pricing took {parallelWatch.Elapsed.TotalSeconds:F2}s");

        var sequentialWatch = Stopwatch.StartNew();
        await _pricer.PriceOrderSequentialAsync(order, ct);
        sequentialWatch.Stop();
        Log.Info("Phase2Runner", $"Sequential pricing took {sequentialWatch.Elapsed.TotalSeconds:F2}s");
    }
}