namespace OrderProcessing.Shared.Models;

public class OrderItem
{//ProductId:int, Quantity:int, UnitPrice:decimal
    public int ProductId  { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}