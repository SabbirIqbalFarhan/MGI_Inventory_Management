using System.ComponentModel.DataAnnotations;

namespace MGI_Inventory_Management.Models
{
    public class CategoryLog
    {
        [Key]
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; } = DateTime.Now;
    }
}