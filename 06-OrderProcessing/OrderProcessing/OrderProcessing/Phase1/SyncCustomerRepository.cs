using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class SyncCustomerRepository
{
    public Customer? GetById(int id)
    {
        Log.Info("SyncCustomerRepository", "Opening connection...");
        Thread.Sleep(200);

        Log.Info("SyncCustomerRepository", $"Executing query for Customer #{id}...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        var customers = JsonFileStore.Read<Customer>(DataFiles.Customers);
        var customer = customers.FirstOrDefault(c => c.Id == id);

        Log.Info("SyncCustomerRepository", customer is null ? $"Customer #{id} not found" : $"Customer #{id} found ({customer.Name})");
        return customer;
    }
}