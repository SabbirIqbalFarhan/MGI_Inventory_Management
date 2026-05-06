using Microsoft.AspNetCore.Identity;

namespace MGI_Inventory_Management.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}