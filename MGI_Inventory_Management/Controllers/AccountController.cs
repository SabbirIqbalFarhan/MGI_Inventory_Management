using MGI_Inventory_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MGI_Inventory_Management.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ─────────────────────────────────────────
        // REGISTER (Admin + Manager only)
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Register()
        {
            ViewBag.Roles = new List<string> { "Admin", "Manager", "Seller", "Supplier" };
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.Roles = new List<string> { "Admin", "Manager", "Seller", "Supplier" };

            if (!ModelState.IsValid)
                return View(model);

            if (!await _roleManager.RoleExistsAsync(model.Role))
                await _roleManager.CreateAsync(new IdentityRole(model.Role));

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                FatherName = model.FatherName,
                MotherName = model.MotherName,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                NationalId = model.NationalId,
                PhoneNumber = model.PhoneNumber,
                EmergencyContact = model.EmergencyContact,
                PresentAddress = model.PresentAddress,
                PermanentAddress = model.PermanentAddress,
                Department = model.Department,
                Designation = model.Designation,
                JoinDate = model.JoinDate,
                EmployeeId = model.EmployeeId,
                Salary = model.Salary
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                TempData["Success"] = $"Employee '{model.FullName}' registered as {model.Role}!";
                return RedirectToAction("ViewRoles");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ─────────────────────────────────────────
        // VIEW ROLES (Admin + Manager only)
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ViewRoles()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoleList = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoleList.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    FatherName = user.FatherName,
                    MotherName = user.MotherName,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    NationalId = user.NationalId,
                    PhoneNumber = user.PhoneNumber,
                    EmergencyContact = user.EmergencyContact,
                    PresentAddress = user.PresentAddress,
                    PermanentAddress = user.PermanentAddress,
                    Department = user.Department,
                    Designation = user.Designation,
                    JoinDate = user.JoinDate,
                    EmployeeId = user.EmployeeId,
                    Salary = user.Salary
                });
            }

            return View(userRoleList);
        }

        // ─────────────────────────────────────────
        // EDIT USER GET (Admin + Manager only)
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> EditUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Prevent editing SuperAdmin
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
            {
                TempData["Error"] = "SuperAdmin cannot be edited.";
                return RedirectToAction("ViewRoles");
            }

            var model = new EditUserViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "",
                FatherName = user.FatherName,
                MotherName = user.MotherName,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                NationalId = user.NationalId,
                PhoneNumber = user.PhoneNumber,
                EmergencyContact = user.EmergencyContact,
                PresentAddress = user.PresentAddress,
                PermanentAddress = user.PermanentAddress,
                Department = user.Department,
                Designation = user.Designation,
                JoinDate = user.JoinDate,
                EmployeeId = user.EmployeeId,
                Salary = user.Salary
            };

            ViewBag.Roles = new List<string> { "Admin", "Manager", "Seller", "Supplier" };
            return View(model);
        }

        // ─────────────────────────────────────────
        // EDIT USER POST (Admin + Manager only)
        // ─────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            ViewBag.Roles = new List<string> { "Admin", "Manager", "Seller", "Supplier" };

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // Prevent editing SuperAdmin
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("SuperAdmin"))
            {
                TempData["Error"] = "SuperAdmin cannot be edited.";
                return RedirectToAction("ViewRoles");
            }

            // Update fields
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.FatherName = model.FatherName;
            user.MotherName = model.MotherName;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.NationalId = model.NationalId;
            user.PhoneNumber = model.PhoneNumber;
            user.EmergencyContact = model.EmergencyContact;
            user.PresentAddress = model.PresentAddress;
            user.PermanentAddress = model.PermanentAddress;
            user.Department = model.Department;
            user.Designation = model.Designation;
            user.JoinDate = model.JoinDate;
            user.EmployeeId = model.EmployeeId;
            user.Salary = model.Salary;

            await _userManager.UpdateAsync(user);

            // Update role
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!await _roleManager.RoleExistsAsync(model.Role))
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            await _userManager.AddToRoleAsync(user, model.Role);

            TempData["Success"] = "Employee updated successfully!";
            return RedirectToAction("ViewRoles");
        }

        // ─────────────────────────────────────────
        // DELETE USER (Admin only — cannot delete SuperAdmin)
        // ─────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("SuperAdmin") || roles.Contains("Admin"))
                {
                    TempData["Error"] = "Admin/SuperAdmin accounts cannot be deleted!";
                    return RedirectToAction("ViewRoles");
                }
                await _userManager.DeleteAsync(user);
                TempData["Success"] = "Employee deleted successfully!";
            }
            return RedirectToAction("ViewRoles");
        }

        // ─────────────────────────────────────────
        // CHANGE PASSWORD (Any logged-in user — own password only)
        // ─────────────────────────────────────────
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(
                user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("ChangePassword");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ─────────────────────────────────────────
        // LOGIN
        // ─────────────────────────────────────────
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Admin");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Admin");

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // ─────────────────────────────────────────
        // LOGOUT
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}