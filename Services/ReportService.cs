using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Data;
using SmartGear_Online.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGear_Online.Services
{
    /// <summary>
    /// QUESTION 5 & 11: Report Service Implementation
    /// Provides analytics and reporting functionality with caching
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<ReportService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // ================================================
        // Generate Comprehensive Sales Report
        // ================================================
        public async Task<SalesReport> GenerateSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Generating sales report from {StartDate} to {EndDate}", startDate, endDate);

                var cacheKey = $"SalesReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

                if (_cache.TryGetValue(cacheKey, out SalesReport cachedReport))
                {
                    _logger.LogInformation("Returning cached sales report");
                    return cachedReport;
                }

                var orders = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != "Cancelled")
                    .Include(o => o.OrderItems)
                    .ToListAsync();

                var totalRevenue = orders.Sum(o => o.TotalPrice);
                var totalOrders = orders.Count;
                var totalItemsSold = orders.Sum(o => o.OrderItems.Sum(oi => oi.Quantity));
                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
                var highestOrderValue = orders.Any() ? orders.Max(o => o.TotalPrice) : 0;

                var periodLength = (endDate - startDate).Days;
                var prevStartDate = startDate.AddDays(-periodLength);
                var prevEndDate = startDate.AddDays(-1);

                var prevOrders = await _context.Orders
                    .Where(o => o.OrderDate >= prevStartDate && o.OrderDate <= prevEndDate && o.Status != "Cancelled")
                    .ToListAsync();

                var prevRevenue = prevOrders.Sum(o => o.TotalPrice);
                var prevOrderCount = prevOrders.Count;

                var revenueChange = prevRevenue > 0 ? ((totalRevenue - prevRevenue) / prevRevenue) * 100 : 0;
                var ordersChange = prevOrderCount > 0 ? ((totalOrders - prevOrderCount) / (decimal)prevOrderCount) * 100 : 0;

                var dailyBreakdown = await GetDailyRevenueAsync(startDate, endDate);
                var categoryBreakdown = await GetRevenueByCategoryAsync(startDate, endDate);
                var topProducts = await GetTopProductsAsync(10, startDate, endDate);

                var report = new SalesReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow,
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    TotalItemsSold = totalItemsSold,
                    AverageOrderValue = averageOrderValue,
                    HighestOrderValue = highestOrderValue,
                    RevenueChange = revenueChange,
                    OrdersChange = (int)ordersChange,
                    DailyBreakdown = dailyBreakdown,
                    CategoryBreakdown = categoryBreakdown,
                    TopProducts = topProducts
                };

                _cache.Set(cacheKey, report, TimeSpan.FromHours(1));
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report");
                throw;
            }
        }

        // ================================================
        // Get Top Selling Products (FIXED)
        // ================================================
        public async Task<List<TopProduct>> GetTopProductsAsync(int count, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                _logger.LogInformation("Getting top {Count} products", count);

                var query = from oi in _context.OrderItems
                            join o in _context.Orders on oi.OrderId equals o.OrderId
                            join p in _context.Products on oi.ProductId equals p.ProductId
                            select new { oi, o, p };

                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.Where(x => x.o.OrderDate >= startDate.Value
                                          && x.o.OrderDate <= endDate.Value
                                          && x.o.Status != "Cancelled");
                }

                var topProducts = await query
                    .GroupBy(x => new { x.p.ProductId, x.p.ProductName, x.p.Category })
                    .Select(g => new TopProduct
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName ?? string.Empty,
                        Category = g.Key.Category ?? string.Empty,
                        QuantitySold = g.Sum(x => x.oi.Quantity),
                        Revenue = g.Sum(x => x.oi.Quantity * x.oi.UnitPrice),
                        AveragePrice = g.Average(x => x.oi.UnitPrice)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(count)
                    .ToListAsync();

                return topProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top products");
                throw;
            }
        }

        // ================================================
        // Get Daily Revenue for Chart
        // ================================================
        public async Task<List<DailyRevenue>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Getting daily revenue from {StartDate} to {EndDate}", startDate, endDate);

                var orders = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != "Cancelled")
                    .Include(o => o.OrderItems)
                    .ToListAsync();

                var dailyRevenue = orders
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new DailyRevenue
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TotalPrice),
                        OrderCount = g.Count(),
                        ItemsSold = g.Sum(o => o.OrderItems.Sum(oi => oi.Quantity))
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                var allDates = new List<DateTime>();
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    allDates.Add(date);
                }

                var completeDailyRevenue = allDates
                    .Select(date => dailyRevenue.FirstOrDefault(d => d.Date == date) ?? new DailyRevenue
                    {
                        Date = date,
                        Revenue = 0,
                        OrderCount = 0,
                        ItemsSold = 0
                    })
                    .ToList();

                return completeDailyRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily revenue");
                throw;
            }
        }

        // ================================================
        // Get Revenue by Category (FIXED)
        // ================================================
        public async Task<List<CategoryRevenue>> GetRevenueByCategoryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Getting revenue by category from {StartDate} to {EndDate}", startDate, endDate);

                var totalRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != "Cancelled")
                    .SumAsync(o => o.TotalPrice);

                var categoryRevenue = await (from oi in _context.OrderItems
                                             join o in _context.Orders on oi.OrderId equals o.OrderId
                                             join p in _context.Products on oi.ProductId equals p.ProductId
                                             where o.OrderDate >= startDate
                                                && o.OrderDate <= endDate
                                                && o.Status != "Cancelled"
                                             group oi by p.Category into g
                                             select new CategoryRevenue
                                             {
                                                 CategoryName = g.Key ?? "Uncategorized",
                                                 Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                                                 ItemsSold = g.Sum(oi => oi.Quantity),
                                                 OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                                                 PercentageOfTotal = 0
                                             }).ToListAsync();

                foreach (var category in categoryRevenue)
                {
                    category.PercentageOfTotal = totalRevenue > 0 ? (category.Revenue / totalRevenue) * 100 : 0;
                }

                return categoryRevenue.OrderByDescending(c => c.Revenue).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue by category");
                throw;
            }
        }

        // ================================================
        // Get Customer Analytics
        // ================================================
        public async Task<CustomerAnalytics> GetCustomerAnalyticsAsync()
        {
            try
            {
                _logger.LogInformation("Getting customer analytics");

                var cacheKey = "CustomerAnalytics";

                if (_cache.TryGetValue(cacheKey, out CustomerAnalytics cachedAnalytics))
                {
                    return cachedAnalytics;
                }

                var customers = await _context.Users.ToListAsync();
                var orders = await _context.Orders
                    .Where(o => o.Status != "Cancelled")
                    .ToListAsync();

                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

                var analytics = new CustomerAnalytics
                {
                    TotalCustomers = customers.Count,
                    NewCustomersLast30Days = customers.Count(u => u.DateRegistered >= thirtyDaysAgo),
                    CustomersWithOrders = orders.Select(o => o.CustomerId).Distinct().Count(),
                    ReturningCustomers = orders
                        .GroupBy(o => o.CustomerId)
                        .Count(g => g.Count() > 1),
                    AverageCustomerLifetimeValue = 0
                };

                var customerOrders = orders
                    .GroupBy(o => o.CustomerId)
                    .Select(g => g.Sum(o => o.TotalPrice));

                analytics.AverageCustomerLifetimeValue = customerOrders.Any() ? customerOrders.Average() : 0;
                analytics.ActiveCustomersLast30Days = orders
                    .Where(o => o.OrderDate >= thirtyDaysAgo)
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .Count();

                _cache.Set(cacheKey, analytics, TimeSpan.FromHours(1));
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer analytics");
                return new CustomerAnalytics();
            }
        }

        // ================================================
        // Get Inventory Status Report
        // ================================================
        public async Task<InventoryReport> GetInventoryReportAsync()
        {
            try
            {
                _logger.LogInformation("Getting inventory report");

                var products = await _context.Products
                    .Where(p => p.IsActive)
                    .ToListAsync();

                var lowStockItems = await GetLowStockAlertsAsync(10);

                var report = new InventoryReport
                {
                    TotalProducts = products.Count,
                    TotalItemsInStock = products.Sum(p => p.QuantityInStock),
                    LowStockCount = products.Count(p => p.QuantityInStock <= p.ReorderLevel && p.QuantityInStock > 0),
                    OutOfStockCount = products.Count(p => p.QuantityInStock <= 0),
                    TotalInventoryValue = products.Sum(p => p.Price * p.QuantityInStock),
                    LowStockItems = lowStockItems
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory report");
                throw;
            }
        }

        // ================================================
        // Get Low Stock Alerts
        // ================================================
        public async Task<List<LowStockAlert>> GetLowStockAlertsAsync(int threshold = 10)
        {
            try
            {
                _logger.LogInformation("Getting low stock alerts with threshold {Threshold}", threshold);

                var products = await _context.Products
                    .Where(p => p.IsActive && p.QuantityInStock <= threshold)
                    .OrderBy(p => p.QuantityInStock)
                    .ToListAsync();

                var alerts = products.Select(p => new LowStockAlert
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName ?? string.Empty,
                    Category = p.Category ?? string.Empty,
                    CurrentStock = p.QuantityInStock,
                    ReorderLevel = p.ReorderLevel,
                    NeededQuantity = p.ReorderLevel - p.QuantityInStock > 0 ? p.ReorderLevel - p.QuantityInStock : 0,
                    LastRestockedDate = p.UpdatedDate
                }).ToList();

                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock alerts");
                return new List<LowStockAlert>();
            }
        }

        // ================================================
        // Get Order Statistics
        // ================================================
        public async Task<OrderStatistics> GetOrderStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("Getting order statistics");

                var cacheKey = "OrderStatistics";

                if (_cache.TryGetValue(cacheKey, out OrderStatistics cachedStats))
                {
                    return cachedStats;
                }

                var orders = await _context.Orders
                    .Where(o => o.Status != "Cancelled")
                    .ToListAsync();

                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                var stats = new OrderStatistics
                {
                    TotalOrders = orders.Count,
                    PendingOrders = orders.Count(o => o.Status == "Pending"),
                    ConfirmedOrders = orders.Count(o => o.Status == "Confirmed"),
                    ProductionOrders = orders.Count(o => o.Status == "In Production"),
                    ShippedOrders = orders.Count(o => o.Status == "Shipped"),
                    DeliveredOrders = orders.Count(o => o.Status == "Delivered"),
                    CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled"),
                    OrdersToday = orders.Count(o => o.OrderDate >= today && o.OrderDate < tomorrow),
                    RevenueToday = orders
                        .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow)
                        .Sum(o => o.TotalPrice)
                };

                _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(5));
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order statistics");
                return new OrderStatistics();
            }
        }

        // ================================================
        // Export Report to CSV
        // ================================================
        public async Task<byte[]> ExportReportToCsvAsync(SalesReport report)
        {
            try
            {
                _logger.LogInformation("Exporting sales report to CSV");

                var sb = new StringBuilder();

                sb.AppendLine("Sales Report");
                sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Period: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
                sb.AppendLine();

                sb.AppendLine("Summary Metrics");
                sb.AppendLine($"Total Revenue,${report.TotalRevenue:F2}");
                sb.AppendLine($"Total Orders,{report.TotalOrders}");
                sb.AppendLine($"Total Items Sold,{report.TotalItemsSold}");
                sb.AppendLine($"Average Order Value,${report.AverageOrderValue:F2}");
                sb.AppendLine($"Highest Order Value,${report.HighestOrderValue:F2}");
                sb.AppendLine($"Revenue Change (vs previous),{report.RevenueChange:F1}%");
                sb.AppendLine($"Orders Change (vs previous),{report.OrdersChange}%");
                sb.AppendLine();

                sb.AppendLine("Daily Breakdown");
                sb.AppendLine("Date,Revenue,Orders,Items Sold");
                foreach (var day in report.DailyBreakdown)
                {
                    sb.AppendLine($"{day.Date:yyyy-MM-dd},{day.Revenue:F2},{day.OrderCount},{day.ItemsSold}");
                }
                sb.AppendLine();

                sb.AppendLine("Category Breakdown");
                sb.AppendLine("Category,Revenue,Items Sold,Orders,% of Total");
                foreach (var cat in report.CategoryBreakdown)
                {
                    sb.AppendLine($"{cat.CategoryName},{cat.Revenue:F2},{cat.ItemsSold},{cat.OrderCount},{cat.PercentageOfTotal:F1}%");
                }
                sb.AppendLine();

                sb.AppendLine("Top Products");
                sb.AppendLine("Product ID,Product Name,Category,Quantity Sold,Revenue,Average Price");
                foreach (var product in report.TopProducts)
                {
                    sb.AppendLine($"{product.ProductId},{product.ProductName},{product.Category},{product.QuantitySold},{product.Revenue:F2},{product.AveragePrice:F2}");
                }

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report to CSV");
                throw;
            }
        }
    }
}