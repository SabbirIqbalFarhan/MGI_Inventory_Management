using MGI_Inventory_Management.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MGI_Inventory_Management.Models
{
    public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    // Navigation (optional but better initialized)
    public List<AddProduct> Products { get; set; } = new List<AddProduct>();
}

}
