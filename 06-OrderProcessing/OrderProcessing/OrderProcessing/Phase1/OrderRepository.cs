using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
namespace OrderProcessing.Phase1;


public class OrderRepository
{
    private static readonly SemaphoreSlim OrdersLock = new(1, 1);

    private readonly HashSet<int> _failSaveForOrderIds;

    public OrderRepository() : this(Array.Empty<int>()) { }

    public OrderRepository(IEnumerable<int> failSaveForOrderIds)
    {
        _failSaveForOrderIds = new HashSet<int>(failSaveForOrderIds);
    }
    
    
    public  async Task SaveAsync(Order order,CancellationToken ct = default)
    {
        await OrdersLock.WaitAsync(ct);
        try
        {
            Log.Info("OrderRepository", "Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("OrderRepository", "Beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info("OrderRepository", $"Saving Order #{order.Id}...");
            await Task.Delay(400, ct);
            
            if (order.Status == OrderStatus.Completed && _failSaveForOrderIds.Contains(order.Id))
                throw new DatabaseWriteException("Connection lost during commit");

            var orders = await JsonFileStore.ReadAsync<Order>(DataFiles.Orders, ct);
            var index = orders.FindIndex(o => o.Id == order.Id);
            if (index >= 0) orders[index] = order;
            else orders.Add(order);
            await JsonFileStore.WriteAsync(DataFiles.Orders, orders, ct);

            Log.Info("OrderRepository", "Confirming transaction...");
            await Task.Delay(200, ct);
        }
        finally
        {
            OrdersLock.Release();
        }
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
    {
        Log.Info("OrderRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("OrderRepository", "Executing query for all orders...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        return await JsonFileStore.ReadAsync<Order>(DataFiles.Orders, ct);
    }
    
    public  async Task<List<Order>> GetByStatusAsync(OrderStatus s,CancellationToken ct=default)
    {
        Log.Info("OrderRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("OrderRepository", $"Executing query for orders with status {s}...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        var orders = await JsonFileStore.ReadAsync<Order>(DataFiles.Orders, ct);
        return orders.Where(o => o.Status == s).ToList();
    }
    
}