namespace OrderProcessing.Shared.Models;

public class PaymentResult
{//Success:bool, TransactionId:string?, Status:PaymentStatus, Message:string?
    
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public PaymentStatus Status  { get; set; }
    public string? Message { get; set; }
}