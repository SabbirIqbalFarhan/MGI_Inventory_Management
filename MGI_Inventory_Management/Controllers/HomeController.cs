using System.Diagnostics;
using System.Linq;
using MGI_Inventory_Management.Data;
using MGI_Inventory_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MGI_Inventory_Management.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // ✅ SHOW CONTACT PAGE + LOAD DATA WITH PAGINATION
        public IActionResult ContactUs(int page = 1)
        {
            int pageSize = 10;

            var totalMessages = _context.ContactMessages.Count();
            var totalPages = (int)Math.Ceiling(totalMessages / (double)pageSize);

            var messages = _context.ContactMessages
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(messages);
        }

        // ✅ SAVE MESSAGE TO DATABASE
        [HttpPost]
        public IActionResult SendMessage(ContactMessage model)
        {
            if (ModelState.IsValid)
            {
                _context.ContactMessages.Add(model);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Message sent successfully!";
            }

            return RedirectToAction("ContactUs");
        }

        // ✅ DELETE MESSAGE
        [HttpPost]
        public IActionResult DeleteMessage(int id)
        {
            var message = _context.ContactMessages.Find(id);
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Message deleted successfully!";
            }

            return RedirectToAction("ContactUs");
        }

        public IActionResult VissionMissionGoal()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}