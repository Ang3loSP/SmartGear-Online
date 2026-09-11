using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartGear_Online.Data;
using SmartGear_Online.Models;
using SmartGear_Online.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Services
{
    /// <summary>
    /// QUESTION 2 & 5: Order Service Implementation
    /// Contains core business logic for order processing
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly OrderSettings _orderSettings;
        private readonly Dictionary<string, DiscountCode> _validDiscounts;

        public OrderService(
            ILogger<OrderService> logger,
            IOptions<OrderSettings> orderSettings,
            IOptions<DiscountSettings> discountSettings,
            ApplicationDbContext context)
        {
            _logger = logger;
            _orderSettings = orderSettings?.Value ?? new OrderSettings();
            _context = context;

            // Discount codes are loaded from configuration (see the
            // "Discounts" section in appsettings.json) instead of being
            // hard-coded here, so codes can change without a redeploy.
            _validDiscounts = (discountSettings?.Value?.Codes ?? new List<DiscountCode>())
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Code))
                .ToDictionary(c => c.Code!, StringComparer.OrdinalIgnoreCase);
        }

        // ================================================
        // QUESTION 5: Calculate Order Totals with Business Logic
        // Single source of truth for totals — used by the checkout GET, the
        // review summary and the actual order placement so they can never
        // disagree. Subtotal uses the CURRENT database price of each product,
        // never the session-snapshot price from the cart.
        // ================================================
        public async Task<OrderTotals> CalculateOrderTotalsAsync(
            List<CartItem> cartItems,
            string shippingMethod,
            string? discountCode = null)
        {
            try
            {
                _logger.LogInformation("Calculating order totals for {ItemCount} items", cartItems?.Count ?? 0);

                if (cartItems == null || !cartItems.Any())
                {
                    return new OrderTotals { Subtotal = 0, GrandTotal = 0 };
                }

                // Current DB prices, not the (possibly stale) cart snapshot.
                var productIds = cartItems.Select(i => i.ProductId).Distinct().ToList();
                var prices = await _context.Products.AsNoTracking()
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId, p => p.Price);

                decimal subtotal = 0;
                foreach (var item in cartItems)
                {
                    if (prices.TryGetValue(item.ProductId, out var price))
                        subtotal += item.Quantity * price;
                }

                // Calculate tax (configurable rate)
                var taxRate = _orderSettings.TaxRate ?? 0.08m;
                var taxAmount = subtotal * taxRate;

                // Shipping rule (matches the checkout UI): Express is a flat
                // R15.00; Standard is free over the threshold, otherwise R5.99;
                // the FREESHIP code makes shipping free regardless.
                var freeShippingThreshold = _orderSettings.FreeShippingThreshold ?? 50;
                var isExpress = string.Equals(shippingMethod, "Express", StringComparison.OrdinalIgnoreCase);
                var isFreeShipping = !isExpress && subtotal >= freeShippingThreshold;

                decimal discountAmount = 0;
                var freeShippingDiscount = false;

                // Apply discount if provided
                if (!string.IsNullOrEmpty(discountCode))
                {
                    var discountResult = ApplyDiscount(discountCode, subtotal);
                    if (discountResult.IsValid)
                    {
                        discountAmount = discountResult.DiscountAmount;

                        // If discount is free shipping, override shipping cost
                        if (discountResult.DiscountType == "FreeShipping")
                        {
                            freeShippingDiscount = true;
                            isFreeShipping = true;
                        }
                    }
                }

                var shippingCost = freeShippingDiscount || isFreeShipping
                    ? 0m
                    : (isExpress ? 15.00m : 5.99m);

                var grandTotal = subtotal + taxAmount + shippingCost - discountAmount;

                _logger.LogInformation("Order totals calculated: Subtotal={Subtotal}, Tax={Tax}, Shipping={Shipping}, Discount={Discount}, GrandTotal={GrandTotal}",
                    subtotal, taxAmount, shippingCost, discountAmount, grandTotal);

                return new OrderTotals
                {
                    Subtotal = subtotal,
                    TaxAmount = taxAmount,
                    TaxRate = taxRate,
                    ShippingCost = shippingCost,
                    DiscountAmount = discountAmount,
                    GrandTotal = grandTotal,
                    IsFreeShipping = isFreeShipping,
                    Currency = "ZAR"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order totals");
                throw;
            }
        }

        // ================================================
        // QUESTION 5: Process Payment (Simulated)
        // ================================================
        public async Task<PaymentResult> ProcessPaymentAsync(Order order, PaymentInfo paymentInfo)
        {
            try
            {
                _logger.LogInformation("Processing payment for order {OrderId}, Amount: R{Amount}",
                    order.OrderId, order.TotalPrice);

                // Simulate payment processing delay
                await Task.Delay(500);

                // In production, integrate with Stripe/PayPal/other payment gateway
                // For demo purposes, we'll accept any valid card number format

                if (string.IsNullOrWhiteSpace(paymentInfo.CardNumberLast4) || paymentInfo.CardNumberLast4.Length != 4)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Invalid card number. Please check and try again."
                    };
                }

                if (string.IsNullOrWhiteSpace(paymentInfo.ExpiryMonth) || string.IsNullOrWhiteSpace(paymentInfo.ExpiryYear))
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Invalid card expiry date."
                    };
                }

                // Simulate success
                var transactionId = $"TXN_{DateTime.Now.Ticks}_{order.OrderId}";

                _logger.LogInformation("Payment processed successfully for order {OrderId}. Transaction ID: {TransactionId}",
                    order.OrderId, transactionId);

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = transactionId,
                    TransactionDate = DateTime.UtcNow,
                    Message = "Payment processed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed for order {OrderId}", order.OrderId);
                return new PaymentResult
                {
                    Success = false,
                    Message = "Payment processing failed. Please try again or contact your bank."
                };
            }
        }

        // ================================================
        // QUESTION 5: Apply Discount Code Business Logic
        // Pure in-memory computation against a fixed discount set —
        // synchronous by design (there is no async work to await).
        // ================================================
        public DiscountResult ApplyDiscount(string discountCode, decimal subtotal)
        {
            try
            {
                _logger.LogInformation("Applying discount code: {DiscountCode} to subtotal: {Subtotal}", discountCode, subtotal);

                if (string.IsNullOrWhiteSpace(discountCode))
                {
                    return new DiscountResult { IsValid = false, Message = "No discount code provided" };
                }

                if (discountCode.Length > 32)
                {
                    return new DiscountResult { IsValid = false, Message = "Discount code is too long" };
                }

                var code = discountCode.ToUpperInvariant().Trim();

                if (!_validDiscounts.TryGetValue(code, out var discount) || !discount.IsActive)
                {
                    _logger.LogWarning("Invalid or inactive discount code: {DiscountCode}", code);
                    return new DiscountResult { IsValid = false, Message = "Invalid or expired discount code" };
                }

                // A past expiry date makes the code invalid even when active.
                if (discount.ExpiryDate.HasValue && discount.ExpiryDate.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("Expired discount code: {DiscountCode}", code);
                    return new DiscountResult { IsValid = false, Message = "Invalid or expired discount code" };
                }

                decimal discountAmount = 0;

                switch (discount.Type)
                {
                    case "Percentage":
                        discountAmount = subtotal * (discount.Value / 100);
                        if (discountAmount > 500) // Max discount limit
                        {
                            discountAmount = 500;
                        }
                        break;
                    case "FixedAmount":
                        discountAmount = discount.Value;
                        if (discountAmount > subtotal)
                        {
                            discountAmount = subtotal;
                        }
                        break;
                    case "FreeShipping":
                        discountAmount = 0;
                        break;
                    default:
                        return new DiscountResult { IsValid = false, Message = "Invalid discount type" };
                }

                _logger.LogInformation("Discount applied: {DiscountCode} gave R{DiscountAmount} off", code, discountAmount);

                return new DiscountResult
                {
                    IsValid = true,
                    DiscountAmount = discountAmount,
                    DiscountType = discount.Type,
                    Message = $"Discount of R{discountAmount:F2} applied successfully!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying discount code {DiscountCode}", discountCode);
                return new DiscountResult { IsValid = false, Message = "Error applying discount. Please try again." };
            }
        }

        // ================================================
        // Place Order (transactional, server-authoritative)
        // ================================================
        public async Task<int> PlaceOrderAsync(string customerId, ShoppingCart cart, CheckoutViewModel model)
        {
            if (cart == null || !cart.HasItems())
                throw new InvalidOperationException("Your cart is empty.");

            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // 1) Validate against the live database first (friendly errors,
            //    no DB writes yet).
            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            foreach (var item in cart.Items)
            {
                if (!products.TryGetValue(item.ProductId, out var product) || !product.IsActive)
                    throw new InvalidOperationException($"Product '{item.ProductName}' is no longer available.");

                if (item.Quantity < 1 || item.Quantity > product.QuantityInStock)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{product.ProductName}'. Available: {product.QuantityInStock}");
            }

            // 2) Simulated payment (runs BEFORE any DB writes so a rejected
            //    card never creates an order row).
            if (!ParseExpiry(model.ExpiryDate, out var expiryMonth, out var expiryYear))
                throw new InvalidOperationException("Invalid card expiry date.");

            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                ShippingAddress = model.GetShippingAddress(),
                BillingAddress = model.GetBillingAddress(),
                ShippingMethod = model.ShippingMethod
            };

            var totals = await CalculateOrderTotalsAsync(cart.Items, model.ShippingMethod, cart.DiscountCode);
            order.TotalPrice = totals.GrandTotal;

            if (!string.IsNullOrWhiteSpace(cart.DiscountCode))
            {
                order.DiscountCode = cart.DiscountCode.Trim().ToUpperInvariant();
                order.DiscountAmount = totals.DiscountAmount;
            }

            order.OrderItems = cart.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = products[item.ProductId].Price
            }).ToList();

            var paymentInfo = new PaymentInfo
            {
                CardNumberLast4 = model.CardNumberLast4,
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                CardHolderName = model.FullName
            };
            var payment = await ProcessPaymentAsync(order, paymentInfo);
            if (!payment.Success)
                throw new InvalidOperationException(payment.Message);

            // 3) Single transaction: order + items + stock decrement.
            //    Nothing persists until Commit succeeds.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart.Items)
            {
                // Atomic conditional decrement — no row read, no TOCTOU.
                var released = await _context.Products
                    .Where(p => p.ProductId == item.ProductId && p.QuantityInStock >= item.Quantity)
                    .ExecuteUpdateAsync(p => p.SetProperty(
                        x => x.QuantityInStock,
                        x => x.QuantityInStock - item.Quantity));

                if (released == 0)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{item.ProductName}'. Please refresh your cart.");

                _logger.LogInformation(
                    "Reduced inventory for product {ProductId} by {Quantity}", item.ProductId, item.Quantity);
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderId} placed by customer {CustomerId}", order.OrderId, customerId);
            return order.OrderId;
        }

        // ================================================
        // Update Status (transition guard + cancel stock restore)
        // ================================================
        public async Task<OrderStatusUpdateResult> UpdateStatusAsync(
            int orderId, OrderStatus newStatus, string actorUserId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    return new OrderStatusUpdateResult { Success = false, Message = "Order not found" };

                var current = order.Status;

                // Idempotent update — accepted silently.
                if (current == newStatus)
                    return new OrderStatusUpdateResult
                    {
                        Success = true,
                        Message = "Order is already " + newStatus.ToDisplayString()
                    };

                // Terminal states cannot be changed.
                if (current == OrderStatus.Delivered)
                    return new OrderStatusUpdateResult
                    {
                        Success = false,
                        Message = "Delivered orders cannot be changed"
                    };

                if (current == OrderStatus.Cancelled)
                    return new OrderStatusUpdateResult
                    {
                        Success = false,
                        Message = "Cancelled orders cannot be changed"
                    };

                // Stock restore only applies to cancelling orders in an
                // cancellable state (Pending / Confirmed).
                if (newStatus == OrderStatus.Cancelled && !order.CanBeCancelled())
                    return new OrderStatusUpdateResult
                    {
                        Success = false,
                        Message = "Order cannot be cancelled at this stage"
                    };

                // Any non-cancel transition must go forward in the rank
                // (no backwards / regression).
                if (newStatus != OrderStatus.Cancelled &&
                    StatusRank(newStatus) <= StatusRank(current))
                {
                    return new OrderStatusUpdateResult
                    {
                        Success = false,
                        Message = "Order status cannot move backwards"
                    };
                }

                // One transaction: status + optional stock restore.
                await using var transaction = await _context.Database.BeginTransactionAsync();

                if (newStatus == OrderStatus.Cancelled)
                {
                    foreach (var item in order.OrderItems)
                    {
                        await _context.Products
                            .Where(p => p.ProductId == item.ProductId)
                            .ExecuteUpdateAsync(p => p.SetProperty(
                                x => x.QuantityInStock,
                                x => x.QuantityInStock + item.Quantity));
                    }
                }

                order.Status = newStatus;
                order.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Order {OrderId} status updated to {NewStatus} by {Actor}",
                    orderId, newStatus, actorUserId);

                return new OrderStatusUpdateResult
                {
                    Success = true,
                    Message = "Order status updated to " + newStatus.ToDisplayString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for order {OrderId}", orderId);
                return new OrderStatusUpdateResult
                {
                    Success = false,
                    Message = "Failed to update order status"
                };
            }
        }

        // ================================================
        // Helpers
        // ================================================
        private static bool ParseExpiry(string? expiry, out string month, out string year)
        {
            month = string.Empty;
            year = string.Empty;

            if (string.IsNullOrWhiteSpace(expiry)) return false;

            var parts = expiry.Split('/');
            if (parts.Length != 2) return false;

            month = parts[0].Trim();
            year = parts[1].Trim();

            return month.Length == 2 && year.Length == 2 &&
                   int.TryParse(month, out var m) && m >= 1 && m <= 12 &&
                   int.TryParse(year, out _);
        }

        private static int StatusRank(OrderStatus status) => status switch
        {
            OrderStatus.Pending => 0,
            OrderStatus.Confirmed => 1,
            OrderStatus.InProduction => 2,
            OrderStatus.Shipped => 3,
            OrderStatus.Delivered => 4,
            OrderStatus.Cancelled => 5,
            _ => -1
        };
    }
}