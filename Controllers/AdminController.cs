using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGear_Online.Models;
using SmartGear_Online.Models.ViewModels;
using SmartGear_Online.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    /// QUESTION 10: ADMIN CONTROLLER WITH ROLE AUTHORIZATION
    /// Only users with Admin role can access these actions
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;

        public AdminController(IProductRepository productRepository,
                               IOrderRepository orderRepository)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
        }

        // ================================================
        // DASHBOARD - Admin only
        // Builds & passes AdminDashboardViewModel
        // ================================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var allOrders = await _orderRepository.GetAllOrdersAsync();
            var allProducts = await _productRepository.GetProductsAsync(1, 1000);

            var model = new AdminDashboardViewModel
            {
                // DATA INTEGRITY: revenue only counts orders that actually
                // generated money — cancelled orders are excluded from every
                // revenue metric below to match your sales reports.
                TotalOrders = allOrders.Count(),
                TotalRevenue = allOrders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .Sum(o => o.TotalPrice),
                AverageOrderValue = allOrders.Any()
                    ? allOrders.Where(o => o.Status != OrderStatus.Cancelled).Average(o => o.TotalPrice)
                    : 0m,

                TodayRevenue = allOrders
                    .Where(o => o.Status != OrderStatus.Cancelled &&
                                o.OrderDate.Date == DateTime.UtcNow.Date)
                    .Sum(o => o.TotalPrice),
                WeekRevenue = allOrders
                    .Where(o => o.Status != OrderStatus.Cancelled &&
                                o.OrderDate >= DateTime.UtcNow.AddDays(-7))
                    .Sum(o => o.TotalPrice),
                MonthRevenue = allOrders
                    .Where(o => o.Status != OrderStatus.Cancelled &&
                                o.OrderDate >= DateTime.UtcNow.AddDays(-30))
                    .Sum(o => o.TotalPrice),

                // Cancelled orders still matter for the status counts below,
                // so those are intentionally NOT filtered here.
                PendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending),
                ConfirmedOrders = allOrders.Count(o => o.Status == OrderStatus.Confirmed),
                ProductionOrders = allOrders.Count(o => o.Status == OrderStatus.InProduction),
                ShippedOrders = allOrders.Count(o => o.Status == OrderStatus.Shipped),
                DeliveredOrders = allOrders.Count(o => o.Status == OrderStatus.Delivered),
                CancelledOrders = allOrders.Count(o => o.Status == OrderStatus.Cancelled),

                LowStockAlerts = allProducts
                    .Where(p => p.QuantityInStock <= p.ReorderLevel)
                    .Select(p => new LowStockAlertViewModel
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        CurrentStock = p.QuantityInStock,
                        ReorderLevel = p.ReorderLevel
                    }).ToList(),

                RecentOrders = allOrders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new RecentOrderViewModel
                    {
                        OrderId = o.OrderId,
                        CustomerName = o.Customer?.FullName ?? "Unknown",
                        OrderDate = o.OrderDate,
                        TotalPrice = o.TotalPrice,
                        Status = o.Status
                    }).ToList(),

                TopProducts = allOrders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new TopProductViewModel
                    {
                        ProductId = g.Key,
                        ProductName = g.First().Product?.ProductName ?? "Unknown",
                        Category = g.First().Product?.Category ?? "N/A",
                        UnitsSold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                        Trend = 0
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(5)
                    .ToList(),

                DailyRevenue = BuildDailyRevenue(allOrders)
            };

            return View(model);
        }

        /// <summary>
        /// Zero-fills the last 30 days so the revenue chart always shows a full range.
        /// </summary>
        private static List<DailyRevenueViewModel> BuildDailyRevenue(IEnumerable<Order> orders)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-29);
            return Enumerable.Range(0, 30)
                .Select(i => startDate.AddDays(i))
                .Select(day => new DailyRevenueViewModel
                {
                    Date = day,
                    Revenue = orders
                        .Where(o => o.Status != OrderStatus.Cancelled &&
                                    o.OrderDate.Date == day)
                        .Sum(o => o.TotalPrice),
                    OrderCount = orders.Count(o => o.OrderDate.Date == day)
                })
                .ToList();
        }

        // ================================================
        // MANAGE PRODUCTS - Admin only
        // Redirects to Inventory view
        // ================================================
        [HttpGet]
        public async Task<IActionResult> ManageProducts()
        {
            var products = await _productRepository.GetProductsAsync(1, 100);
            return View("Inventory", products);
        }

        // ================================================
        // INVENTORY - Admin only (explicit route)
        // ================================================
        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var products = await _productRepository.GetProductsAsync(1, 100);
            return View(products);
        }

        // ================================================
        // ADD PRODUCT - Admin only with CSRF protection
        // ================================================
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                // DATA INTEGRITY: friendly duplicate-name check instead of a 500
                // from the unique index.
                if (await _productRepository.ProductNameExistsAsync(product.ProductName))
                {
                    ModelState.AddModelError("ProductName",
                        "A product with this name already exists. Choose a different name.");
                    return View(product);
                }

                product.CreatedDate = DateTime.UtcNow;
                product.UpdatedDate = DateTime.UtcNow;
                await _productRepository.AddProductAsync(product);

                TempData["Success"] = "'" + product.ProductName + "' was added successfully.";
                return RedirectToAction("Inventory");
            }
            return View(product);
        }

        // ================================================
        // FIX: EditProduct was missing entirely — Inventory.cshtml
        // linked here but the action didn't exist, causing a 404.
        // Reuses the existing Product/Edit view to avoid duplicating
        // the edit form markup that already works correctly.
        // ================================================
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            return RedirectToAction("Edit", "Product", new { id });
        }

        // ================================================
        // DELETE PRODUCT - Admin only with CSRF protection
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _productRepository.DeleteProductAsync(id);
            return RedirectToAction("Inventory");
        }
    }
}
