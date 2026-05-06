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
        // REGISTER (Admin only — reached via Add Roles button)
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            ViewBag.Roles = new List<string> { "Admin", "Manager", "Seller", "Supplier" };
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.Roles = new List<string> { "Admin", "Manager", "Seller", "Supplier" };

            if (!ModelState.IsValid)
                return View(model);

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(model.Role))
                await _roleManager.CreateAsync(new IdentityRole(model.Role));

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                TempData["Success"] = $"User '{model.FullName}' registered as {model.Role}!";
                return RedirectToAction("ViewRoles");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ─────────────────────────────────────────
        // VIEW ROLES (Admin only)
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin")]
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
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            return View(userRoleList);
        }

        // ─────────────────────────────────────────
        // DELETE USER (Admin only)
        // ─────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["Success"] = "User deleted successfully!";
            }
            return RedirectToAction("ViewRoles");
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

        // ─────────────────────────────────────────
        // ACCESS DENIED
        // ─────────────────────────────────────────
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}