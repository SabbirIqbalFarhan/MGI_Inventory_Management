using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MGI_Inventory_Management.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public string OrderedBy { get; set; } = string.Empty;
        // Add these fields to your existing Order class:
        public string? ShopName { get; set; }
        public string? ShopAddress { get; set; }
        public string? ShopContact { get; set; }
        // Also add SellerUserId to track which seller placed the order:
        public string? SellerUserId { get; set; }
        public string Status { get; set; } = "Pending";

        public List<OrderItem> Items { get; set; } = [];

        public decimal TotalAmount => Items?.Sum(i => i.Quantity * i.SellingPrice) ?? 0;
    }
}