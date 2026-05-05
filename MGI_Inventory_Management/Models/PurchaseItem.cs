using System.ComponentModel.DataAnnotations;

namespace MGI_Inventory_Management.Models
{
    public class PurchaseItem
    {
        [Key]
        public int Id { get; set; }

        public int PurchaseId { get; set; }

        public Purchase Purchase { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public Category Category { get; set; }

        [Required]
        public string ProductName { get; set; }

        [Required]
        public int Quantity { get; set; }

        public decimal PurchasePrice { get; set; }
        
    }
}
