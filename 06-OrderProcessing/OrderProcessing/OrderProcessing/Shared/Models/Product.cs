namespace OrderProcessing.Shared.Models;

public class Product
{//Id:int, Name, Price:decimal, StockQuantity:int, WeightKg:decimal
    
    public int Id { get; set; }
    public  string? Name { get; set; }
    public decimal  Price { get; set; }
    public int StockQuantity { get; set; }
    public decimal WeightKg { get; set; }
    
}