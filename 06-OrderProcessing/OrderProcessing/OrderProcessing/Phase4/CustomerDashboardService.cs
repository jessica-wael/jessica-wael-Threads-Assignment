using OrderProcessing.Phase3;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase4;

public class CustomerDashboardService
{
     private readonly CustomerService _customers;
    private readonly HashSet<int> _failPaymentsForCustomerIds;

    public CustomerDashboardService(CustomerService customers, IEnumerable<int>? failPaymentsForCustomerIds = null)
    {
        _customers = customers;
        _failPaymentsForCustomerIds = new HashSet<int>(failPaymentsForCustomerIds ?? Array.Empty<int>());
    }

    public async Task<CustomerDashboard> LoadAsync(int customerId, CancellationToken ct = default)
    {
        var profile = _customers.GetCustomerAsync(customerId, ct);
        var orders = LoadOrdersAsync(customerId, ct);
        var payments = LoadPaymentsAsync(customerId, ct);
        var emails = LoadEmailsAsync(customerId, ct);

        var all = Task.WhenAll(profile, orders, payments, emails);
        try
        {
            await all; // await واحد بس
        }
        catch
        {
            if (all.Exception is null) throw;
            throw all.Exception;
        }

        return new CustomerDashboard
        {
            Customer = await profile,
            Orders = await orders,
            Payments = await payments,
            Emails = await emails
        };
    }

    private static async Task SimulateQueryAsync(string name, CancellationToken ct)
    {
        Log.Info("Dashboard", $"Opening connection ({name})...");
        await Task.Delay(200, ct);
        Log.Info("Dashboard", $"Executing query ({name})...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);
    }

    private static async Task<HashSet<int>> OrderIdsOfAsync(int customerId, CancellationToken ct)
    {
        await SimulateQueryAsync("order ids", ct);
        var all = await JsonFileStore.ReadAsync<Order>(DataFiles.Orders, ct);
        return all.Where(o => o.CustomerId == customerId).Select(o => o.Id).ToHashSet();
    }

    private async Task<List<Order>> LoadOrdersAsync(int customerId, CancellationToken ct)
    {
        await SimulateQueryAsync("orders", ct);
        var all = await JsonFileStore.ReadAsync<Order>(DataFiles.Orders, ct);
        return all.Where(o => o.CustomerId == customerId).ToList();
    }

    private async Task<List<PaymentTransaction>> LoadPaymentsAsync(int customerId, CancellationToken ct)
    {
        await SimulateQueryAsync("payments", ct);

        if (_failPaymentsForCustomerIds.Contains(customerId))
            throw new TimeoutException("Payments store timeout");

        var orderIds = await OrderIdsOfAsync(customerId, ct);
        var all = await JsonFileStore.ReadAsync<PaymentTransaction>(DataFiles.Payments, ct);
        return all.Where(p => orderIds.Contains(p.OrderId)).ToList();
    }

    private async Task<List<EmailMessage>> LoadEmailsAsync(int customerId, CancellationToken ct)
    {
        await SimulateQueryAsync("emails", ct);
        var orderIds = await OrderIdsOfAsync(customerId, ct);
        var all = await JsonFileStore.ReadAsync<EmailMessage>(DataFiles.Emails, ct);
        return all.Where(e => orderIds.Contains(e.OrderId)).ToList();
    }
}