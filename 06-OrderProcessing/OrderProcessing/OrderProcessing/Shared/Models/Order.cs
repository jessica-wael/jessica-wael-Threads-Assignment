namespace OrderProcessing.Shared.Models;

public class Order
{ /* Id, CustomerId, Items:List<OrderItem>, CouponCode:string?,
    SubTotal, Discount, ShippingCost, TotalAmount, Status:OrderStatus,
    CreatedAt:DateTime, FailureReason:string?, TransactionId:string?*/
    
    public List<OrderItem>? Items { get; set; }
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? CouponCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public OrderStatus  Status { get; set; }
    public DateTime  CreatedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? TransactionId { get; set; }

    
}