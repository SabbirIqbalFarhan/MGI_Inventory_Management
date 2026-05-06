using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MGI_Inventory_Management.Models
{
    public class Purchase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PurchasedBy { get; set; }
        // Add these fields to your existing Purchase class:
        public string? ShopName { get; set; }
        public string? ShopAddress { get; set; }
        public string? ShopContact { get; set; }
        public string? SupplierUserId { get; set; }
        public DateTime PurchaseDate { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public List<PurchaseItem> Items { get; set; } = [];
    }
}
