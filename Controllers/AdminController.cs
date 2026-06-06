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
        // Fixed: Now builds & passes AdminDashboardViewModel
        // ================================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var allOrders = await _orderRepository.GetAllOrdersAsync();
            var allProducts = await _productRepository.GetProductsAsync(1, 1000);

            var model = new AdminDashboardViewModel
            {
                TotalOrders = allOrders.Count(),
                TotalRevenue = allOrders.Sum(o => o.TotalPrice),
                AverageOrderValue = allOrders.Any()
                    ? allOrders.Average(o => o.TotalPrice)
                    : 0m,

                TodayRevenue = allOrders
                    .Where(o => o.OrderDate.Date == DateTime.UtcNow.Date)
                    .Sum(o => o.TotalPrice),
                WeekRevenue = allOrders
                    .Where(o => o.OrderDate >= DateTime.UtcNow.AddDays(-7))
                    .Sum(o => o.TotalPrice),
                MonthRevenue = allOrders
                    .Where(o => o.OrderDate >= DateTime.UtcNow.AddDays(-30))
                    .Sum(o => o.TotalPrice),

                PendingOrders = allOrders.Count(o => o.Status == "Pending"),
                ConfirmedOrders = allOrders.Count(o => o.Status == "Confirmed"),
                ProductionOrders = allOrders.Count(o => o.Status == "In Production"),
                ShippedOrders = allOrders.Count(o => o.Status == "Shipped"),
                DeliveredOrders = allOrders.Count(o => o.Status == "Delivered"),
                CancelledOrders = allOrders.Count(o => o.Status == "Cancelled"),

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
                    }).ToList()
            };

            return View(model);
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
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                await _productRepository.AddProductAsync(product);
                return RedirectToAction("Inventory");
            }
            return View(product);
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