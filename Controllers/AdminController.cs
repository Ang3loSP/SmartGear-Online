using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGear_Online.Models;
using SmartGear_Online.Models.ViewModels;
using SmartGear_Online.Repositories;
using SmartGear_Online.Services;
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
        private readonly IReportService _reportService;

        public AdminController(IProductRepository productRepository,
                               IOrderRepository orderRepository,
                               IReportService reportService)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _reportService = reportService;
        }

        // ================================================
        // DASHBOARD - Admin only
        // Builds & passes AdminDashboardViewModel
        // ================================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.UtcNow;

            // All aggregate figures come from ReportService (SQL-side
            // aggregation with short-lived caching) + a bounded "recent
            // orders" query — the dashboard never loads every order and every
            // product into memory just to render itself.
            var metrics = await _reportService.GetRevenueMetricsAsync();
            var statistics = await _reportService.GetOrderStatisticsAsync();
            var customerAnalytics = await _reportService.GetCustomerAnalyticsAsync();
            var topProducts = await _reportService.GetTopProductsAsync(5);
            var lowStock = await _reportService.GetLowStockByReorderLevelAsync();
            var dailyRevenue = await _reportService.GetDailyRevenueAsync(now.Date.AddDays(-29), now.Date);
            var recentOrders = await _orderRepository.GetRecentOrdersAsync(10);

            var model = new AdminDashboardViewModel
            {
                // DATA INTEGRITY: revenue only counts orders that actually
                // generated money — cancelled orders are excluded from every
                // revenue metric below to match your sales reports.
                TotalOrders = statistics.TotalOrders + statistics.CancelledOrders,
                TotalRevenue = metrics.TotalRevenue,
                AverageOrderValue = metrics.AverageOrderValue,
                TotalCustomers = customerAnalytics.TotalCustomers,

                TodayRevenue = metrics.TodayRevenue,
                WeekRevenue = metrics.WeekRevenue,
                MonthRevenue = metrics.MonthRevenue,
                YearRevenue = metrics.YearRevenue,

                // Cancelled orders still matter for the status counts below,
                // so those are intentionally used as-is from the stats.
                PendingOrders = statistics.PendingOrders,
                ConfirmedOrders = statistics.ConfirmedOrders,
                ProductionOrders = statistics.ProductionOrders,
                ShippedOrders = statistics.ShippedOrders,
                DeliveredOrders = statistics.DeliveredOrders,
                CancelledOrders = statistics.CancelledOrders,

                LowStockAlerts = lowStock.Select(l => new LowStockAlertViewModel
                {
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    CurrentStock = l.CurrentStock,
                    ReorderLevel = l.ReorderLevel
                }).ToList(),

                RecentOrders = recentOrders.Select(o => new RecentOrderViewModel
                {
                    OrderId = o.OrderId,
                    CustomerName = o.Customer?.FullName ?? "Unknown",
                    OrderDate = o.OrderDate,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status
                }).ToList(),

                TopProducts = topProducts.Select(p => new TopProductViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Category = p.Category,
                    UnitsSold = p.QuantitySold,
                    Revenue = p.Revenue,
                    Trend = 0
                }).ToList(),

                DailyRevenue = dailyRevenue.Select(d => new DailyRevenueViewModel
                {
                    Date = d.Date,
                    Revenue = d.Revenue,
                    OrderCount = d.OrderCount
                }).ToList()
            };

            return View(model);
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
