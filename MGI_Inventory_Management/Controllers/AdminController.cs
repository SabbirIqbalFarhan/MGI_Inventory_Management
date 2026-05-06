using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MGI_Inventory_Management.Data;
using MGI_Inventory_Management.Models;
using System;
using System.Linq;

namespace MGI_Inventory_Management.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────
        public IActionResult Index()
        {
            return View();
        }

        // ─────────────────────────────────────────
        // MANAGE CATEGORIES
        // ─────────────────────────────────────────
        public IActionResult ManageCategories(int page = 1)
        {
            int pageSize = 10;
            var all = _context.Categories.ToList();
            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Categories = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCategory(string categoryName)
        {
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                bool exists = _context.Categories
                    .Any(c => c.Name.ToLower() == categoryName.ToLower());

                if (!exists)
                {
                    _context.Categories.Add(new Category { Name = categoryName });
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
                    TempData["CategoryError"] = "Cannot delete — products exist under this category!";
                    return RedirectToAction("ManageCategories");
                }

                _context.Categories.Remove(category);
                _context.SaveChanges();
                TempData["CategorySuccess"] = "Category deleted successfully!";
            }
            return RedirectToAction("ManageCategories");
        }

        // ─────────────────────────────────────────
        // MANAGE MASTER PRODUCTS
        // ─────────────────────────────────────────
        public IActionResult ManageProducts(int page = 1)
        {
            int pageSize = 10;
            var allMasters = _context.ProductMasters.Include(p => p.Category).ToList();
            int totalPages = (int)Math.Ceiling(allMasters.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.ProductMasters = allMasters.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProductMaster(string productName, int categoryId, string description)
        {
            if (!string.IsNullOrWhiteSpace(productName) && categoryId > 0)
            {
                bool exists = _context.ProductMasters
                    .Any(p => p.ProductName.ToLower() == productName.ToLower()
                           && p.CategoryId == categoryId);

                if (!exists)
                {
                    _context.ProductMasters.Add(new ProductMaster
                    {
                        ProductName = productName,
                        CategoryId = categoryId,
                        Description = description ?? string.Empty
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
        public IActionResult DeleteProductMaster(int id)
        {
            var item = _context.ProductMasters.Find(id);
            if (item != null)
            {
                _context.ProductMasters.Remove(item);
                _context.SaveChanges();
                TempData["MasterSuccess"] = "Product removed from master list!";
            }
            return RedirectToAction("ManageProducts");
        }

        // ─────────────────────────────────────────
        // ADD PRODUCT
        // ─────────────────────────────────────────
        public IActionResult AddProduct()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProduct(AddProduct product)
        {
            ViewBag.Categories = _context.Categories.ToList();
            ModelState.Remove("Category");

            if (!ModelState.IsValid)
                return View(product);

            product.PublishedDate = DateTime.Now;
            _context.Products.Add(product);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Product added successfully!";
            return RedirectToAction("ViewProduct");
        }

        // ─────────────────────────────────────────
        // GET PRODUCTS BY CATEGORY (AJAX)
        // ─────────────────────────────────────────
        public IActionResult GetProductsByCategory(int categoryId)
        {
            var products = _context.ProductMasters
                .Where(p => p.CategoryId == categoryId)
                .Select(p => new { p.Id, p.ProductName, p.Description })
                .ToList();

            return Json(products);
        }

        // ─────────────────────────────────────────
        // GET SELLING PRICE (AJAX)
        // Returns the most recent non-zero selling price for the product.
        // "Purchased stock" records have SellingPrice = 0, so we skip those
        // and look for the latest record that actually has a selling price set.
        // ─────────────────────────────────────────
        public IActionResult GetSellingPrice(string productName, int categoryId)
        {
            // BUG FIX #3: Skip records where SellingPrice = 0 (e.g. "Purchased stock")
            // so the order form always shows the real selling price.
            var product = _context.Products
                .Where(p => p.Name == productName
                         && p.CategoryId == categoryId
                         && p.SellingPrice > 0)
                .OrderByDescending(p => p.PublishedDate)
                .FirstOrDefault();

            // Fallback: if still nothing found, return any latest record
            if (product == null)
            {
                product = _context.Products
                    .Where(p => p.Name == productName && p.CategoryId == categoryId)
                    .OrderByDescending(p => p.PublishedDate)
                    .FirstOrDefault();
            }

            return Json(new { price = product?.SellingPrice ?? 0 });
        }

        // ─────────────────────────────────────────
        // GET PURCHASE RATE (AJAX)
        // ─────────────────────────────────────────
        public IActionResult GetPurchaseRate(string productName, int categoryId)
        {
            var product = _context.Products
                .Where(p => p.Name == productName && p.CategoryId == categoryId)
                .OrderByDescending(p => p.PublishedDate)
                .FirstOrDefault();

            return Json(new { price = product?.PurchaseRate ?? 0 });
        }

        // ─────────────────────────────────────────
        // VIEW PRODUCTS
        // ─────────────────────────────────────────
        public IActionResult ViewProduct(int page1 = 1, int page2 = 1)
        {
            int pageSize = 10;

            // ─── TABLE 1 — every individual record, newest first ───
            var allEntriesList = _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.PublishedDate)
                .ToList();  // full list for grouping

            int totalPages1 = (int)Math.Ceiling(allEntriesList.Count / (double)pageSize);
            if (totalPages1 == 0) totalPages1 = 1;

            var pagedEntries = allEntriesList
                .Skip((page1 - 1) * pageSize)
                .Take(pageSize)
                .ToList();  // paged list only for Table 1 display

            // ─── TABLE 2 — net stock summary (group from FULL list) ───
            var groupedList = allEntriesList   // ✅ use full list, NOT pagedEntries
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
                            m.ProductName.ToLower() == g.Key.Name.ToLower() &&
                            m.CategoryId == g.Key.CategoryId);

                    var netQuantity = g.Sum(x => x.Quantity);

                    var latestProduct = g
                        .Where(x => !x.Description.StartsWith("Deleted stock (original")
                                 && !x.Description.StartsWith("Edited stock (old)"))
                        .OrderByDescending(x => x.PublishedDate)
                        .FirstOrDefault();

                    return new
                    {
                        ProductName = g.Key.Name,
                        Category = g.Key.CategoryName,
                        Description = master != null ? master.Description : "-",
                        TotalQuantity = netQuantity,
                        CategoryId = g.Key.CategoryId,
                        LatestId = latestProduct != null ? latestProduct.Id : 0
                    };
                })
                .ToList();

            int totalPages2 = (int)Math.Ceiling(groupedList.Count / (double)pageSize);
            if (totalPages2 == 0) totalPages2 = 1;

            ViewBag.AllEntries = pagedEntries;
            ViewBag.GroupedProducts = groupedList
                .Skip((page2 - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            ViewBag.CurrentPage1 = page1;
            ViewBag.TotalPages1 = totalPages1;
            ViewBag.CurrentPage2 = page2;
            ViewBag.TotalPages2 = totalPages2;

            return View();
        }

        // ─────────────────────────────────────────
        // EDIT PRODUCT GET
        // ─────────────────────────────────────────
        public IActionResult EditProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            // If this record has no selling price (e.g. it was a "Purchased stock" record
            // which always stores SellingPrice = 0), pre-fill from the last known
            // non-zero selling price for this product so the edit form is useful.
            if (product.SellingPrice == 0)
            {
                var lastKnownPrice = _context.Products
                    .Where(p => p.Name == product.Name
                             && p.CategoryId == product.CategoryId
                             && p.SellingPrice > 0)
                    .OrderByDescending(p => p.PublishedDate)
                    .Select(p => p.SellingPrice)
                    .FirstOrDefault();

                product.SellingPrice = lastKnownPrice;
            }

            // Same for purchase rate
            if (product.PurchaseRate == 0)
            {
                var lastKnownRate = _context.Products
                    .Where(p => p.Name == product.Name
                             && p.CategoryId == product.CategoryId
                             && p.PurchaseRate > 0)
                    .OrderByDescending(p => p.PublishedDate)
                    .Select(p => p.PurchaseRate)
                    .FirstOrDefault();

                product.PurchaseRate = lastKnownRate;
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        // ─────────────────────────────────────────
        // EDIT PRODUCT POST
        //
        // How it works:
        //   - The existing record is STAMPED in Table 1 as a historical audit entry
        //     (its quantity remains visible in Table 1 for the history log).
        //   - A NEW record is inserted with whatever the user typed.
        //     Quantity can be positive (adds stock) or negative (removes stock).
        //     Table 2 net = sum of all records, so the new record's quantity is
        //     added directly to the running total — no offset needed.
        //
        // Example: net stock is 30 (from previous records).
        //   User edits and types Quantity = 10  → net becomes 40.
        //   User edits and types Quantity = -10 → net becomes 20.
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProduct(AddProduct product)
        {
            ViewBag.Categories = _context.Categories.ToList();
            ModelState.Remove("Category");
            ModelState.Remove("Description");

            if (!ModelState.IsValid)
                return View(product);

            var existing = _context.Products.Find(product.Id);
            if (existing == null) return NotFound();

            // 1. Stamp original record as a historical audit entry in Table 1.
            //    Its quantity is preserved so Table 1 shows an accurate history log.
            existing.Description = $"Edited stock (old) — was {existing.Quantity} units @ {existing.SellingPrice}";
            _context.Products.Update(existing);

            // 2. Insert a NEW record with the user's values.
            //    The quantity typed by the user is added directly to the Table 2 net sum.
            _context.Products.Add(new AddProduct
            {
                Name = existing.Name,           // name is locked (readonly in view)
                CategoryId = product.CategoryId,
                Quantity = product.Quantity,        // positive = adds stock, negative = removes stock
                PurchaseRate = product.PurchaseRate,
                SellingPrice = product.SellingPrice,
                Description = "Edited stock",
                PublishedDate = DateTime.Now
            });

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction("ViewProduct");
        }

        // ─────────────────────────────────────────
        // DELETE PRODUCT
        //
        // BUG FIX #1: The old code used product.Quantity (the single record's
        // quantity) as the negative offset, which only cancelled that one record.
        // If multiple records existed for the same product (add + edit + purchase
        // etc.) the net quantity in Table 2 was not fully zeroed.
        //
        // Fix: calculate the FULL current net quantity for this product group
        // and insert a single negative record that exactly cancels it to zero.
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            // Calculate the true net quantity for this product group
            var netQuantity = _context.Products
                .Where(p => p.Name == product.Name && p.CategoryId == product.CategoryId)
                .Sum(p => (int?)p.Quantity) ?? 0;

            // Stamp original record for audit trail
            product.Description = $"Deleted stock (original — {product.Quantity} units)";
            _context.Products.Update(product);

            // Insert a single negative record that brings net to exactly zero
            if (netQuantity != 0)
            {
                _context.Products.Add(new AddProduct
                {
                    Name = product.Name,
                    CategoryId = product.CategoryId,
                    Quantity = -netQuantity,          // cancels the full net, not just one record
                    PurchaseRate = product.PurchaseRate,
                    SellingPrice = product.SellingPrice,
                    Description = "Deleted stock",
                    PublishedDate = DateTime.Now
                });
            }

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction("ViewProduct");
        }

        // ─────────────────────────────────────────
        // MAKE ORDER
        // ─────────────────────────────────────────
        public IActionResult Order()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Order(string orderedBy, List<int> categoryIds,
                                   List<string> productNames, List<int> quantities)
        {
            if (string.IsNullOrWhiteSpace(orderedBy) || categoryIds == null || categoryIds.Count == 0)
            {
                TempData["OrderError"] = "Please fill all fields.";
                ViewBag.Categories = _context.Categories.ToList();
                return View();
            }

            var order = new Order
            {
                OrderedBy = orderedBy,
                OrderDate = DateTime.Now,
                Status = "Pending"
            };

            for (int i = 0; i < categoryIds.Count; i++)
            {
                // BUG FIX #3 (price lookup): use the same non-zero price logic
                var product = _context.Products
                    .Where(p => p.Name == productNames[i]
                             && p.CategoryId == categoryIds[i]
                             && p.SellingPrice > 0)
                    .OrderByDescending(p => p.PublishedDate)
                    .FirstOrDefault();

                // Fallback to any latest record if no priced record found
                if (product == null)
                {
                    product = _context.Products
                        .Where(p => p.Name == productNames[i] && p.CategoryId == categoryIds[i])
                        .OrderByDescending(p => p.PublishedDate)
                        .FirstOrDefault();
                }

                order.Items.Add(new OrderItem
                {
                    CategoryId = categoryIds[i],
                    ProductName = productNames[i],
                    Quantity = quantities[i],
                    SellingPrice = product?.SellingPrice ?? 0
                });
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            TempData["OrderSuccess"] = "Order placed successfully!";
            return RedirectToAction("OrderRequest");
        }

        // ─────────────────────────────────────────
        // ORDER REQUEST
        // ─────────────────────────────────────────
        public IActionResult OrderRequest(int page = 1)
        {
            int pageSize = 10;

            var all = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Category)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Orders = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View();
        }

        // ─────────────────────────────────────────
        // APPROVE ORDER
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveOrder(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id);

            if (order != null && order.Status == "Pending")
            {
                foreach (var item in order.Items)
                {
                    var netStock = _context.Products
                        .Where(p => p.Name == item.ProductName
                                 && p.CategoryId == item.CategoryId)
                        .Sum(p => (int?)p.Quantity) ?? 0;

                    var reservedStock = _context.Orders
                        .Include(o => o.Items)
                        .Where(o => o.Status == "Approved" && o.Id != id)
                        .SelectMany(o => o.Items)
                        .Where(i => i.ProductName == item.ProductName
                                 && i.CategoryId == item.CategoryId)
                        .Sum(i => (int?)i.Quantity) ?? 0;

                    var available = netStock - reservedStock;

                    if (available < item.Quantity)
                    {
                        TempData["OrderError"] =
                            $"Not enough stock for '{item.ProductName}'! " +
                            $"Available: {available}, Requested: {item.Quantity}";
                        return RedirectToAction("OrderRequest");
                    }
                }

                order.Status = "Approved";
                _context.SaveChanges();
                TempData["OrderSuccess"] = "Order approved!";
            }
            else
            {
                TempData["OrderError"] = "Only pending orders can be approved!";
            }

            return RedirectToAction("OrderRequest");
        }

        // ─────────────────────────────────────────
        // DELIVER ORDER
        //
        // BUG FIX #4: The double-quantity reduction was caused by EditProduct
        // adding a new positive record WITHOUT first cancelling the old one,
        // so the net stock was doubled — meaning when delivery deducted the
        // ordered quantity once, it appeared to deduct twice relative to what
        // the user expected.  EditProduct is now fixed (see above), so delivery
        // only needs to insert one negative record per item as before.
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeliverOrder(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id);

            if (order != null && order.Status == "Approved")
            {
                foreach (var item in order.Items)
                {
                    var latestProduct = _context.Products
                        .Where(p => p.Name == item.ProductName
                                 && p.CategoryId == item.CategoryId)
                        .OrderByDescending(p => p.PublishedDate)
                        .FirstOrDefault();

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
                _context.SaveChanges();
                TempData["OrderSuccess"] = "Order delivered & stock updated!";
            }
            else
            {
                TempData["OrderError"] = "Order must be approved first!";
            }

            return RedirectToAction("OrderRequest");
        }

        // ─────────────────────────────────────────
        // DELETE ORDER
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteOrder(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id);

            if (order != null && order.Status == "Pending")
            {
                _context.OrderItems.RemoveRange(order.Items);
                _context.Orders.Remove(order);
                _context.SaveChanges();
                TempData["OrderSuccess"] = "Order deleted!";
            }
            else
            {
                TempData["OrderError"] = "Only pending orders can be deleted!";
            }

            return RedirectToAction("OrderRequest");
        }

        // ─────────────────────────────────────────
        // EDIT ORDER GET
        // ─────────────────────────────────────────
        public IActionResult EditOrder(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Category)
                .FirstOrDefault(o => o.Id == id);

            if (order == null || order.Status != "Pending")
                return RedirectToAction("OrderRequest");

            ViewBag.Categories = _context.Categories.ToList();
            return View(order);
        }

        // ─────────────────────────────────────────
        // EDIT ORDER POST
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditOrder(int id, string orderedBy,
            List<int> categoryIds, List<string> productNames, List<int> quantities)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id);

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
                    .OrderByDescending(p => p.PublishedDate)
                    .FirstOrDefault();

                if (product == null)
                {
                    product = _context.Products
                        .Where(p => p.Name == productNames[i] && p.CategoryId == categoryIds[i])
                        .OrderByDescending(p => p.PublishedDate)
                        .FirstOrDefault();
                }

                order.Items.Add(new OrderItem
                {
                    OrderId = order.Id,
                    CategoryId = categoryIds[i],
                    ProductName = productNames[i],
                    Quantity = quantities[i],
                    SellingPrice = product?.SellingPrice ?? 0
                });
            }

            _context.SaveChanges();
            TempData["OrderSuccess"] = "Order updated!";
            return RedirectToAction("OrderRequest");
        }

        // ─────────────────────────────────────────
        // ORDER HISTORY
        // ─────────────────────────────────────────
        public IActionResult OrderHistory(int page = 1)
        {
            int pageSize = 10;

            var all = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Category)
                .Where(o => o.Status == "Delivered")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Orders = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View();
        }

        // ─────────────────────────────────────────
        // PURCHASE PRODUCT PAGE
        // ─────────────────────────────────────────
        public IActionResult PurchaseProduct()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        // ─────────────────────────────────────────
        // PURCHASE PRODUCT POST
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PurchaseProduct(string purchasedBy,
            List<int> categoryIds,
            List<string> productNames,
            List<int> quantities,
            List<decimal> purchasePrices)
        {
            if (string.IsNullOrWhiteSpace(purchasedBy) || categoryIds == null || categoryIds.Count == 0)
            {
                TempData["Error"] = "Please fill all fields.";
                ViewBag.Categories = _context.Categories.ToList();
                return View();
            }

            var purchase = new Purchase
            {
                PurchasedBy = purchasedBy,
                PurchaseDate = DateTime.Now,
                Status = "Pending"
            };

            for (int i = 0; i < categoryIds.Count; i++)
            {
                purchase.Items.Add(new PurchaseItem
                {
                    CategoryId = categoryIds[i],
                    ProductName = productNames[i],
                    Quantity = quantities[i],
                    PurchasePrice = purchasePrices != null && purchasePrices.Count > i
                                        ? purchasePrices[i]
                                        : 0
                });
            }

            _context.Purchases.Add(purchase);
            _context.SaveChanges();

            TempData["Success"] = "Purchase request created!";
            return RedirectToAction("PurchaseRequest");
        }

        // ─────────────────────────────────────────
        // PURCHASE REQUEST
        // ─────────────────────────────────────────
        public IActionResult PurchaseRequest(int page = 1)
        {
            int pageSize = 10;

            var all = _context.Purchases
                .Include(p => p.Items)
                .ThenInclude(i => i.Category)
                .OrderByDescending(p => p.PurchaseDate)
                .ToList();

            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Purchases = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View();
        }

        // ─────────────────────────────────────────
        // APPROVE PURCHASE
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApprovePurchase(int id)
        {
            var purchase = _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefault(p => p.Id == id);

            if (purchase != null && purchase.Status == "Pending")
            {
                purchase.Status = "Approved";
                _context.SaveChanges();
                TempData["Success"] = "Purchase approved!";
            }
            else
            {
                TempData["Error"] = "Only pending purchases can be approved!";
            }

            return RedirectToAction("PurchaseRequest");
        }

        // ─────────────────────────────────────────
        // DELIVER PURCHASE — stock added here
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeliverPurchase(int id)
        {
            var purchase = _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefault(p => p.Id == id);

            if (purchase != null && purchase.Status == "Approved")
            {
                foreach (var item in purchase.Items)
                {
                    // Carry the last known selling price for this product so that
                    // Table 1 shows a meaningful selling price (not 0) and the
                    // Edit form pre-fills correctly without a separate lookup.
                    var lastKnownSellingPrice = _context.Products
                        .Where(p => p.Name == item.ProductName
                                 && p.CategoryId == item.CategoryId
                                 && p.SellingPrice > 0)
                        .OrderByDescending(p => p.PublishedDate)
                        .Select(p => p.SellingPrice)
                        .FirstOrDefault();

                    _context.Products.Add(new AddProduct
                    {
                        Name = item.ProductName,
                        CategoryId = item.CategoryId,
                        Quantity = item.Quantity,
                        PurchaseRate = item.PurchasePrice,
                        SellingPrice = lastKnownSellingPrice,   // 0 if brand-new product, real price otherwise
                        Description = "Purchased stock",
                        PublishedDate = DateTime.Now
                    });
                }

                purchase.Status = "Delivered";
                _context.SaveChanges();
                TempData["Success"] = "Purchase delivered & stock updated!";
            }
            else
            {
                TempData["Error"] = "Purchase must be approved first!";
            }

            return RedirectToAction("PurchaseRequest");
        }

        // ─────────────────────────────────────────
        // DELETE PURCHASE
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePurchase(int id)
        {
            var purchase = _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefault(p => p.Id == id);

            if (purchase != null && purchase.Status == "Pending")
            {
                _context.PurchaseItems.RemoveRange(purchase.Items);
                _context.Purchases.Remove(purchase);
                _context.SaveChanges();
                TempData["Success"] = "Purchase request deleted!";
            }
            else
            {
                TempData["Error"] = "Only pending purchases can be deleted!";
            }

            return RedirectToAction("PurchaseRequest");
        }

        // ─────────────────────────────────────────
        // PURCHASE HISTORY
        // ─────────────────────────────────────────
        public IActionResult PurchaseHistory(int page = 1)
        {
            int pageSize = 10;

            var all = _context.Purchases
                .Include(p => p.Items)
                .ThenInclude(i => i.Category)
                .Where(p => p.Status == "Delivered")
                .OrderByDescending(p => p.PurchaseDate)
                .ToList();

            int totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            ViewBag.Purchases = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View();
        }

        // ─────────────────────────────────────────
        // ORDER INVOICE
        // ─────────────────────────────────────────
        public IActionResult DownloadOrderInvoice(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Category)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();

            return new Rotativa.AspNetCore.ViewAsPdf("OrderInvoice", order)
            {
                FileName = $"Order_Invoice_{id}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15),
                CustomSwitches = "--background --print-media-type"
            };
        }

        // ─────────────────────────────────────────
        // PURCHASE INVOICE
        // ─────────────────────────────────────────
        public IActionResult DownloadPurchaseInvoice(int id)
        {
            var purchase = _context.Purchases
                .Include(p => p.Items)
                .ThenInclude(i => i.Category)
                .FirstOrDefault(p => p.Id == id);

            if (purchase == null) return NotFound();

            return new Rotativa.AspNetCore.ViewAsPdf("PurchaseInvoice", purchase)
            {
                FileName = $"Purchase_Invoice_{id}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(15, 15, 15, 15),
                CustomSwitches = "--background --print-media-type"
            };
        }

        // ─────────────────────────────────────────
        // MONTHLY ORDER INVOICE
        // ─────────────────────────────────────────
        public IActionResult DownloadMonthlyOrderInvoice(int month, int year)
        {
            var orders = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Category)
                .Where(o => o.OrderDate.Month == month
                         && o.OrderDate.Year == year
                         && o.Status == "Delivered")
                .OrderBy(o => o.OrderDate)
                .ToList();

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
        // MONTHLY PURCHASE INVOICE
        // ─────────────────────────────────────────
        public IActionResult DownloadMonthlyPurchaseInvoice(int month, int year)
        {
            var purchases = _context.Purchases
                .Include(p => p.Items)
                .ThenInclude(i => i.Category)
                .Where(p => p.PurchaseDate.Month == month
                         && p.PurchaseDate.Year == year
                         && p.Status == "Delivered")
                .OrderBy(p => p.PurchaseDate)
                .ToList();

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
    }
}
