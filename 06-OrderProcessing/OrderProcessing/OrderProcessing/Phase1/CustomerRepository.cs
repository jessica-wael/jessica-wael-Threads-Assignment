namespace OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Logging;

public class CustomerRepository
{
    public async Task<Customer?> GetByIdAsync(int id , CancellationToken ct=default)
    {
        Log.Info("CustomerRepository","Opening connection....");
        await Task.Delay(200,ct);
        
        Log.Info("CustomerRepository", $"Executing query for Customer #{id}...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);
        
        var customers = await JsonFileStore.ReadAsync<Customer>(DataFiles.Customers, ct);
        var customer = customers.FirstOrDefault(c => c.Id == id);

        Log.Info("CustomerRepository", customer is null ? $"Customer #{id} not found" : $"Customer #{id} found ({customer.Name})");
        return customer;   
    }
} 