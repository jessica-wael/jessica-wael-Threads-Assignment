using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class InventoryService
{
    private readonly ProductRepository _products;

    public InventoryService(ProductRepository products)
    {
        _products = products;
    }

    public async Task<bool> ReserveStockAsync(Order order, CancellationToken ct = default)
    {
        var reserved = new List<OrderItem>();
        try
        {
            foreach (var item in  order.Items!)
            {
                var ok = await  _products.ReserveStockAsync(item.ProductId, item.Quantity, ct);
                if (!ok)
                {
                    Log.Error("InventoryService" , $"Order #{order.Id} -> product #{item.ProductId} not available , rolling back {reserved.Count} item");
                    await RollbackAsync(order, reserved);
                    return false;
                }
                reserved.Add(item); 
            }
            
            return true;

        }
        catch (OperationCanceledException)
        {
            await RollbackAsync(order, reserved);
            throw;
        }
        
    }

    public async Task ReleaseStockAsync(Order order, CancellationToken ct = default)
    {
        foreach (var item in order.Items! )
        {
            await _products.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
        }
        
    }
    
    private async Task RollbackAsync(Order order, List<OrderItem> reserved)
    {
        foreach (var item in reserved)
        {
            await _products.ReleaseStockAsync(item.ProductId, item.Quantity, CancellationToken.None);
        }
    }
}