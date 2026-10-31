using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
namespace OrderProcessing.Phase1;

public class IncomingOrderRepository
{
    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
    {
        
        Log.Info("IncomingOrderRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("IncomingOrderRepository", "Executing query for incoming orders...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        return await JsonFileStore.ReadAsync<Order>(DataFiles.IncomingOrders, ct);
    }
}