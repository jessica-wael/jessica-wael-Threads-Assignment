using System.Diagnostics;
using OrderProcessing.Phase3;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4;

public class LiveCheckout
{
    private readonly OrderProcessor _processor;

    public LiveCheckout(OrderProcessor processor)
    {
        _processor = processor;
    }
    
    private async Task<(Order Order, double Seconds)> RunOneAsync(Order order, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _processor.ProcessOrderAsync(order, ct);
        }
        catch (Exception ex)
        {
            order.Status = OrderStatus.Failed;
            order.FailureReason = ex.Message;
        }
        return (order, sw.Elapsed.TotalSeconds);
    }

    public async Task RunAsync(List<Order> orders, CancellationToken ct = default)
    {
        var total = Stopwatch.StartNew();

     
        var tasks = orders.Select(o => RunOneAsync(o, ct)).ToList();

        
        int done = 0;
        await foreach (var finished in Task.WhenEach(tasks))
        {
            var (order, seconds) = await finished;
            done++;

            string result = order.FailureReason is null
                ? order.Status.ToString()
                : $"{order.Status} ({order.FailureReason})";

            Console.WriteLine($"[{done,2}/{orders.Count}] Order #{order.Id} -> {result} after {seconds:F1} s");
        }

       
        Console.WriteLine($"Total time: {total.Elapsed.TotalSeconds:F1} s");
        foreach (var group in orders.GroupBy(o => o.Status).OrderBy(g => g.Key))
            Console.WriteLine($"{group.Key}: {group.Count()}");
    }
}