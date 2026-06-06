using SmartGear_Online.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartGear_Online.Services
{
    /// <summary>
    /// QUESTION 2 & 5: Order Service Interface
    /// Contains business logic for order processing, calculations, and validation
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Calculate order totals (subtotal, tax, shipping, grand total)
        /// </summary>
        Task<OrderTotals> CalculateOrderTotalsAsync(List<CartItem> cartItems, string shippingMethod, string? discountCode = null);

        /// <summary>
        /// Validate order before submission
        /// </summary>
        Task<OrderValidationResult> ValidateOrderAsync(Order order, List<CartItem> cartItems);

        /// <summary>
        /// Process order payment (simulated)
        /// </summary>
        Task<PaymentResult> ProcessPaymentAsync(Order order, PaymentInfo paymentInfo);

        /// <summary>
        /// Apply discount code to order
        /// </summary>
        Task<DiscountResult> ApplyDiscountAsync(string discountCode, decimal subtotal);

        /// <summary>
        /// Get order status history with timestamps
        /// </summary>
        Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId);

        /// <summary>
        /// Generate order invoice as PDF (simulated)
        /// </summary>
        Task<byte[]> GenerateInvoicePdfAsync(int orderId);

        /// <summary>
        /// Check if product customization is valid
        /// </summary>
        Task<bool> ValidateCustomizationAsync(Customization customization);
    }

    /// <summary>
    /// DTO for order totals calculation
    /// </summary>
    public class OrderTotals
    {
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TaxRate { get; set; } = 0.08m;
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public bool IsFreeShipping { get; set; }
        public string Currency { get; set; } = "USD";
    }

    /// <summary>
    /// DTO for order validation result
    /// </summary>
    public class OrderValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO for payment result
    /// </summary>
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
    }

    /// <summary>
    /// DTO for discount result
    /// </summary>
    public class DiscountResult
    {
        public bool IsValid { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for order status history
    /// </summary>
    public class OrderStatusHistory
    {
        public int HistoryId { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusDisplayName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for payment information
    /// </summary>
    public class PaymentInfo
    {
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "CreditCard";
    }
}