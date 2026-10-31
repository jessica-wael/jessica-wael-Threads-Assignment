using System.Diagnostics;
using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;


public class OrderProcessor
{

    private static readonly SemaphoreSlim EmailLock = new(1, 1);

    private readonly CustomerService _customers;
    private readonly InventoryService _inventory;
    private readonly PricingService _pricing;
    private readonly IPaymentGateway _payments;
    private readonly OrderRepository _orders;
    private readonly IEmailService _email;
    private readonly ProductRepository _products = new();
    
    public OrderProcessor(CustomerService customers, InventoryService inventory, PricingService pricing, IPaymentGateway payments, OrderRepository orders, IEmailService email)
    {
        _customers = customers;
        _inventory = inventory;
        _pricing = pricing;
        _payments = payments;
        _orders = orders;
        _email = email;
    }
    
    public async Task<bool>  ProcessOrderAsync(Order order, CancellationToken ct = default)
    {
        bool reserved = false;
        string? transactionId = null;

        try
        {
            // Step 1: ا
            var sw = Stopwatch.StartNew();
            Customer customer;
            try
            {
                customer = await _customers.GetCustomerAsync(order.CustomerId, ct);
            }
            catch (CustomerNotFoundException ex)
            {
                LogStep(order, 1, "Customer", sw, "not found");
                return await FinishAsync(order, OrderStatus.Rejected, ex.Message, ct);
            }

            if (!customer.IsActive)
            {
                LogStep(order, 1, "Customer", sw, "inactive");
                return await FinishAsync(order, OrderStatus.Rejected, "Customer is inactive", ct);
            }
            LogStep(order, 1, "Customer", sw, "OK");

            // Step 2: 
            sw.Restart();
            reserved = await _inventory.ReserveStockAsync(order, ct);
            if (!reserved)
            {
                LogStep(order, 2, "Reserve", sw, "failed");
                string name = await FindShortageNameAsync(order, ct);
                return await FinishAsync(order, OrderStatus.Rejected, $"Insufficient stock for '{name}'", ct);
            }
            LogStep(order, 2, "Reserve", sw, "OK");

            // Step 3: 
            sw.Restart();
            try
            {
                await _pricing.CalculateTotalAsync(order, ct);
            }
            catch (InvalidCouponException ex)
            {
                LogStep(order, 3, "Total", sw, "invalid coupon");
                await _inventory.ReleaseStockAsync(order, ct);
                return await FinishAsync(order, OrderStatus.Rejected, ex.Message, ct);
            }
            LogStep(order, 3, "Total", sw, $"{order.TotalAmount:F2}");

            // Step 4: 
            sw.Restart();
            PaymentResult payment;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                payment = await _payments.ProcessPaymentAsync(order.Id, order.TotalAmount, timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                Log.Error("OrderProcessor", $"Order #{order.Id} -> Step 4/7 Payment ... TIMEOUT after {sw.ElapsedMilliseconds} ms, released");
                await _inventory.ReleaseStockAsync(order, ct);
                return await FinishAsync(order, OrderStatus.PaymentFailed, "Payment gateway timeout (3s)", ct);
            }

            if (!payment.Success)
            {
                LogStep(order, 4, "Payment", sw, "declined");
                await _inventory.ReleaseStockAsync(order, ct);
                return await FinishAsync(order, OrderStatus.PaymentFailed, payment.Message, ct);
            }

            transactionId = payment.TransactionId;
            LogStep(order, 4, "Payment", sw, $"OK ({transactionId})");

            // Step 5:
            sw.Restart();
            order.TransactionId = transactionId;
            order.Status = OrderStatus.Completed;
            try
            {
                await _orders.SaveAsync(order, ct);
            }
            catch (DatabaseWriteException)
            {
                await _payments.RefundAsync(transactionId!, ct);
                await _inventory.ReleaseStockAsync(order, ct);
                order.Status = OrderStatus.Failed;
                order.FailureReason = $"Database save failed after payment; payment {transactionId} refunded";
                await _orders.SaveAsync(order, ct);
                LogStep(order, 5, "Save", sw, "failed, compensated");
                return false;
            }
            LogStep(order, 5, "Save", sw, "Completed");

            // Step 6: 
            sw.Restart();
            await SendConfirmationWithRetryAsync(order, customer, ct);
            LogStep(order, 6, "Email", sw, "done");

            // Step 7: 
            return order.Status == OrderStatus.Completed;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            if (order.Status == OrderStatus.Completed)
                return true;

            if (reserved)
                await _inventory.ReleaseStockAsync(order, CancellationToken.None);

            if (transactionId is not null)
                await _payments.RefundAsync(transactionId, CancellationToken.None);

            order.Status = OrderStatus.Pending;
            order.FailureReason = "Cancelled";
            await _orders.SaveAsync(order, CancellationToken.None);
            Log.Error("OrderProcessor", $"Order #{order.Id} -> Cancelled");
            return false;
        }
    }


    private async Task SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken ct)
    {
        int[] retryDelays = { 500, 1000, 2000 };
        string? lastError = null;
        int attempts = 0;

        for (int retry = 0; retry <= retryDelays.Length; retry++)
        {
            if (retry > 0)
                await Task.Delay(retryDelays[retry - 1], ct);

            attempts++;
            try
            {
                await _email.SendConfirmationAsync(order, customer, ct);
                await AppendEmailAsync(DataFiles.Emails, BuildEmail(order, customer, attempts, null), ct);
                return;
            }
            catch (SmtpUnavailableException ex)
            {
                lastError = ex.Message;
                Log.Error("OrderProcessor", $"Order #{order.Id} -> email attempt {attempts} failed: {ex.Message}");
            }
        }

       
        await AppendEmailAsync(DataFiles.EmailRetry, BuildEmail(order, customer, attempts, lastError), ct);
        Log.Info("OrderProcessor", $"Email for Order #{order.Id} queued for retry later");
    }

    private async Task<bool> FinishAsync(Order order, OrderStatus status, string reason, CancellationToken ct)
    {
        order.Status = status;
        order.FailureReason = reason;
        await _orders.SaveAsync(order, CancellationToken.None);
        Log.Error("OrderProcessor", $"Order #{order.Id} -> {status}: {reason}");
        return false;
    }

    private async Task<string> FindShortageNameAsync(Order order, CancellationToken ct)
    {
        foreach (var item in order.Items!)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is not null && product.StockQuantity < item.Quantity)
                return product.Name;
        }
        return "unknown";
    }

    private static void LogStep(Order order, int step, string name, Stopwatch sw, string result)
        => Log.Info("OrderProcessor", $"Order #{order.Id} -> Step {step}/7 {name} ... {result} in {sw.ElapsedMilliseconds} ms");

    private static EmailMessage BuildEmail(Order order, Customer customer, int attempts, string? lastError) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        To = customer.Email,
        Subject = $"Order #{order.Id} confirmed",
        Body = $"Thank you {customer.Name}, your order #{order.Id} is confirmed.",
        SentAt = DateTime.Now,
        Attempts = attempts,
        LastError = lastError
    };

    private static async Task AppendEmailAsync(string file, EmailMessage message, CancellationToken ct)
    {
        await EmailLock.WaitAsync(ct);
        try
        {
            var list = await JsonFileStore.ReadAsync<EmailMessage>(file, ct);
            list.Add(message);
            await JsonFileStore.WriteAsync(file, list, ct);
        }
        finally
        {
            EmailLock.Release();
        }
    }
    
}