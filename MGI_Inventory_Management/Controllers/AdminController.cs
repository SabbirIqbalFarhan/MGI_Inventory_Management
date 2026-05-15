using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MGI_Inventory_Management.Data;
using MGI_Inventory_Management.Models;
using System;
using System.Linq;

namespace MGI_Inventory_Management.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ─────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────
        public IActionResult Index() => View();

        // ─────────────────────────────────────────
        // MANAGE CATEGORIES
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult ManageCategories(int page = 1, int logPage = 1)
        {
            int pageSize = 10;

            var all = _context.Categories.ToList();
            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Categories = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var allLogs = _context.CategoryLogs
                .OrderByDescending(l => l.PerformedAt).ToList();

            int totalLogPages = (int)Math.Ceiling(allLogs.Count / (double)pageSize);
            if (totalLogPages == 0) totalLogPages = 1;

            ViewBag.CategoryLogs = allLogs
                .Skip((logPage - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.LogCurrentPage = logPage;
            ViewBag.LogTotalPages = totalLogPages;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteCategoryLog(int id)
        {
            var log = _context.CategoryLogs.Find(id);
            if (log != null)
            {
                _context.CategoryLogs.Remove(log);
                _context.SaveChanges();
                TempData["CategorySuccess"] = "Log deleted!";
            }
            return RedirectToAction("ManageCategories");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public IActionResult AddCategory(string categoryName)
        {
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                bool exists = _context.Categories
                    .Any(c => c.Name.ToLower() == categoryName.ToLower());

                if (!exists)
                {
                    string addedBy = GetUserLabel();
                    _context.Categories.Add(new Category
                    {
                        Name = categoryName,
                        AddedBy = addedBy,
                        AddedAt = DateTime.Now
                    });
                    _context.CategoryLogs.Add(new CategoryLog
                    {
                        Action = "Added",
                        CategoryName = categoryName,
                        PerformedBy = addedBy,
                        PerformedAt = DateTime.Now
                    });
                    _context.SaveChanges();
                    TempData["CategorySuccess"] = "Category added successfully!";
                }
                else
                {
                    TempData["CategoryError"] = "This category already exists!";
                }
            }
            return RedirectToAction("ManageCategories");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                bool hasProducts = _context.Products.Any(p => p.CategoryId == id);
                bool hasMaster = _context.ProductMasters.Any(p => p.CategoryId == id);

                if (hasProducts || hasMaster)
                {
                    TempData["CategoryError"] =
                        "Cannot delete — products exist under this category!";
                    return RedirectToAction("ManageCategories");
                }

                string deletedBy = GetUserLabel();
                _context.CategoryLogs.Add(new CategoryLog
                {
                    Action = "Deleted",
                    CategoryName = category.Name,
                    PerformedBy = deletedBy,
                    PerformedAt = DateTime.Now
                });
                _context.Categories.Remove(category);
                _context.SaveChanges();
                TempData["CategorySuccess"] = "Category deleted successfully!";
            }
            return RedirectToAction("ManageCategories");
        }

        // ─────────────────────────────────────────
        // MANAGE MASTER PRODUCTS
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult ManageProducts(int page = 1, int logPage = 1)
        {
            int pageSize = 10;

            var allMasters = _context.ProductMasters
                .Include(p => p.Category).ToList();

            int totalPages = (int)Math.Ceiling(allMasters.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.ProductMasters = allMasters
                .Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var allLogs = _context.ProductMasterLogs
                .OrderByDescending(l => l.PerformedAt).ToList();

            int totalLogPages = (int)Math.Ceiling(allLogs.Count / (double)pageSize);
            if (totalLogPages == 0) totalLogPages = 1;

            ViewBag.ProductMasterLogs = allLogs
                .Skip((logPage - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.LogCurrentPage = logPage;
            ViewBag.LogTotalPages = totalLogPages;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddProductMaster(
            string productName, int categoryId,
            string description, IFormFile? imageFile)
        {
            if (!string.IsNullOrWhiteSpace(productName) && categoryId > 0)
            {
                bool exists = _context.ProductMasters
                    .Any(p => p.ProductName.ToLower() == productName.ToLower()
                           && p.CategoryId == categoryId);

                if (!exists)
                {
                    string addedBy = GetUserLabel();
                    var category = _context.Categories.Find(categoryId);
                    string categoryName = category?.Name ?? "";

                    string? imagePath = null;
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot", "uploads", "products");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString()
                            + Path.GetExtension(imageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await imageFile.CopyToAsync(stream);

                        imagePath = "/uploads/products/" + uniqueFileName;
                    }

                    _context.ProductMasters.Add(new ProductMaster
                    {
                        ProductName = productName,
                        CategoryId = categoryId,
                        Description = description ?? string.Empty,
                        AddedBy = addedBy,
                        AddedAt = DateTime.Now,
                        ImagePath = imagePath
                    });
                    _context.ProductMasterLogs.Add(new ProductMasterLog
                    {
                        Action = "Added",
                        ProductName = productName,
                        CategoryName = categoryName,
                        PerformedBy = addedBy,
                        PerformedAt = DateTime.Now,
                        ImagePath = imagePath
                    });
                    _context.SaveChanges();
                    TempData["MasterSuccess"] = "Product added to master list!";
                }
                else
                {
                    TempData["MasterError"] = "This product already exists in this category!";
                }
            }
            return RedirectToAction("ManageProducts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult DeleteProductMaster(int id)
        {
            var item = _context.ProductMasters
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id);

            if (item != null)
            {
                var netQuantity = _context.Products
                    .Where(p => p.Name == item.ProductName && p.CategoryId == item.CategoryId
                             && !p.Description.StartsWith("Edited stock (old)")
                             && !p.Description.StartsWith("Stock removed (original)"))
                    .Sum(p => (int?)p.Quantity) ?? 0;

                if (netQuantity != 0)
                {
                    TempData["MasterError"] =
                        $"Cannot delete — \"{item.ProductName}\" still has {netQuantity} units in stock.";
                    return RedirectToAction("ManageProducts");
                }

                string deletedBy = GetUserLabel();
                _context.ProductMasterLogs.Add(new ProductMasterLog
                {
                    Action = "Deleted",
                    ProductName = item.ProductName,
                    CategoryName = item.Category?.Name ?? "",
                    PerformedBy = deletedBy,
                    PerformedAt = DateTime.Now,
                    ImagePath = item.ImagePath
                });
                _context.ProductMasters.Remove(item);
                _context.SaveChanges();
                TempData["MasterSuccess"] = "Product removed from master list!";
            }
            return RedirectToAction("ManageProducts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteProductMasterLog(int id)
        {
            var log = _context.ProductMasterLogs.Find(id);
            if (log != null)
            {
                _context.ProductMasterLogs.Remove(log);
                _context.SaveChanges();
                TempData["MasterSuccess"] = "Log deleted!";
            }
            return RedirectToAction("ManageProducts");
        }

        // ─────────────────────────────────────────
        // ADD PRODUCT
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult AddProduct()
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.ProductMasters = _context.ProductMasters
                .Include(p => p.Category).ToList();
            return View();
        }

        [HttpPost]
        [ActionName("AddProduct")]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public IActionResult AddProductPost(AddProduct product)
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.ProductMasters = _context.ProductMasters
                .Include(p => p.Category).ToList();

            ModelState.Remove("Category");

            if (!ModelState.IsValid)
                return View("AddProduct", product);

            string performedBy = GetUserLabel();
            product.PublishedDate = DateTime.Now;
            product.Description = $"Stock added by {performedBy}";

            _context.Products.Add(product);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Product added successfully!";
            return RedirectToAction("ViewProduct");
        }

        // ─────────────────────────────────────────
        // AJAX ENDPOINTS
        // ─────────────────────────────────────────
        [Authorize]
        public IActionResult GetProductsByCategory(int categoryId)
        {
            var products = _context.ProductMasters
                .Where(p => p.CategoryId == categoryId)
                .Select(p => new { p.Id, p.ProductName, p.Description })
                .ToList();
            return Json(products);
        }

        [Authorize]
        public IActionResult GetSellingPrice(string productName, int categoryId)
        {
            var product = _context.Products
                .Where(p => p.Name == productName && p.CategoryId == categoryId && p.SellingPrice > 0)
                .OrderByDescending(p => p.PublishedDate)
                .FirstOrDefault();

            if (product == null)
                product = _context.Products
                    .Where(p => p.Name == productName && p.CategoryId == categoryId)
                    .OrderByDescending(p => p.PublishedDate)
                    .FirstOrDefault();

            return Json(new { price = product?.SellingPrice ?? 0 });
        }

        [Authorize]
        public IActionResult GetPurchaseRate(string productName, int categoryId)
        {
            var product = _context.Products
                .Where(p => p.Name == productName && p.CategoryId == categoryId)
                .OrderByDescending(p => p.PublishedDate)
                .FirstOrDefault();
            return Json(new { price = product?.PurchaseRate ?? 0 });
        }

        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> GetSellers()
        {
            var sellers = await _userManager.GetUsersInRoleAsync("Seller");
            var list = sellers.Select(s => new { s.Id, s.FullName, s.Email }).ToList();
            return Json(list);
        }

        [Authorize(Roles = "Admin,Manager,Supplier")]
        public async Task<IActionResult> GetSuppliers()
        {
            var suppliers = await _userManager.GetUsersInRoleAsync("Supplier");
            var list = suppliers.Select(s => new { s.Id, s.FullName, s.Email }).ToList();
            return Json(list);
        }

        // ─────────────────────────────────────────
        // VIEW PRODUCTS
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager,Seller,Supplier")]
        public IActionResult ViewProduct(int page1 = 1, int page2 = 1,
            int? filterCategory = null, string? searchName = null)
        {
            int pageSize = 10;

            var allEntriesList = _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.PublishedDate)
                .ToList();

            int totalPages1 = (int)Math.Ceiling(allEntriesList.Count / (double)pageSize);
            if (totalPages1 == 0) totalPages1 = 1;

            var pagedEntries = allEntriesList
                .Skip((page1 - 1) * pageSize).Take(pageSize).ToList();

            var masterImageMap = _context.ProductMasters
                .Where(m => m.ImagePath != null)
                .GroupBy(m => m.ProductName.ToLower())
                .ToDictionary(g => g.Key, g => g.First().ImagePath!);

            ViewBag.ProductMasterMap = masterImageMap;

            var groupedQuery = allEntriesList
                .GroupBy(p => new
                {
                    p.Name,
                    p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : ""
                })
                .Select(g =>
                {
                    var master = _context.ProductMasters
                        .FirstOrDefault(m =>
                            m.ProductName.ToLower() == g.Key.Name.ToLower()
                            && m.CategoryId == g.Key.CategoryId);

                    var allRecords = g.OrderByDescending(x => x.PublishedDate).ToList();

                    var lastDeletionRecord = allRecords
                        .FirstOrDefault(x => x.Description.StartsWith("Stock removed by"));

                    List<dynamic> activeRecords;
                    if (lastDeletionRecord != null)
                    {
                        activeRecords = allRecords
                            .Where(x => x.PublishedDate > lastDeletionRecord.PublishedDate
                                     && !x.Description.StartsWith("Stock removed by"))
                            .ToList<dynamic>();
                    }
                    else
                    {
                        activeRecords = allRecords
                            .Where(x => !x.Description.StartsWith("Stock removed by"))
                            .ToList<dynamic>();
                    }

                    var netQty = activeRecords.Sum(x => (int?)x.Quantity) ?? 0;

                    var latestActive = activeRecords
                        .OrderByDescending(x => x.PublishedDate)
                        .FirstOrDefault();

                    var latestWithPrice = activeRecords
                        .Where(x => x.SellingPrice > 0 || x.PurchaseRate > 0)
                        .OrderByDescending(x => x.PublishedDate)
                        .FirstOrDefault();

                    var mostRecentRecord = allRecords.FirstOrDefault();

                    bool isDeleted = mostRecentRecord != null
                        && mostRecentRecord.Description.StartsWith("Stock removed by");

                    return new
                    {
                        ProductName = g.Key.Name,
                        Category = g.Key.CategoryName,
                        CategoryId = g.Key.CategoryId,
                        Description = master != null ? master.Description : "-",
                        ImagePath = master?.ImagePath,
                        TotalQuantity = netQty,
                        LatestId = isDeleted ? 0 : (latestActive?.Id ?? 0),
                        SellingPrice = latestWithPrice?.SellingPrice ?? 0,
                        PurchaseRate = latestWithPrice?.PurchaseRate ?? 0
                    };
                })
                .AsEnumerable();

            groupedQuery = groupedQuery.Where(x => x.LatestId > 0);

            if (filterCategory.HasValue && filterCategory > 0)
                groupedQuery = groupedQuery.Where(x => x.CategoryId == filterCategory);

            if (!string.IsNullOrWhiteSpace(searchName))
                groupedQuery = groupedQuery.Where(x =>
                    x.ProductName.ToLower().Contains(searchName.ToLower()));

            var filteredList = groupedQuery.ToList();

            int totalPages2 = (int)Math.Ceiling(filteredList.Count / (double)pageSize);
            if (totalPages2 == 0) totalPages2 = 1;

            ViewBag.AllEntries = pagedEntries;
            ViewBag.GroupedProducts = filteredList
                .Skip((page2 - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage1 = page1;
            ViewBag.TotalPages1 = totalPages1;
            ViewBag.CurrentPage2 = page2;
            ViewBag.TotalPages2 = totalPages2;
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.FilterCategory = filterCategory ?? 0;
            ViewBag.SearchName = searchName ?? "";

            return View();
        }

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult EditProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            if (product.SellingPrice == 0)
            {
                var lastKnownPrice = _context.Products
                    .Where(p => p.Name == product.Name && p.CategoryId == product.CategoryId
                             && p.SellingPrice > 0)
                    .OrderByDescending(p => p.PublishedDate)
                    .Select(p => p.SellingPrice).FirstOrDefault();
                product.SellingPrice = lastKnownPrice;
            }

            if (product.PurchaseRate == 0)
            {
                var lastKnownRate = _context.Products
                    .Where(p => p.Name == product.Name && p.CategoryId == product.CategoryId
                             && p.PurchaseRate > 0)
                    .OrderByDescending(p => p.PublishedDate)
                    .Select(p => p.PurchaseRate).FirstOrDefault();
                product.PurchaseRate = lastKnownRate;
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public IActionResult EditProduct(AddProduct product)
        {
            ViewBag.Categories = _context.Categories.ToList();
            ModelState.Remove("Category");
            ModelState.Remove("Description");

            if (!ModelState.IsValid) return View(product);

            var existing = _context.Products.Find(product.Id);
            if (existing == null) return NotFound();

            string performedBy = GetUserLabel();

            var allRecords = _context.Products
                .Where(p => p.Name == existing.Name && p.CategoryId == existing.CategoryId)
                .OrderByDescending(p => p.PublishedDate).ToList();

            var lastDeletion = allRecords
                .FirstOrDefault(x => x.Description.StartsWith("Stock removed by"));

            int currentNetQty;
            if (lastDeletion != null)
            {
                currentNetQty = allRecords
                    .Where(x => x.PublishedDate > lastDeletion.PublishedDate
                             && !x.Description.StartsWith("Stock removed by"))
                    .Sum(x => (int?)x.Quantity) ?? 0;
            }
            else
            {
                currentNetQty = allRecords
                    .Where(x => !x.Description.StartsWith("Stock removed by"))
                    .Sum(x => (int?)x.Quantity) ?? 0;
            }

            int newNetQty = currentNetQty + product.Quantity;

            if (newNetQty < 0)
            {
                TempData["ErrorMessage"] =
                    $"Invalid quantity — current stock is {currentNetQty} units. " +
                    $"You cannot subtract more than {currentNetQty} units.";
                return RedirectToAction("ViewProduct");
            }

            _context.Products.Add(new AddProduct
            {
                Name = existing.Name,
                CategoryId = existing.CategoryId,
                Quantity = product.Quantity,
                PurchaseRate = product.PurchaseRate,
                SellingPrice = product.SellingPrice,
                Description = $"Stock edited by {performedBy}",
                PublishedDate = DateTime.Now
            });
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction("ViewProduct");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            string performedBy = GetUserLabel();
            string productName = product.Name;
            int categoryId = product.CategoryId;

            _context.Products.Add(new AddProduct
            {
                Name = productName,
                CategoryId = categoryId,
                Quantity = 0,
                PurchaseRate = 0,
                SellingPrice = 0,
                Description = $"Stock removed by {performedBy}",
                PublishedDate = DateTime.Now
            });
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction("ViewProduct");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProductEntry(int id)
        {
            var entry = _context.Products.Find(id);
            if (entry != null)
            {
                _context.Products.Remove(entry);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Entry deleted successfully!";
            }
            return RedirectToAction("ViewProduct");
        }

        // ─────────────────────────────────────────
        // MAKE ORDER
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Seller,Manager")]
        public IActionResult Order(int? categoryId = null, string? productName = null,
            decimal? unitPrice = null)
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.PrefilledCategoryId = categoryId;
            ViewBag.PrefilledProductName = productName;
            ViewBag.PrefilledUnitPrice = unitPrice;
            ViewBag.CurrentUserLabel = GetUserLabel();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Seller,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Order(string orderedBy, string? shopName,
            string? shopAddress, string? shopContact, string? sellerUserId,
            List<int> categoryIds, List<string> productNames,
            List<int> quantities, List<decimal> unitPrices)
        {
            if (string.IsNullOrWhiteSpace(orderedBy) || categoryIds == null || categoryIds.Count == 0)
            {
                TempData["OrderError"] = "Please fill all required fields.";
                ViewBag.Categories = _context.Categories.ToList();
                ViewBag.CurrentUserLabel = GetUserLabel();
                return View();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            string? assignedSellerUserId = User.IsInRole("Seller")
                ? currentUser?.Id : sellerUserId;

            var order = new Order
            {
                OrderedBy = orderedBy,
                OrderDate = DateTime.Now,
                Status = "Pending",
                ShopName = shopName,
                ShopAddress = shopAddress,
                ShopContact = shopContact,
                SellerUserId = assignedSellerUserId
            };

            for (int i = 0; i < categoryIds.Count; i++)
            {
                decimal price = (unitPrices != null && unitPrices.Count > i && unitPrices[i] > 0)
                    ? unitPrices[i]
                    : (_context.Products
                        .Where(p => p.Name == productNames[i]
                                 && p.CategoryId == categoryIds[i]
                                 && p.SellingPrice > 0)
                        .OrderByDescending(p => p.PublishedDate)
                        .Select(p => p.SellingPrice)
                        .FirstOrDefault());

                order.Items.Add(new OrderItem
                {
                    CategoryId = categoryIds[i],
                    ProductName = productNames[i],
                    Quantity = quantities[i],
                    SellingPrice = price
                });
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            _context.OrderActivityLogs.Add(new OrderActivityLog
            {
                OrderId = order.Id,
                Action = "Created",
                PerformedBy = GetUserLabel(),
                PerformedAt = DateTime.Now
            });
            _context.SaveChanges();

            TempData["OrderSuccess"] = "Order placed successfully!";
            return RedirectToAction("OrderRequest");
        }

        // ─────────────────────────────────────────
        // ORDER REQUEST
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> OrderRequest(int page = 1)
        {
            int pageSize = 10;
            var currentUser = await _userManager.GetUserAsync(User);
            var isSeller = User.IsInRole("Seller");

            var query = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Category)
                .AsQueryable();

            if (isSeller)
                query = query.Where(o => o.SellerUserId == currentUser!.Id);

            var all = query.OrderByDescending(o => o.OrderDate).ToList();

            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var orderIds = all.Select(o => o.Id).ToList();
            ViewBag.ActivityLogs = _context.OrderActivityLogs
                .Where(l => orderIds.Contains(l.OrderId))
                .OrderBy(l => l.PerformedAt)
                .ToList();

            ViewBag.Orders = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveOrder(int id)
        {
            var order = _context.Orders.Include(o => o.Items).FirstOrDefault(o => o.Id == id);

            if (order != null && order.Status == "Pending")
            {
                foreach (var item in order.Items)
                {
                    // Use same logic as ViewProduct — only count records after last deletion
                    var allRecords = _context.Products
                        .Where(p => p.Name == item.ProductName
                                 && p.CategoryId == item.CategoryId)
                        .OrderByDescending(p => p.PublishedDate)
                        .ToList();

                    var lastDeletion = allRecords
                        .FirstOrDefault(x => x.Description.StartsWith("Stock removed by"));

                    int netStock;
                    if (lastDeletion != null)
                    {
                        netStock = allRecords
                            .Where(x => x.PublishedDate > lastDeletion.PublishedDate
                                     && !x.Description.StartsWith("Stock removed by"))
                            .Sum(x => (int?)x.Quantity) ?? 0;
                    }
                    else
                    {
                        netStock = allRecords
                            .Where(x => !x.Description.StartsWith("Stock removed by"))
                            .Sum(x => (int?)x.Quantity) ?? 0;
                    }

                    // Reserved stock from other approved orders
                    var reservedStock = _context.Orders
                        .Include(o => o.Items)
                        .Where(o => o.Status == "Approved" && o.Id != id)
                        .SelectMany(o => o.Items)
                        .Where(i => i.ProductName == item.ProductName
                                 && i.CategoryId == item.CategoryId)
                        .Sum(i => (int?)i.Quantity) ?? 0;

                    int availableStock = netStock - reservedStock;

                    if (availableStock < item.Quantity)
                    {
                        TempData["OrderError"] =
                            $"Not enough stock for '{item.ProductName}'. " +
                            $"Available: {availableStock} units, Required: {item.Quantity} units.";
                        return RedirectToAction("OrderRequest");
                    }
                }

                order.Status = "Approved";
                _context.OrderActivityLogs.Add(new OrderActivityLog
                {
                    OrderId = order.Id,
                    Action = "Approved",
                    PerformedBy = GetUserLabel(),
                    PerformedAt = DateTime.Now
                });
                _context.SaveChanges();
                TempData["OrderSuccess"] = "Order approved!";
            }
            else
            {
                TempData["OrderError"] = "Only pending orders can be approved!";
            }

            return RedirectToAction("OrderRequest");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Seller")]
        [ValidateAntiForgeryToken]
        public IActionResult DeliverOrder(int id)
        {
            var order = _context.Orders.Include(o => o.Items).FirstOrDefault(o => o.Id == id);

            if (order != null && order.Status == "Approved")
            {
                foreach (var item in order.Items)
                {
                    var latestProduct = _context.Products
                        .Where(p => p.Name == item.ProductName && p.CategoryId == item.CategoryId)
                        .OrderByDescending(p => p.PublishedDate).FirstOrDefault();

                    _context.Products.Add(new AddProduct
                    {
                        Name = item.ProductName,
                        CategoryId = item.CategoryId,
                        Quantity = -item.Quantity,
                        PurchaseRate = latestProduct?.PurchaseRate ?? 0,
                        SellingPrice = item.SellingPrice,
                        Description = "Ordered stock",
                        PublishedDate = DateTime.Now
                    });
                }

                order.Status = "Delivered";
                _context.OrderActivityLogs.Add(new OrderActivityLog
                {
                    OrderId = order.Id,
                    Action = "Delivered",
                    PerformedBy = GetUserLabel(),
                    PerformedAt = DateTime.Now
                });
                _context.SaveChanges();
                TempData["OrderSuccess"] = "Order delivered & stock updated!";
            }
            else TempData["OrderError"] = "Order must be approved first!";

            return RedirectToAction("OrderRequest");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Seller")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteOrder(int id)
        {
            var order = _context.Orders.Include(o => o.Items).FirstOrDefault(o => o.Id == id);

            if (order != null && order.Status == "Pending")
            {
                _context.OrderActivityLogs.Add(new OrderActivityLog
                {
                    OrderId = order.Id,
                    Action = "Deleted",
                    PerformedBy = GetUserLabel(),
                    PerformedAt = DateTime.Now
                });
                _context.SaveChanges();

                _context.OrderItems.RemoveRange(order.Items);
                _context.Orders.Remove(order);
                _context.SaveChanges();
                TempData["OrderSuccess"] = "Order deleted!";
            }
            else TempData["OrderError"] = "Only pending orders can be deleted!";

            return RedirectToAction("OrderRequest");
        }

        [Authorize(Roles = "Admin,Manager,Seller")]
        public IActionResult EditOrder(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Category)
                .FirstOrDefault(o => o.Id == id);

            if (order == null || order.Status != "Pending")
                return RedirectToAction("OrderRequest");

            ViewBag.Categories = _context.Categories.ToList();
            return View(order);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Seller")]
        [ValidateAntiForgeryToken]
        public IActionResult EditOrder(int id, string orderedBy,
            List<int> categoryIds, List<string> productNames, List<int> quantities)
        {
            var order = _context.Orders.Include(o => o.Items).FirstOrDefault(o => o.Id == id);

            if (order == null || order.Status != "Pending")
                return RedirectToAction("OrderRequest");

            order.OrderedBy = orderedBy;
            _context.OrderItems.RemoveRange(order.Items);

            for (int i = 0; i < categoryIds.Count; i++)
            {
                var product = _context.Products
                    .Where(p => p.Name == productNames[i]
                             && p.CategoryId == categoryIds[i]
                             && p.SellingPrice > 0)
                    .OrderByDescending(p => p.PublishedDate).FirstOrDefault();

                if (product == null)
                    product = _context.Products
                        .Where(p => p.Name == productNames[i] && p.CategoryId == categoryIds[i])
                        .OrderByDescending(p => p.PublishedDate).FirstOrDefault();

                order.Items.Add(new OrderItem
                {
                    OrderId = order.Id,
                    CategoryId = categoryIds[i],
                    ProductName = productNames[i],
                    Quantity = quantities[i],
                    SellingPrice = product?.SellingPrice ?? 0
                });
            }

            _context.OrderActivityLogs.Add(new OrderActivityLog
            {
                OrderId = order.Id,
                Action = "Edited",
                PerformedBy = GetUserLabel(),
                PerformedAt = DateTime.Now
            });
            _context.SaveChanges();
            TempData["OrderSuccess"] = "Order updated!";
            return RedirectToAction("OrderRequest");
        }

        // ─────────────────────────────────────────
        // ORDER HISTORY
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> OrderHistory(int page = 1,
            string? sellerFilter = null)
        {
            int pageSize = 10;
            var currentUser = await _userManager.GetUserAsync(User);
            var isSeller = User.IsInRole("Seller");

            var query = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Category)
                .Where(o => o.Status == "Delivered")
                .AsQueryable();

            if (isSeller)
                query = query.Where(o => o.SellerUserId == currentUser!.Id);
            else if (!string.IsNullOrEmpty(sellerFilter) && sellerFilter != "all")
                query = query.Where(o => o.SellerUserId == sellerFilter);

            var all = query.OrderByDescending(o => o.OrderDate).ToList();

            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Orders = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SellerFilter = sellerFilter;

            if (!isSeller)
            {
                var sellers = await _userManager.GetUsersInRoleAsync("Seller");
                ViewBag.Sellers = sellers.Select(s => new { s.Id, s.FullName }).ToList();
            }

            return View();
        }

        // ─────────────────────────────────────────
        // ORDER INVOICES
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> DownloadOrderInvoice(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isSeller = User.IsInRole("Seller");

            var order = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Category)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();

            if (isSeller && order.SellerUserId != currentUser?.Id)
                return Forbid();

            return new Rotativa.AspNetCore.ViewAsPdf("OrderInvoice", order)
            {
                FileName = $"Order_Invoice_{id}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15),
                CustomSwitches = "--background --print-media-type"
            };
        }

        [Authorize(Roles = "Admin,Manager,Seller")]
        public async Task<IActionResult> DownloadMonthlyOrderInvoice(
            int month, int year, string? sellerFilter = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isSeller = User.IsInRole("Seller");

            var query = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Category)
                .Where(o => o.OrderDate.Month == month
                         && o.OrderDate.Year == year
                         && o.Status == "Delivered")
                .AsQueryable();

            if (isSeller)
                query = query.Where(o => o.SellerUserId == currentUser!.Id);
            else if (!string.IsNullOrEmpty(sellerFilter) && sellerFilter != "all")
                query = query.Where(o => o.SellerUserId == sellerFilter);

            var orders = query.OrderBy(o => o.OrderDate).ToList();

            var report = new MonthlyOrderReport
            {
                Orders = orders,
                Month = new DateTime(year, month, 1).ToString("MMMM yyyy")
            };

            return new Rotativa.AspNetCore.ViewAsPdf("MonthlyOrderInvoice", report)
            {
                FileName = $"Monthly_Order_{month}_{year}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15),
                CustomSwitches = "--background --print-media-type"
            };
        }

        // ─────────────────────────────────────────
        // MAKE PURCHASE
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult PurchaseProduct(int? categoryId = null,
            string? productName = null, decimal? unitPrice = null)
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.PrefilledCategoryId = categoryId;
            ViewBag.PrefilledProductName = productName;
            ViewBag.PrefilledUnitPrice = unitPrice;
            ViewBag.CurrentUserLabel = GetUserLabel();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurchaseProduct(string purchasedBy,
            string? supplierUserId, List<int> categoryIds,
            List<string> productNames, List<int> quantities,
            List<decimal> purchasePrices)
        {
            if (string.IsNullOrWhiteSpace(purchasedBy)
                || categoryIds == null || categoryIds.Count == 0)
            {
                TempData["Error"] = "Please fill all required fields.";
                ViewBag.Categories = _context.Categories.ToList();
                ViewBag.CurrentUserLabel = GetUserLabel();
                return View();
            }

            if (string.IsNullOrWhiteSpace(supplierUserId))
            {
                TempData["Error"] = "Please assign a supplier.";
                ViewBag.Categories = _context.Categories.ToList();
                ViewBag.CurrentUserLabel = GetUserLabel();
                return View();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            string? assignedSupplierUserId = User.IsInRole("Supplier")
                ? currentUser?.Id : supplierUserId;

            var purchase = new Purchase
            {
                PurchasedBy = purchasedBy,
                PurchaseDate = DateTime.Now,
                Status = "Pending",
                SupplierUserId = assignedSupplierUserId
            };

            for (int i = 0; i < categoryIds.Count; i++)
            {
                purchase.Items.Add(new PurchaseItem
                {
                    CategoryId = categoryIds[i],
                    ProductName = productNames[i],
                    Quantity = quantities[i],
                    PurchasePrice = purchasePrices != null && purchasePrices.Count > i
                        ? purchasePrices[i] : 0
                });
            }

            _context.Purchases.Add(purchase);
            _context.SaveChanges();

            _context.PurchaseActivityLogs.Add(new PurchaseActivityLog
            {
                PurchaseId = purchase.Id,
                Action = "Created",
                PerformedBy = GetUserLabel(),
                PerformedAt = DateTime.Now
            });
            _context.SaveChanges();

            TempData["Success"] = "Purchase request created!";
            return RedirectToAction("PurchaseRequest");
        }

        // ─────────────────────────────────────────
        // PURCHASE REQUEST
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager,Supplier")]
        public async Task<IActionResult> PurchaseRequest(int page = 1)
        {
            int pageSize = 10;
            var currentUser = await _userManager.GetUserAsync(User);
            var isSupplier = User.IsInRole("Supplier");

            var query = _context.Purchases
                .Include(p => p.Items).ThenInclude(i => i.Category)
                .AsQueryable();

            if (isSupplier)
                query = query.Where(p => p.SupplierUserId == currentUser!.Id);

            var all = query.OrderByDescending(p => p.PurchaseDate).ToList();

            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var purchaseIds = all.Select(p => p.Id).ToList();
            ViewBag.ActivityLogs = _context.PurchaseActivityLogs
                .Where(l => purchaseIds.Contains(l.PurchaseId))
                .OrderBy(l => l.PerformedAt)
                .ToList();

            ViewBag.Purchases = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Supplier")]
        [ValidateAntiForgeryToken]
        public IActionResult ApprovePurchase(int id)
        {
            var purchase = _context.Purchases
                .Include(p => p.Items).FirstOrDefault(p => p.Id == id);

            if (purchase != null && purchase.Status == "Pending")
            {
                purchase.Status = "Approved";
                _context.PurchaseActivityLogs.Add(new PurchaseActivityLog
                {
                    PurchaseId = purchase.Id,
                    Action = "Approved",
                    PerformedBy = GetUserLabel(),
                    PerformedAt = DateTime.Now
                });
                _context.SaveChanges();
                TempData["Success"] = "Purchase approved!";
            }
            else TempData["Error"] = "Only pending purchases can be approved!";

            return RedirectToAction("PurchaseRequest");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Supplier")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePurchase(int id)
        {
            var purchase = _context.Purchases
                .Include(p => p.Items).FirstOrDefault(p => p.Id == id);

            if (purchase != null && purchase.Status == "Pending")
            {
                _context.PurchaseActivityLogs.Add(new PurchaseActivityLog
                {
                    PurchaseId = purchase.Id,
                    Action = "Deleted",
                    PerformedBy = GetUserLabel(),
                    PerformedAt = DateTime.Now
                });
                _context.SaveChanges();

                _context.PurchaseItems.RemoveRange(purchase.Items);
                _context.Purchases.Remove(purchase);
                _context.SaveChanges();
                TempData["Success"] = "Purchase request deleted!";
            }
            else TempData["Error"] = "Only pending purchases can be deleted!";

            return RedirectToAction("PurchaseRequest");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Supplier")]
        [ValidateAntiForgeryToken]
        public IActionResult DeliverPurchase(int id)
        {
            var purchase = _context.Purchases
                .Include(p => p.Items).FirstOrDefault(p => p.Id == id);

            if (purchase != null && purchase.Status == "Approved")
            {
                foreach (var item in purchase.Items)
                {
                    var lastKnownSellingPrice = _context.Products
                        .Where(p => p.Name == item.ProductName
                                 && p.CategoryId == item.CategoryId
                                 && p.SellingPrice > 0)
                        .OrderByDescending(p => p.PublishedDate)
                        .Select(p => p.SellingPrice).FirstOrDefault();

                    _context.Products.Add(new AddProduct
                    {
                        Name = item.ProductName,
                        CategoryId = item.CategoryId,
                        Quantity = item.Quantity,
                        PurchaseRate = item.PurchasePrice,
                        SellingPrice = lastKnownSellingPrice,
                        Description = "Purchased stock",
                        PublishedDate = DateTime.Now
                    });
                }

                purchase.Status = "Delivered";
                _context.PurchaseActivityLogs.Add(new PurchaseActivityLog
                {
                    PurchaseId = purchase.Id,
                    Action = "Delivered",
                    PerformedBy = GetUserLabel(),
                    PerformedAt = DateTime.Now
                });
                _context.SaveChanges();
                TempData["Success"] = "Purchase delivered & stock updated!";
            }
            else TempData["Error"] = "Purchase must be approved first!";

            return RedirectToAction("PurchaseRequest");
        }

        // ─────────────────────────────────────────
        // EDIT PURCHASE
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager,Supplier")]
        public IActionResult EditPurchase(int id)
        {
            var purchase = _context.Purchases
                .Include(p => p.Items).ThenInclude(i => i.Category)
                .FirstOrDefault(p => p.Id == id);

            if (purchase == null || purchase.Status != "Pending")
                return RedirectToAction("PurchaseRequest");

            ViewBag.Categories = _context.Categories.ToList();
            return View(purchase);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Supplier")]
        [ValidateAntiForgeryToken]
        public IActionResult EditPurchase(int id, string purchasedBy,
            List<int> categoryIds, List<string> productNames,
            List<int> quantities, List<decimal> purchasePrices)
        {
            var purchase = _context.Purchases
                .Include(p => p.Items).FirstOrDefault(p => p.Id == id);

            if (purchase == null || purchase.Status != "Pending")
                return RedirectToAction("PurchaseRequest");

            purchase.PurchasedBy = purchasedBy;
            _context.PurchaseItems.RemoveRange(purchase.Items);

            for (int i = 0; i < categoryIds.Count; i++)
            {
                purchase.Items.Add(new PurchaseItem
                {
                    PurchaseId = purchase.Id,
                    CategoryId = categoryIds[i],
                    ProductName = productNames[i],
                    Quantity = quantities[i],
                    PurchasePrice = purchasePrices != null && purchasePrices.Count > i
                        ? purchasePrices[i] : 0
                });
            }

            _context.PurchaseActivityLogs.Add(new PurchaseActivityLog
            {
                PurchaseId = purchase.Id,
                Action = "Edited",
                PerformedBy = GetUserLabel(),
                PerformedAt = DateTime.Now
            });
            _context.SaveChanges();
            TempData["Success"] = "Purchase updated!";
            return RedirectToAction("PurchaseRequest");
        }

        // ─────────────────────────────────────────
        // PURCHASE HISTORY
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager,Supplier")]
        public async Task<IActionResult> PurchaseHistory(int page = 1,
            string? supplierFilter = null)
        {
            int pageSize = 10;
            var currentUser = await _userManager.GetUserAsync(User);
            var isSupplier = User.IsInRole("Supplier");

            var query = _context.Purchases
                .Include(p => p.Items).ThenInclude(i => i.Category)
                .Where(p => p.Status == "Delivered")
                .AsQueryable();

            if (isSupplier)
                query = query.Where(p => p.SupplierUserId == currentUser!.Id);
            else if (!string.IsNullOrEmpty(supplierFilter) && supplierFilter != "all")
                query = query.Where(p => p.SupplierUserId == supplierFilter);

            var all = query.OrderByDescending(p => p.PurchaseDate).ToList();

            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Purchases = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SupplierFilter = supplierFilter;

            if (!isSupplier)
            {
                var suppliers = await _userManager.GetUsersInRoleAsync("Supplier");
                ViewBag.Suppliers = suppliers
                    .Select(s => new { s.Id, s.FullName }).ToList();
            }

            return View();
        }

        // ─────────────────────────────────────────
        // PURCHASE INVOICES
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin,Manager,Supplier")]
        public async Task<IActionResult> DownloadPurchaseInvoice(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isSupplier = User.IsInRole("Supplier");

            var purchase = _context.Purchases
                .Include(p => p.Items).ThenInclude(i => i.Category)
                .FirstOrDefault(p => p.Id == id);

            if (purchase == null) return NotFound();

            if (isSupplier && purchase.SupplierUserId != currentUser?.Id)
                return Forbid();

            return new Rotativa.AspNetCore.ViewAsPdf("PurchaseInvoice", purchase)
            {
                FileName = $"Purchase_Invoice_{id}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15),
                CustomSwitches = "--background --print-media-type"
            };
        }

        [Authorize(Roles = "Admin,Manager,Supplier")]
        public async Task<IActionResult> DownloadMonthlyPurchaseInvoice(
            int month, int year, string? supplierFilter = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isSupplier = User.IsInRole("Supplier");

            var query = _context.Purchases
                .Include(p => p.Items).ThenInclude(i => i.Category)
                .Where(p => p.PurchaseDate.Month == month
                         && p.PurchaseDate.Year == year
                         && p.Status == "Delivered")
                .AsQueryable();

            if (isSupplier)
                query = query.Where(p => p.SupplierUserId == currentUser!.Id);
            else if (!string.IsNullOrEmpty(supplierFilter) && supplierFilter != "all")
                query = query.Where(p => p.SupplierUserId == supplierFilter);

            var purchases = query.OrderBy(p => p.PurchaseDate).ToList();

            var report = new MonthlyPurchaseReport
            {
                Purchases = purchases,
                Month = new DateTime(year, month, 1).ToString("MMMM yyyy")
            };

            return new Rotativa.AspNetCore.ViewAsPdf("MonthlyPurchaseInvoice", report)
            {
                FileName = $"Monthly_Purchase_{month}_{year}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15),
                CustomSwitches = "--background --print-media-type"
            };
        }

        // ─────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────
        private string GetUserLabel()
        {
            var user = _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefault(u => u.UserName == User.Identity!.Name);

            if (user == null) return User.Identity!.Name ?? "Unknown";

            var roleId = _context.UserRoles
                .FirstOrDefault(ur => ur.UserId == user.Id)?.RoleId;

            var roleName = roleId != null
                ? _context.Roles.FirstOrDefault(r => r.Id == roleId)?.Name ?? ""
                : "";

            string fullName = !string.IsNullOrEmpty(user.FullName)
                ? user.FullName : user.UserName ?? "Unknown";

            return string.IsNullOrEmpty(roleName)
                ? fullName
                : $"{fullName} ({roleName})";

        }
        private string FormatOrderNumber(int orderId, DateTime orderDate)
        {
            return $"{orderId:D4}{orderDate.Month:D2}{orderDate.Year}";
        }

        private string FormatPurchaseNumber(int purchaseId, DateTime purchaseDate)
        {
            return $"{purchaseId:D4}{purchaseDate.Month:D2}{purchaseDate.Year}";
        }
    }
}