using System.Diagnostics;
using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;

public class SyncVsAsyncBenchmark
{
    public async Task RunAsync()
    {
        var sw = new Stopwatch();

      
        await DataSeeder.ResetAsync(CancellationToken.None);
        var syncOrders = await LoadFirstTenAsync();
        var customers = await LoadCustomersAsync();
        var products = await LoadProductsAsync();
        var syncProcessor = new SyncOrderProcessor(customers, products);

        int syncThreadsBefore = ThreadPool.ThreadCount;
        sw.Restart();
        var threads = syncOrders.Select(o => new Thread(() => syncProcessor.Process(o))).ToList();
        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());
        sw.Stop();
        PrintResult("SYNC (thread per order)", sw.ElapsedMilliseconds, syncThreadsBefore, ThreadPool.ThreadCount, syncOrders);

    
        await DataSeeder.ResetAsync(CancellationToken.None);
        var asyncOrders = await LoadFirstTenAsync();
        var asyncProcessor = BuildProcessor();

        int asyncThreadsBefore = ThreadPool.ThreadCount;
        sw.Restart();
        var tasks = asyncOrders.Select(o => asyncProcessor.ProcessOrderAsync(o)).ToList();
        foreach (var task in tasks)
            await task;
        sw.Stop();
        PrintResult("ASYNC (parallel)", sw.ElapsedMilliseconds, asyncThreadsBefore, ThreadPool.ThreadCount, asyncOrders);
    }

    private static OrderProcessor BuildProcessor() => new(
        new CustomerService(),
        new InventoryService(new ProductRepository()),
        new PricingService(Array.Empty<int>()),
        new PaymentGateway(),
        new OrderRepository(),
        new EmailService());

    private static async Task<List<Order>> LoadFirstTenAsync()
    {
        var all = await new IncomingOrderRepository().GetAllAsync(CancellationToken.None);
        return all.Take(10).ToList();
    }

    private static async Task<List<Customer>> LoadCustomersAsync()
    {
        var repo = new CustomerRepository();
        var list = new List<Customer>();
        for (int id = 1; id <= 10; id++)
        {
            var c = await repo.GetByIdAsync(id, CancellationToken.None);
            if (c is not null) list.Add(c);
        }
        return list;
    }

    private static async Task<List<Product>> LoadProductsAsync()
    {
        var repo = new ProductRepository();
        var list = new List<Product>();
        for (int id = 1; id <= 8; id++)
        {
            var p = await repo.GetByIdAsync(id, CancellationToken.None);
            if (p is not null) list.Add(p);
        }
        return list;
    }

    private static void PrintResult(string name, long ms, int threadsBefore, int threadsAfter, List<Order> orders)
    {
        int completed = orders.Count(o => o.Status == OrderStatus.Completed);
        int rejected = orders.Count(o => o.Status == OrderStatus.Rejected);
        Console.WriteLine($"{name} -> {ms} ms | ThreadPool {threadsBefore} -> {threadsAfter} | Completed: {completed}, Rejected: {rejected}");
    }
}