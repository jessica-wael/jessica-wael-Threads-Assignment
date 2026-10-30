namespace OrderProcessing.Shared.Models;

public class EmailMessage
{//Id:Guid, OrderId, To, Subject, Body, SentAt, Attempts:int, LastError:string?
    
    public Guid Id  { get; set; }
    public int OrderId { get; set; }
    public string? To { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public DateTime SentAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    
    
}