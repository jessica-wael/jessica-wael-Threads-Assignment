namespace OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

public class ProductRepository
{
    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Log.Info("ProductRepository ", "Opening Connection...");
        await Task.Delay(200, ct);
        
        Log.Info("ProductRepository",$"Executing query for Product #{id}...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        var products = await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);
        var product=products.FirstOrDefault(p=>p.Id==id);

        Log.Info("ProductRepository", product is null ? $"Product #{id} not found" : $"Product #{id} found ({product.Name})");
        return product;
    } 
    
    private static readonly SemaphoreSlim StockLock = new(1, 1);
    public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken ct = default)
    {
        await StockLock.WaitAsync(ct);
        try
        {
            Log.Info("ProductRepository", "Safe Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("ProductRepository", "Safe Beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info("ProductRepository", $"Safe Updating stock for product #{productId}...");
            await Task.Delay(400, ct);
            var products = await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);
            var product = products.FirstOrDefault(p => p.Id == productId);

            if (product is null || product.StockQuantity < qty)
            {
                return false;
            }

            product.StockQuantity -= qty;
            await JsonFileStore.WriteAsync(DataFiles.Products, products, ct);

            Log.Info("ProductRepository", "Confirming transaction...");
            await Task.Delay(200, ct);
            return true;
        }
        finally
        {
            StockLock.Release();
        }
    }

    public async Task<bool> ReserveStockUnsafeAsync(int productId, int qty, CancellationToken ct = default)
    {
        var products = await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);
        var product = products.FirstOrDefault(p => p.Id == productId);
        
          if (product is null || product.StockQuantity < qty)
              return false;
          
          Log.Info("ProductRepository", "Unsafe Opening connection...");
          await Task.Delay(200, ct);
       
          Log.Info("ProductRepository", "Unsafe Beginning transaction...");
          await Task.Delay(100, ct);

          Log.Info("ProductRepository", "Unsafe Updating stock...");
          await Task.Delay(400, ct);
        
        product.StockQuantity -= qty;
        await JsonFileStore.WriteAsync(DataFiles.Products, products, ct);
        
        return true;
    }

    public async Task ReleaseStockAsync(int productId, int qty, CancellationToken ct = default)
    {
        await StockLock.WaitAsync(ct);
        try
        {
            Log.Info("ProductRepository", $"Releasing {qty} units of product #{productId}...");
            await Task.Delay(300, ct);
            
            var products = await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);
            var product = products.FirstOrDefault(p => p.Id == productId);

            if (product is null)
            {
                return;
            }
            
            product.StockQuantity += qty;
            await JsonFileStore.WriteAsync(DataFiles.Products, products, ct);
            
        }
        finally
        {
            StockLock.Release();
        }
    }
}