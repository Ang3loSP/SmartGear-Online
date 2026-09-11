using SmartGear_Online.Models;
using SmartGear_Online.Models.ViewModels;
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
        /// Process order payment (simulated)
        /// </summary>
        Task<PaymentResult> ProcessPaymentAsync(Order order, PaymentInfo paymentInfo);

        /// <summary>
        /// Apply discount code to order
        /// </summary>
        DiscountResult ApplyDiscount(string discountCode, decimal subtotal);

        /// <summary>
        /// Places an order from the session cart in a single transaction:
        /// re-validates products/stock against the database, computes totals and
        /// unit prices server-side (never trusting posted totals), runs the
        /// simulated payment, atomically decrements stock, and clears nothing
        /// until commit succeeds.
        /// </summary>
        Task<int> PlaceOrderAsync(string customerId, ShoppingCart cart, CheckoutViewModel model);

        /// <summary>
        /// Applies an order status change with a transition guard (no backwards
        /// moves, no reviving cancelled/delivered orders). When the new status is
        /// Cancelled, inventory is restored inside the same transaction.
        /// </summary>
        Task<OrderStatusUpdateResult> UpdateStatusAsync(int orderId, OrderStatus newStatus, string actorUserId);
    }

    /// <summary>
    /// DTO for the result of an order status update.
    /// </summary>
    public class OrderStatusUpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
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
        public string Currency { get; set; } = "ZAR";
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
    /// DTO for payment information.
    /// SECURITY: only the last four digits of the card are ever collected —
    /// the full PAN and CVV are never bound, stored or logged.
    /// </summary>
    public class PaymentInfo
    {
        public string CardNumberLast4 { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "CreditCard";
    }
}