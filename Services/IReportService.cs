using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartGear_Online.Services
{
    /// <summary>
    /// QUESTION 5 & 11: Report Service Interface
    /// Handles sales reporting, analytics, and data aggregation
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Generate sales report for date range
        /// </summary>
        Task<SalesReport> GenerateSalesReportAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get top selling products
        /// </summary>
        Task<List<TopProduct>> GetTopProductsAsync(int count, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get revenue by day for chart display
        /// </summary>
        Task<List<DailyRevenue>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get revenue by category
        /// </summary>
        Task<List<CategoryRevenue>> GetRevenueByCategoryAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get customer analytics
        /// </summary>
        Task<CustomerAnalytics> GetCustomerAnalyticsAsync();

        /// <summary>
        /// Get inventory status report
        /// </summary>
        Task<InventoryReport> GetInventoryReportAsync();

        /// <summary>
        /// Get low stock alerts
        /// </summary>
        Task<List<LowStockAlert>> GetLowStockAlertsAsync(int threshold = 10);

        /// <summary>
        /// Get order statistics (pending, shipped, delivered counts)
        /// </summary>
        Task<OrderStatistics> GetOrderStatisticsAsync();

        /// <summary>
        /// Export report as CSV
        /// </summary>
        Task<byte[]> ExportReportToCsvAsync(SalesReport report);
    }

    // ================================================
    // Report DTOs
    // ================================================

    public class SalesReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }

        // Summary metrics
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal HighestOrderValue { get; set; }

        // Comparison metrics (vs previous period)
        public decimal RevenueChange { get; set; }
        public int OrdersChange { get; set; }

        // Breakdown
        public List<DailyRevenue> DailyBreakdown { get; set; } = new List<DailyRevenue>();
        public List<CategoryRevenue> CategoryBreakdown { get; set; } = new List<CategoryRevenue>();
        public List<TopProduct> TopProducts { get; set; } = new List<TopProduct>();
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public int ItemsSold { get; set; }
    }

    public class CategoryRevenue
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int ItemsSold { get; set; }
        public int OrderCount { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    public class TopProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal AveragePrice { get; set; }
    }

    public class CustomerAnalytics
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersLast30Days { get; set; }
        public int ActiveCustomersLast30Days { get; set; }
        public int ReturningCustomers { get; set; }
        public decimal AverageCustomerLifetimeValue { get; set; }
        public int CustomersWithOrders { get; set; }
    }

    public class InventoryReport
    {
        public int TotalProducts { get; set; }
        public int TotalItemsInStock { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public List<LowStockAlert> LowStockItems { get; set; } = new List<LowStockAlert>();
    }

    public class LowStockAlert
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public int NeededQuantity { get; set; }
        public DateTime LastRestockedDate { get; set; }
    }

    public class OrderStatistics
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int ProductionOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        // Today's metrics
        public int OrdersToday { get; set; }
        public decimal RevenueToday { get; set; }
    }
}