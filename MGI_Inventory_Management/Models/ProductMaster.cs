using System.ComponentModel.DataAnnotations;

namespace MGI_Inventory_Management.Models
{
    public class ProductMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        // ── TRACKING ──────────────────────────────
        public string? AddedBy { get; set; }
        public DateTime? AddedAt { get; set; }

        // ── IMAGE ─────────────────────────────────
        public string? ImagePath { get; set; }
    }
}