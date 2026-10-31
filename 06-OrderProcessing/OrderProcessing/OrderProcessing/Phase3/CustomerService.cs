using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3;

public class CustomerService
{
    private readonly CustomerRepository _customers = new ();

    public async Task<Customer> GetCustomerAsync(int id, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(id, ct);

        if (customer is null)
            throw new CustomerNotFoundException($"Customer #{id} is not found");
        
        return customer;
    }
}