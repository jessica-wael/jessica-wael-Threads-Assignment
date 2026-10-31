namespace OrderProcessing.Shared.Models;

public class PaymentTransaction
{//OrderId, Amount, Status:PaymentStatus, TransactionId:string?, CreatedAt
    
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus  Status { get; set; }
    public string? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}