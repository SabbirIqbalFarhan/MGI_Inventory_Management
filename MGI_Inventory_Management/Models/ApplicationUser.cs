using Microsoft.AspNetCore.Identity;

namespace MGI_Inventory_Management.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? NationalId { get; set; }
        public string? EmergencyContact { get; set; }
        public string? PresentAddress { get; set; }
        public string? PermanentAddress { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public DateTime? JoinDate { get; set; }
        public string? EmployeeId { get; set; }
        public decimal? Salary { get; set; }
    }
}