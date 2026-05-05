namespace MGI_Inventory_Management.Models
{
    public class MonthlyOrderReport
    {
        public List<Order> Orders { get; set; } = new List<Order>();
        public string Month { get; set; } = string.Empty;
    }

    public class MonthlyPurchaseReport
    {
        public List<Purchase> Purchases { get; set; } = new List<Purchase>();
        public string Month { get; set; } = string.Empty;
    }
}