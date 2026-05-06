using System.ComponentModel.DataAnnotations;

namespace MGI_Inventory_Management.Models
{
    public class RegisterViewModel
    {
        // ── Basic Auth ──
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        // ── Personal Details ──
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

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

        // ── Job Details ──
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