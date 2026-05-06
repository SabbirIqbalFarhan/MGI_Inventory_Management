using System.ComponentModel.DataAnnotations;

namespace MGI_Inventory_Management.Models
{
    public class EditUserViewModel
    {
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Father's Name")]
        public string? FatherName { get; set; }

        [Display(Name = "Mother's Name")]
        public string? MotherName { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Display(Name = "National ID (NID)")]
        public string? NationalId { get; set; }

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Emergency Contact")]
        public string? EmergencyContact { get; set; }

        [Display(Name = "Present Address")]
        public string? PresentAddress { get; set; }

        [Display(Name = "Permanent Address")]
        public string? PermanentAddress { get; set; }

        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Designation")]
        public string? Designation { get; set; }

        [Display(Name = "Join Date")]
        [DataType(DataType.Date)]
        public DateTime? JoinDate { get; set; }

        [Display(Name = "Employee ID")]
        public string? EmployeeId { get; set; }

        [Display(Name = "Salary (BDT)")]
        public decimal? Salary { get; set; }
    }
}