using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MGI_Inventory_Management.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }
        public string? NationalId { get; set; }
        public string? EmergencyContact { get; set; }
        public string? PresentAddress { get; set; }
        public string? PermanentAddress { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }

        [DataType(DataType.Date)]
        public DateTime? JoinDate { get; set; }

        public string? EmployeeId { get; set; }
        public decimal? Salary { get; set; }
        public bool IsSuperAdmin { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}