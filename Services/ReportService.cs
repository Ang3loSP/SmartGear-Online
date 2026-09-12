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

                if (_cache.TryGetValue(cacheKey, out SalesReport? cachedReport))
                {
                    _logger.LogInformation("Returning cached sales report");
                    return cachedReport!;
                }

                var orders = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != OrderStatus.Cancelled)
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
                    .Where(o => o.OrderDate >= prevStartDate && o.OrderDate <= prevEndDate && o.Status != OrderStatus.Cancelled)
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
                                          && x.o.Status != OrderStatus.Cancelled);
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

                // Aggregate in the database (GROUP BY) instead of pulling every
                // order + order item into memory and grouping client-side.
                var revenueByDate = await (from o in _context.Orders
                                           where o.OrderDate >= startDate
                                              && o.OrderDate <= endDate
                                              && o.Status != OrderStatus.Cancelled
                                           group o by o.OrderDate.Date into g
                                           select new DailyRevenue
                                           {
                                               Date = g.Key,
                                               Revenue = g.Sum(x => x.TotalPrice),
                                               OrderCount = g.Count(),
                                               ItemsSold = 0
                                           }).ToListAsync();

                var itemsByDate = await (from oi in _context.OrderItems
                                         join o in _context.Orders on oi.OrderId equals o.OrderId
                                         where o.OrderDate >= startDate
                                            && o.OrderDate <= endDate
                                            && o.Status != OrderStatus.Cancelled
                                         group oi by o.OrderDate.Date into g
                                         select new { Date = g.Key, Items = g.Sum(x => x.Quantity) })
                                        .ToDictionaryAsync(x => x.Date, x => x.Items);

                var byDate = revenueByDate.ToDictionary(d => d.Date);

                // Zero-fill every day in the range so charts always show a
                // full, gapless range.
                var allDates = new List<DateTime>();
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    allDates.Add(date);
                }

                return allDates
                    .Select(date => byDate.TryGetValue(date, out var day)
                        ? new DailyRevenue
                        {
                            Date = date,
                            Revenue = day.Revenue,
                            OrderCount = day.OrderCount,
                            ItemsSold = itemsByDate.TryGetValue(date, out var items) ? items : 0
                        }
                        : new DailyRevenue
                        {
                            Date = date,
                            Revenue = 0,
                            OrderCount = 0,
                            ItemsSold = 0
                        })
                    .ToList();
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
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => o.TotalPrice);

                var categoryRevenue = await (from oi in _context.OrderItems
                                             join o in _context.Orders on oi.OrderId equals o.OrderId
                                             join p in _context.Products on oi.ProductId equals p.ProductId
                                             where o.OrderDate >= startDate
                                                && o.OrderDate <= endDate
                                                && o.Status != OrderStatus.Cancelled
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

                if (_cache.TryGetValue(cacheKey, out CustomerAnalytics? cachedAnalytics))
                {
                    return cachedAnalytics!;
                }

                var customers = await _context.Users.ToListAsync();
                var orders = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled)
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

                if (_cache.TryGetValue(cacheKey, out OrderStatistics? cachedStats))
                {
                    return cachedStats!;
                }

                var orders = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .ToListAsync();

                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                var stats = new OrderStatistics
                {
                    TotalOrders = orders.Count,
                    PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
                    ConfirmedOrders = orders.Count(o => o.Status == OrderStatus.Confirmed),
                    ProductionOrders = orders.Count(o => o.Status == OrderStatus.InProduction),
                    ShippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped),
                    DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
                    CancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled),
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
        // Get Revenue Metrics (single aggregate query)
        // ================================================
        public async Task<RevenueMetrics> GetRevenueMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Getting revenue metrics");

                const string cacheKey = "RevenueMetrics";
                if (_cache.TryGetValue(cacheKey, out RevenueMetrics? cachedMetrics))
                    return cachedMetrics!;

                // Cancelled orders generate no revenue, so every figure below
                // filters them out — matching the dashboard's existing rule.
                var now = DateTime.UtcNow;
                var today = now.Date;
                var weekStart = now.AddDays(-7);
                var monthStart = now.AddDays(-30);
                var yearStart = now.AddYears(-1);

                var row = await (from o in _context.Orders
                                 where o.Status != OrderStatus.Cancelled
                                 group o by 1 into g
                                 select new RevenueMetrics
                                 {
                                     TotalRevenue = g.Sum(x => x.TotalPrice),
                                     TotalOrders = g.Count(),
                                     TodayRevenue = g.Sum(x => x.OrderDate >= today ? x.TotalPrice : 0m),
                                     WeekRevenue = g.Sum(x => x.OrderDate >= weekStart ? x.TotalPrice : 0m),
                                     MonthRevenue = g.Sum(x => x.OrderDate >= monthStart ? x.TotalPrice : 0m),
                                     YearRevenue = g.Sum(x => x.OrderDate >= yearStart ? x.TotalPrice : 0m)
                                 }).FirstOrDefaultAsync();

                var metrics = row ?? new RevenueMetrics();
                metrics.AverageOrderValue = metrics.TotalOrders > 0
                    ? metrics.TotalRevenue / metrics.TotalOrders
                    : 0m;

                _cache.Set(cacheKey, metrics, TimeSpan.FromMinutes(5));
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue metrics");
                return new RevenueMetrics();
            }
        }

        // ================================================
        // Get Low Stock Alerts (per-product reorder level)
        // ================================================
        public async Task<List<LowStockAlert>> GetLowStockByReorderLevelAsync()
        {
            try
            {
                _logger.LogInformation("Getting low stock alerts by reorder level");

                // Live (uncached) — matches the admin dashboard's definition of
                // "at or below that product's own reorder level", computed with
                // a projection so the whole Products table is never loaded.
                return await _context.Products
                    .Where(p => p.IsActive && p.QuantityInStock <= p.ReorderLevel)
                    .OrderBy(p => p.QuantityInStock)
                    .Select(p => new LowStockAlert
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName ?? string.Empty,
                        Category = p.Category ?? string.Empty,
                        CurrentStock = p.QuantityInStock,
                        ReorderLevel = p.ReorderLevel,
                        NeededQuantity = p.ReorderLevel - p.QuantityInStock > 0 ? p.ReorderLevel - p.QuantityInStock : 0,
                        LastRestockedDate = p.UpdatedDate
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock alerts by reorder level");
                return new List<LowStockAlert>();
            }
        }

        // ================================================
        // Export Report to CSV
        // ================================================
        public Task<byte[]> ExportReportToCsvAsync(SalesReport report)
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
                sb.AppendLine(JoinCsvRow("Total Revenue", $"R{report.TotalRevenue:F2}"));
                sb.AppendLine(JoinCsvRow("Total Orders", report.TotalOrders.ToString()));
                sb.AppendLine(JoinCsvRow("Total Items Sold", report.TotalItemsSold.ToString()));
                sb.AppendLine(JoinCsvRow("Average Order Value", $"R{report.AverageOrderValue:F2}"));
                sb.AppendLine(JoinCsvRow("Highest Order Value", $"R{report.HighestOrderValue:F2}"));
                sb.AppendLine(JoinCsvRow("Revenue Change (vs previous)", $"{report.RevenueChange:F1}%"));
                sb.AppendLine(JoinCsvRow("Orders Change (vs previous)", $"{report.OrdersChange}%"));
                sb.AppendLine();

                sb.AppendLine("Daily Breakdown");
                sb.AppendLine(JoinCsvRow("Date", "Revenue", "Orders", "Items Sold"));
                foreach (var day in report.DailyBreakdown)
                {
                    sb.AppendLine(JoinCsvRow(
                        day.Date.ToString("yyyy-MM-dd"),
                        day.Revenue.ToString("F2"),
                        day.OrderCount.ToString(),
                        day.ItemsSold.ToString()));
                }
                sb.AppendLine();

                sb.AppendLine("Category Breakdown");
                sb.AppendLine(JoinCsvRow("Category", "Revenue", "Items Sold", "Orders", "% of Total"));
                foreach (var cat in report.CategoryBreakdown)
                {
                    sb.AppendLine(JoinCsvRow(
                        cat.CategoryName,
                        cat.Revenue.ToString("F2"),
                        cat.ItemsSold.ToString(),
                        cat.OrderCount.ToString(),
                        $"{cat.PercentageOfTotal:F1}%"));
                }
                sb.AppendLine();

                sb.AppendLine("Top Products");
                sb.AppendLine(JoinCsvRow("Product ID", "Product Name", "Category", "Quantity Sold", "Revenue", "Average Price"));
                foreach (var product in report.TopProducts)
                {
                    sb.AppendLine(JoinCsvRow(
                        product.ProductId.ToString(),
                        product.ProductName,
                        product.Category,
                        product.QuantitySold.ToString(),
                        product.Revenue.ToString("F2"),
                        product.AveragePrice.ToString("F2")));
                }

                return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report to CSV");
                throw;
            }
        }

        // ================================================
        // CSV Cell Escaping
        // ================================================
        /// <summary>
        /// Escapes a single CSV cell: RFC-4180 quoting (double internal quotes,
        /// wrap in quotes when the cell contains a delimiter/quote/newline) and
        /// formula-injection protection — cells starting with = + - @ or a tab
        /// get a leading apostrophe so they are never interpreted as an Excel
        /// formula open.
        /// </summary>
        private static string EscapeCsvCell(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value[0] == '=' || value[0] == '+' || value[0] == '-' ||
                value[0] == '@' || value[0] == '\t' || value[0] == '\r')
            {
                value = "'" + value;
            }

            if (value.IndexOf(',') >= 0 ||
                value.IndexOf('"') >= 0 ||
                value.IndexOf('\n') >= 0 ||
                value.IndexOf('\r') >= 0)
            {
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        /// <summary>
        /// Joins cells into a properly escaped CSV row.
        /// </summary>
        private static string JoinCsvRow(params string?[] cells)
            => string.Join(",", cells.Select(EscapeCsvCell));
    }
}