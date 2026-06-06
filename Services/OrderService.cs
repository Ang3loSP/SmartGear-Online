using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
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
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderService> _logger;
        private readonly OrderSettings _orderSettings;

        // Valid discount codes (in production, this would be in database)
        private static readonly Dictionary<string, DiscountCode> _validDiscounts = new()
        {
            { "WELCOME10", new DiscountCode { Code = "WELCOME10", Type = "Percentage", Value = 10, IsActive = true } },
            { "SAVE20", new DiscountCode { Code = "SAVE20", Type = "Percentage", Value = 20, IsActive = true } },
            { "FREESHIP", new DiscountCode { Code = "FREESHIP", Type = "FreeShipping", Value = 0, IsActive = true } },
            { "FLAT25", new DiscountCode { Code = "FLAT25", Type = "FixedAmount", Value = 25, IsActive = true } }
        };

        public OrderService(
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            ILogger<OrderService> logger,
            IOptions<OrderSettings> orderSettings)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _logger = logger;
            _orderSettings = orderSettings?.Value ?? new OrderSettings();
        }

        // ================================================
        // QUESTION 5: Calculate Order Totals with Business Logic
        // ================================================
        public async Task<OrderTotals> CalculateOrderTotalsAsync(
            List<CartItem> cartItems,
            string shippingMethod,
            string discountCode = null)
        {
            try
            {
                _logger.LogInformation("Calculating order totals for {ItemCount} items", cartItems?.Count ?? 0);

                if (cartItems == null || !cartItems.Any())
                {
                    return new OrderTotals { Subtotal = 0, GrandTotal = 0 };
                }

                // Calculate subtotal
                var subtotal = cartItems.Sum(item => item.Quantity * item.Price);

                // Calculate tax (configurable rate)
                var taxRate = _orderSettings.TaxRate ?? 0.08m;
                var taxAmount = subtotal * taxRate;

                // Calculate shipping (free shipping over threshold)
                var freeShippingThreshold = _orderSettings.FreeShippingThreshold ?? 50;
                var isFreeShipping = subtotal >= freeShippingThreshold ||
                                    (discountCode == "FREESHIP");

                decimal shippingCost = 0;
                if (!isFreeShipping)
                {
                    shippingCost = shippingMethod?.ToLower() == "express" ? 15.99m : 5.99m;
                }

                // Apply discount if provided
                decimal discountAmount = 0;
                if (!string.IsNullOrEmpty(discountCode))
                {
                    var discountResult = await ApplyDiscountAsync(discountCode, subtotal);
                    if (discountResult.IsValid)
                    {
                        discountAmount = discountResult.DiscountAmount;

                        // If discount is free shipping, override shipping cost
                        if (discountResult.DiscountType == "FreeShipping")
                        {
                            shippingCost = 0;
                            isFreeShipping = true;
                        }
                    }
                }

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
                    Currency = "USD"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order totals");
                throw;
            }
        }

        // ================================================
        // QUESTION 5: Validate Order Business Rules
        // ================================================
        public async Task<OrderValidationResult> ValidateOrderAsync(Order order, List<CartItem> cartItems)
        {
            var result = new OrderValidationResult { IsValid = true };

            try
            {
                _logger.LogInformation("Validating order for customer {CustomerId}", order.CustomerId);

                // Validate cart is not empty
                if (cartItems == null || !cartItems.Any())
                {
                    result.IsValid = false;
                    result.Errors.Add("Your cart is empty. Please add items before checking out.");
                    return result;
                }

                // Validate each item has sufficient stock
                foreach (var item in cartItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);

                    if (product == null)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Product '{item.ProductName}' no longer exists.");
                        continue;
                    }

                    if (!product.CanFulfillOrder(item.Quantity))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Insufficient stock for '{product.ProductName}'. Available: {product.QuantityInStock}");
                    }

                    // Warning for low stock
                    if (product.QuantityInStock <= product.ReorderLevel && product.QuantityInStock > 0)
                    {
                        result.Warnings.Add($"'{product.ProductName}' is running low on stock ({product.QuantityInStock} left). Order soon!");
                    }
                }

                // Validate shipping address
                if (string.IsNullOrWhiteSpace(order.ShippingAddress))
                {
                    result.IsValid = false;
                    result.Errors.Add("Shipping address is required.");
                }
                else if (order.ShippingAddress.Length < 10)
                {
                    result.Warnings.Add("Please provide a complete shipping address for accurate delivery.");
                }

                // Validate total price is positive
                if (order.TotalPrice <= 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid order total. Please review your cart.");
                }

                // Validate maximum order value (business rule)
                var maxOrderValue = _orderSettings.MaxOrderValue ?? 10000;
                if (order.TotalPrice > maxOrderValue)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Order total exceeds maximum allowed value of ${maxOrderValue}. Please contact support for bulk orders.");
                }

                _logger.LogInformation("Order validation completed. IsValid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}",
                    result.IsValid, result.Errors.Count, result.Warnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating order");
                result.IsValid = false;
                result.Errors.Add("An error occurred while validating your order. Please try again.");
                return result;
            }
        }

        // ================================================
        // QUESTION 5: Process Payment (Simulated)
        // ================================================
        public async Task<PaymentResult> ProcessPaymentAsync(Order order, PaymentInfo paymentInfo)
        {
            try
            {
                _logger.LogInformation("Processing payment for order {OrderId}, Amount: ${Amount}",
                    order.OrderId, order.TotalPrice);

                // Simulate payment processing delay
                await Task.Delay(500);

                // In production, integrate with Stripe/PayPal/other payment gateway
                // For demo purposes, we'll accept any valid card number format

                if (string.IsNullOrWhiteSpace(paymentInfo.CardNumber) || paymentInfo.CardNumber.Length < 15)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Invalid card number. Please check and try again."
                    };
                }

                if (string.IsNullOrWhiteSpace(paymentInfo.Cvv) || paymentInfo.Cvv.Length < 3)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Invalid CVV code."
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
        // ================================================
        public async Task<DiscountResult> ApplyDiscountAsync(string discountCode, decimal subtotal)
        {
            try
            {
                _logger.LogInformation("Applying discount code: {DiscountCode} to subtotal: {Subtotal}", discountCode, subtotal);

                if (string.IsNullOrWhiteSpace(discountCode))
                {
                    return new DiscountResult { IsValid = false, Message = "No discount code provided" };
                }

                var code = discountCode.ToUpperInvariant().Trim();

                if (!_validDiscounts.TryGetValue(code, out var discount) || !discount.IsActive)
                {
                    _logger.LogWarning("Invalid or inactive discount code: {DiscountCode}", code);
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

                _logger.LogInformation("Discount applied: {DiscountCode} gave ${DiscountAmount} off", code, discountAmount);

                return new DiscountResult
                {
                    IsValid = true,
                    DiscountAmount = discountAmount,
                    DiscountType = discount.Type,
                    Message = $"Discount of ${discountAmount:F2} applied successfully!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying discount code {DiscountCode}", discountCode);
                return new DiscountResult { IsValid = false, Message = "Error applying discount. Please try again." };
            }
        }

        // ================================================
        // Get Order Status History
        // ================================================
        public async Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Getting status history for order {OrderId}", orderId);

                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return new List<OrderStatusHistory>();
                }

                // Build status timeline based on order dates
                var history = new List<OrderStatusHistory>();

                // Status display names mapping
                var statusDisplayNames = new Dictionary<string, string>
                {
                    { "Pending", "Order Received" },
                    { "Confirmed", "Order Confirmed" },
                    { "In Production", "Being Customized" },
                    { "Shipped", "Order Shipped" },
                    { "Delivered", "Order Delivered" },
                    { "Cancelled", "Order Cancelled" }
                };

                // Add status entries based on order progress
                history.Add(new OrderStatusHistory
                {
                    OrderId = orderId,
                    Status = "Pending",
                    StatusDisplayName = "Order Received",
                    ChangedAt = order.OrderDate,
                    ChangedBy = "System",
                    Notes = "Your order has been received and is awaiting confirmation."
                });

                // If order is confirmed or beyond, add confirmed entry
                if (order.Status != "Pending")
                {
                    var confirmedDate = order.OrderDate.AddHours(2); // Simulated confirmation time
                    history.Add(new OrderStatusHistory
                    {
                        OrderId = orderId,
                        Status = "Confirmed",
                        StatusDisplayName = "Order Confirmed",
                        ChangedAt = confirmedDate,
                        ChangedBy = "System",
                        Notes = "Your order has been confirmed and is being prepared for production."
                    });
                }

                // If order is in production or beyond
                if (order.Status == "In Production" || order.Status == "Shipped" || order.Status == "Delivered")
                {
                    var productionDate = order.OrderDate.AddDays(1);
                    history.Add(new OrderStatusHistory
                    {
                        OrderId = orderId,
                        Status = "In Production",
                        StatusDisplayName = "In Production",
                        ChangedAt = productionDate,
                        ChangedBy = "System",
                        Notes = "Your custom items are being manufactured and personalized."
                    });
                }

                // If order is shipped
                if (order.Status == "Shipped" || order.Status == "Delivered")
                {
                    var shippedDate = order.OrderDate.AddDays(3);
                    history.Add(new OrderStatusHistory
                    {
                        OrderId = orderId,
                        Status = "Shipped",
                        StatusDisplayName = "Order Shipped",
                        ChangedAt = shippedDate,
                        ChangedBy = "System",
                        Notes = $"Your order has been shipped. Tracking number: {order.TrackingNumber ?? "pending"}"
                    });
                }

                // If order is delivered
                if (order.Status == "Delivered")
                {
                    var deliveredDate = order.OrderDate.AddDays(7);
                    history.Add(new OrderStatusHistory
                    {
                        OrderId = orderId,
                        Status = "Delivered",
                        StatusDisplayName = "Order Delivered",
                        ChangedAt = deliveredDate,
                        ChangedBy = "System",
                        Notes = "Your order has been delivered. Enjoy your custom gear!"
                    });
                }

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status history for order {OrderId}", orderId);
                return new List<OrderStatusHistory>();
            }
        }

        // ================================================
        // Generate Invoice PDF (Simulated)
        // ================================================
        public async Task<byte[]> GenerateInvoicePdfAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Generating invoice PDF for order {OrderId}", orderId);

                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    throw new ArgumentException($"Order {orderId} not found");
                }

                // In production, use a PDF generation library like iTextSharp or QuestPDF
                // For demo, return a simulated byte array
                await Task.Delay(100); // Simulate PDF generation

                // This would be actual PDF content in production
                var simulatedPdf = System.Text.Encoding.UTF8.GetBytes($"SIMULATED_PDF_INVOICE_ORDER_{orderId}");

                _logger.LogInformation("Invoice PDF generated for order {OrderId}", orderId);
                return simulatedPdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice PDF for order {OrderId}", orderId);
                throw;
            }
        }

        // ================================================
        // Validate Customization Business Rules
        // ================================================
        public async Task<bool> ValidateCustomizationAsync(Customization customization)
        {
            try
            {
                _logger.LogInformation("Validating customization for product {ProductId}", customization.ProductId);

                var product = await _productRepository.GetProductByIdAsync(customization.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for customization", customization.ProductId);
                    return false;
                }

                // Validate color is supported
                var validColors = new[] { "Red", "Blue", "Green", "Yellow", "White", "Black", "Orange", "Purple", "Pink" };
                if (!string.IsNullOrEmpty(customization.Color) && !validColors.Contains(customization.Color, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid color {Color} selected for product {ProductId}", customization.Color, customization.ProductId);
                    return false;
                }

                // Validate custom text length
                if (!string.IsNullOrEmpty(customization.CustomText) && customization.CustomText.Length > 50)
                {
                    _logger.LogWarning("Custom text too long ({Length} chars) for product {ProductId}",
                        customization.CustomText.Length, customization.ProductId);
                    return false;
                }

                // Validate logo URL is valid (if provided)
                if (!string.IsNullOrEmpty(customization.LogoImageUrl))
                {
                    var isValidUrl = Uri.TryCreate(customization.LogoImageUrl, UriKind.Absolute, out _);
                    if (!isValidUrl)
                    {
                        _logger.LogWarning("Invalid logo URL for product {ProductId}", customization.ProductId);
                        return false;
                    }
                }

                _logger.LogInformation("Customization validation passed for product {ProductId}", customization.ProductId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating customization for product {ProductId}", customization.ProductId);
                return false;
            }
        }
    }

    // ================================================
    // Supporting Classes
    // ================================================

    public class DiscountCode
    {
        public string Code { get; set; }
        public string Type { get; set; } // "Percentage", "FixedAmount", "FreeShipping"
        public decimal Value { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class OrderSettings
    {
        public decimal? TaxRate { get; set; } = 0.08m;
        public decimal? FreeShippingThreshold { get; set; } = 50;
        public decimal? MaxOrderValue { get; set; } = 10000;
        public int? MaxItemsPerOrder { get; set; } = 50;
    }
}