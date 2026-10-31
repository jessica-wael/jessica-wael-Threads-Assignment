using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3;


public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct = default);
    Task RefundAsync(string transactionId, CancellationToken ct = default);
}

public class PaymentGateway : IPaymentGateway
{
    private static readonly SemaphoreSlim PaymentsLock = new(1, 1);
    private readonly HashSet<int> _timeoutOrderIds;
    private readonly HashSet<int> _declineOrderIds;

    public PaymentGateway(IEnumerable<int>? timeoutOrderIds = null, IEnumerable<int>? declineOrderIds = null)
    {
        _timeoutOrderIds = new HashSet<int>(timeoutOrderIds ?? Array.Empty<int>());
        _declineOrderIds = new HashSet<int>(declineOrderIds ?? Array.Empty<int>());
    }
    
      public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct = default)
    {
        Log.Info("PaymentGateway", $"Order #{orderId} -> Authorize...");
        await Task.Delay(800, ct);

        Log.Info("PaymentGateway", $"Order #{orderId} -> Fraud check...");
        await Task.Delay(500, ct);

        if (_declineOrderIds.Contains(orderId))
        {
            await AppendAsync(new PaymentTransaction
            {
                OrderId = orderId, Amount = amount, Status = PaymentStatus.Declined, CreatedAt = DateTime.Now
            }, ct);

            return new PaymentResult { Success = false, Status = PaymentStatus.Declined, Message = "Card declined by issuer" };
        }

        Log.Info("PaymentGateway", $"Order #{orderId} -> Capture...");
        await Task.Delay(_timeoutOrderIds.Contains(orderId) ? 5000 : 700, ct);

        string transactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();

        await AppendAsync(new PaymentTransaction
        {
            OrderId = orderId, Amount = amount, Status = PaymentStatus.Captured,
            TransactionId = transactionId, CreatedAt = DateTime.Now
        }, ct);

        return new PaymentResult { Success = true, TransactionId = transactionId, Status = PaymentStatus.Captured, Message = "OK" };
    }

    public async Task RefundAsync(string transactionId, CancellationToken ct = default)
    {
        Log.Info("PaymentGateway", $"Refund {transactionId} -> sending...");
        await Task.Delay(300, ct);

        await PaymentsLock.WaitAsync(ct);
        try
        {
            var payments = await JsonFileStore.ReadAsync<PaymentTransaction>(DataFiles.Payments, ct);
            payments.First(p => p.TransactionId == transactionId).Status = PaymentStatus.Refunded;
            await JsonFileStore.WriteAsync(DataFiles.Payments, payments, ct);
        }
        finally
        {
            PaymentsLock.Release();
        }

        Log.Success("PaymentGateway", $"Refund {transactionId} -> Refunded");
    }

    private static async Task AppendAsync(PaymentTransaction payment, CancellationToken ct)
    {
        await PaymentsLock.WaitAsync(ct);
        try
        {
            var payments = await JsonFileStore.ReadAsync<PaymentTransaction>(DataFiles.Payments, ct);
            payments.Add(payment);
            await JsonFileStore.WriteAsync(DataFiles.Payments, payments, ct);
        }
        finally
        {
            PaymentsLock.Release();
        }
    }
}