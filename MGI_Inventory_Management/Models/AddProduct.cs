using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MGI_Inventory_Management.Models
{
    public class AddProduct
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal PurchaseRate { get; set; }

        [Required]
        public decimal SellingPrice { get; set; }

        public DateTime PublishedDate { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }

        [BindNever]  // ✅ FIX 1: Prevents model binding from requiring this nav property
        public Category Category { get; set; }
        
    }
}