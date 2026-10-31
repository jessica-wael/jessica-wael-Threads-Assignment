using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

using OrderProcessing.Phase1;

namespace OrderProcessing.Phase2;

public class RaceConditionDemo
{
    public async Task RunAsync(CancellationToken ct=default)
    {
        //unsafe reservation
        Log.Info("RaceConditionDemo", "UNSAFE: resetting keyboard stock to 1");
        var product = new ProductRepository();
        
        var products = await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);
        products.First(p => p.Id == 4).StockQuantity = 1;
        await JsonFileStore.WriteAsync(DataFiles.Products, products, ct);

        var task10 = product.ReserveStockUnsafeAsync(4, 1, ct);
        var task11 = product.ReserveStockUnsafeAsync(4, 1, ct);

        bool result10 = await task10;
        bool result11 = await task11;

        var after = await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);
        int stock = after.First(p => p.Id == 4).StockQuantity;

        Log.Info("RaceConditionDemo", $"UNSAFE - Order 10 reserved: {result10}");
        Log.Info("RaceConditionDemo", $"UNSAFE - Order 11 reserved: {result11}");
        Log.Info("RaceConditionDemo", $"UNSAFE - Keyboard stock: {stock}");
        
        
       // safe reservation
        var products2 = await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);
        products2.First(p => p.Id == 4).StockQuantity = 1;
        await JsonFileStore.WriteAsync(DataFiles.Products, products2, ct);

        var safe10 = product.ReserveStockAsync(4, 1, ct);
        var safe11 = product.ReserveStockAsync(4, 1, ct);

        bool safeResult10 = await safe10;
        bool safeResult11 = await safe11;

        var after2 = await JsonFileStore.ReadAsync<Product>(DataFiles.Products, ct);
        int stock2 = after2.First(p => p.Id == 4).StockQuantity;

        Log.Info("RaceConditionDemo", $"SAFE - Order 10 reserved: {safeResult10}");
        Log.Info("RaceConditionDemo", $"SAFE - Order 11 reserved: {safeResult11}");
        Log.Info("RaceConditionDemo", $"SAFE - Keyboard stock: {stock2}");
    }
}