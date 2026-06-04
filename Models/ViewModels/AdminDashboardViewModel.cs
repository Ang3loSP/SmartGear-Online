using System;
using System.Collections.Generic;

namespace SmartGear_Online.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Admin Dashboard - displays sales analytics and metrics
    /// Question 4 & 12: Admin dashboard data aggregation
    /// </summary>
    public class AdminDashboardViewModel
    {
        // Key Metrics
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageOrderValue { get; set; }

        // Revenue Metrics
        public decimal TodayRevenue { get; set; }
        public decimal WeekRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }

        // Percentage Changes
        public decimal RevenueChangePercent { get; set; }
        public decimal OrdersChangePercent { get; set; }
        public decimal CustomersChangePercent { get; set; }

        // Order Status Breakdown
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int ProductionOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        // Top Products
        public List<TopProductViewModel> TopProducts { get; set; } = new();

        // Low Stock Alerts
        public List<LowStockAlertViewModel> LowStockAlerts { get; set; } = new();

        // Recent Orders
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();

        // Chart Data
        public List<DailyRevenueViewModel> DailyRevenue { get; set; } = new();

        // Performance Metrics
        public decimal ConversionRate { get; set; }
        public decimal CustomerRetentionRate { get; set; }
        public decimal CustomerSatisfactionScore { get; set; }
    }

    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Trend { get; set; }
        public string TrendIcon => Trend >= 0 ? "arrow-up" : "arrow-down";
        public string TrendColor => Trend >= 0 ? "success" : "warning";
    }

    public class LowStockAlertViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public string AlertLevel => CurrentStock <= 5 ? "Critical" : (CurrentStock <= ReorderLevel ? "Warning" : "OK");
    }

    public class RecentOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass => Status switch
        {
            "Pending" => "bg-warning",
            "Confirmed" => "bg-info",
            "In Production" => "bg-primary",
            "Shipped" => "bg-success",
            "Delivered" => "bg-secondary",
            _ => "bg-dark"
        };
    }

    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public string DateLabel => Date.ToString("MMM dd");
    }
}