// Models/OrderActivityLog.cs
public class OrderActivityLog
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Action { get; set; } = "";       // Created, Approved, Edited, Deleted, Delivered
    public string PerformedBy { get; set; } = "";
    public DateTime PerformedAt { get; set; }
}

// Models/PurchaseActivityLog.cs
public class PurchaseActivityLog
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public string Action { get; set; } = "";
    public string PerformedBy { get; set; } = "";
    public DateTime PerformedAt { get; set; }
}