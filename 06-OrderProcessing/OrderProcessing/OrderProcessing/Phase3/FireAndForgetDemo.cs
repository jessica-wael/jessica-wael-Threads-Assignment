using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
namespace OrderProcessing.Phase3;

public class FireAndForgetDemo
{
    private readonly OrderRepository _orderRepository = new();
    private readonly IEmailService _emailService;

    public FireAndForgetDemo(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task RunAsync()
    {
        await DataSeeder.ResetAsync(CancellationToken.None);
        var customer = await new CustomerRepository().GetByIdAsync(1, CancellationToken.None);
        var order = new Order { Id = 1, CustomerId = 1, Status = OrderStatus.Completed };

        Console.WriteLine("===== BEFORE =====");
        await ProcessOrderAsync(order, customer!);
        await Task.Delay(2000);   

        Console.WriteLine("===== AFTER ====");
        await ProcessOrderFixedAsync(order, customer!);
    }

  
#pragma warning disable CS4014
    public async Task ProcessOrderAsync(Order order, Customer customer)
    {
        await _orderRepository.SaveAsync(order);
        _emailService.SendConfirmationAsync(order, customer);   // not awaited
        Console.WriteLine("Order processing finished");
    }
#pragma warning restore CS4014


    public async Task ProcessOrderFixedAsync(Order order, Customer customer)
    {
        await _orderRepository.SaveAsync(order);
        await _emailService.SendConfirmationAsync(order, customer);
        Console.WriteLine("Order processing finished");
    }
}