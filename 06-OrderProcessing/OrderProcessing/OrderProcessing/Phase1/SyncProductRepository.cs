using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Storage;
namespace OrderProcessing.Phase1;

public class SyncProductRepository
{
    public Product? GetById(int id)
    {
        Log.Info("SyncProductRepository","Opening connection...");
        Thread.Sleep(200);
        
        Log.Info("SyncProductRepository",$"Executing query for Customer #{id}...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        var products = JsonFileStore.Read<Product>(DataFiles.Products);
        var product = products.FirstOrDefault(p=>p.Id == id);
        
        Log.Info("SyncProductRepository",product is null? $"Product #{id} not found" : $"Product #{id} found ({product.Name})");
        return product;
    }

    public bool ReserveStock(int productId, int qty)
    {
        Log.Info("SyncProductRepository","Opening connection...");
        Thread.Sleep(200);
        
        Log.Info("SyncProductRepository",$"Beginning transaction...");
        Thread.Sleep(100);

        Log.Info("SyncProductRepository",$"Updating stoch for #{productId}...");
        Thread.Sleep(400);
        
        var products = JsonFileStore.Read<Product>(DataFiles.Products); 
        var product = products.FirstOrDefault(p=>p.Id == productId);

        if (product is null || product.StockQuantity < qty)
        {
            return false;
        }
        product.StockQuantity -= qty;
        JsonFileStore.Write(DataFiles.Products , products);
        
        Log.Info("SyncProductRepository",$"Confirming transaction...");
        Thread.Sleep(200);

        return true;
    }
}