using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Extensions;
using SmartGear_Online.Models;
using SmartGear_Online.Models.ViewModels;
using SmartGear_Online.Repositories;
using SmartGear_Online.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    // ================================================
    // QUESTION 10.8: AUTHORIZATION - Require authentication for all actions
    // ================================================
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            INotificationService notificationService,
            ILogger<OrderController> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ================================================
        // CHECKOUT ACTION
        // ================================================
        [HttpGet]
        public IActionResult Checkout()
        {
            try
            {
                _logger.LogInformation("Checkout page requested by user {UserId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

                var shoppingCart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("ShoppingCart");

                if (shoppingCart == null || !shoppingCart.HasItems())
                {
                    _logger.LogWarning("User {UserId} attempted checkout with empty cart",
                        User.FindFirstValue(ClaimTypes.NameIdentifier));

                    TempData["Error"] = "Your cart is empty. Please add items before checkout.";
                    return RedirectToAction("Index", "Cart");
                }

                var checkoutModel = new CheckoutViewModel
                {
                    CartItems = shoppingCart.Items,
                    Subtotal = shoppingCart.Subtotal,
                    Tax = shoppingCart.Subtotal * 0.08m,
                    ShippingCost = shoppingCart.Subtotal > 50 ? 0 : 5.99m,
                    GrandTotal = shoppingCart.Subtotal * 1.08m + (shoppingCart.Subtotal > 50 ? 0 : 5.99m)
                };

                return View(checkoutModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page for user {UserId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

                TempData["Error"] = "An error occurred while loading the checkout page. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // ================================================
        // PLACE ORDER ACTION - WITH CSRF PROTECTION
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]  // QUESTION 10.8: CSRF PROTECTION
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            try
            {
                _logger.LogInformation("PlaceOrder POST called for user {UserId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

                var shoppingCart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("ShoppingCart");

                if (shoppingCart == null || !shoppingCart.HasItems())
                {
                    _logger.LogWarning("User {UserId} attempted place order with empty cart",
                        User.FindFirstValue(ClaimTypes.NameIdentifier));

                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid checkout model for user {UserId}",
                        User.FindFirstValue(ClaimTypes.NameIdentifier));

                    model.CartItems = shoppingCart.Items;
                    model.Subtotal = shoppingCart.Subtotal;
                    model.Tax = shoppingCart.Subtotal * 0.08m;
                    model.ShippingCost = shoppingCart.Subtotal > 50 ? 0 : 5.99m;
                    model.GrandTotal = shoppingCart.Subtotal + model.Tax + model.ShippingCost;

                    return View("Checkout", model);
                }

                foreach (var item in shoppingCart.Items)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);

                    if (product == null)
                    {
                        _logger.LogError("Product {ProductId} not found during order placement", item.ProductId);
                        TempData["Error"] = $"Product '{item.ProductName}' no longer exists.";
                        return RedirectToAction("Index", "Cart");
                    }

                    if (!product.CanFulfillOrder(item.Quantity))
                    {
                        _logger.LogWarning("Insufficient stock for product {ProductName}", product.ProductName);
                        TempData["Error"] = $"Insufficient stock for '{product.ProductName}'.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                var order = new Order
                {
                    CustomerId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    ShippingAddress = model.GetShippingAddress(),
                    BillingAddress = model.GetBillingAddress(),
                    ShippingMethod = model.ShippingMethod,
                    TotalPrice = model.GrandTotal
                };

                var orderId = await _orderRepository.CreateOrderAsync(order);

                foreach (var item in shoppingCart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    };

                    await _orderRepository.AddOrderItemAsync(orderItem);
                    await _productRepository.ReduceInventoryAsync(item.ProductId, item.Quantity);
                }

                HttpContext.Session.Remove("ShoppingCart");

                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendOrderConfirmationEmailAsync(orderId, userEmail);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send confirmation email for order {OrderId}", orderId);
                    }
                });

                _logger.LogInformation("Order {OrderId} placed successfully by user {UserId}",
                    orderId, User.FindFirstValue(ClaimTypes.NameIdentifier));

                TempData["Success"] = $"Order #{orderId} placed successfully!";
                return RedirectToAction("Confirmation", new { id = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during order placement for user {UserId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

                TempData["Error"] = "An unexpected error occurred while processing your order. Please contact support.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // ================================================
        // ORDER CONFIRMATION - WITH OWNERSHIP CHECK
        // ================================================
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            try
            {
                _logger.LogInformation("Order confirmation requested for order {OrderId} by user {UserId}",
                    id, User.FindFirstValue(ClaimTypes.NameIdentifier));

                var order = await _orderRepository.GetOrderByIdAsync(id);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for confirmation", id);
                    return NotFound("Order not found");
                }

                // QUESTION 10.8: SECURITY CHECK - Ensure user owns this order
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (order.CustomerId != userId && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning("User {UserId} attempted to view order {OrderId} without permission",
                        userId, id);
                    return Unauthorized("You don't have permission to view this order");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order confirmation for order {OrderId}", id);
                TempData["Error"] = "Unable to load order confirmation. Please check your order history.";
                return RedirectToAction("History");
            }
        }

        // ================================================
        // ORDER HISTORY - USER SPECIFIC
        // ================================================
        [HttpGet]
        public async Task<IActionResult> History()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation("Order history requested by user {UserId}", userId);

                // QUESTION 10.8: SECURITY - Only returns orders for the logged-in user
                var orders = await _orderRepository.GetCustomerOrdersAsync(userId);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order history");
                TempData["Error"] = "Unable to load order history.";
                return View(new List<Order>());
            }
        }

        // ================================================
        // ORDER TRACKING - WITH OWNERSHIP CHECK
        // ================================================
        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            try
            {
                _logger.LogInformation("Order tracking requested for order {OrderId}", id);

                var order = await _orderRepository.GetOrderByIdAsync(id);

                if (order == null)
                {
                    return NotFound("Order not found");
                }

                // QUESTION 10.8: SECURITY CHECK - Ensure user owns this order
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (order.CustomerId != userId && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning("User {UserId} attempted to track order {OrderId} without permission",
                        userId, id);
                    return Unauthorized();
                }

                var estimatedDelivery = order.OrderDate.AddDays(
                    order.ShippingMethod == "Express" ? 3 : 7);

                ViewBag.EstimatedDelivery = estimatedDelivery;
                ViewBag.DaysRemaining = Math.Max(0, (estimatedDelivery - DateTime.UtcNow).Days);

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tracking for order {OrderId}", id);
                TempData["Error"] = "Unable to load tracking information.";
                return RedirectToAction("History");
            }
        }

        // ================================================
        // QUESTION 10.8: ADMIN ONLY - UPDATE ORDER STATUS
        // ================================================
        [HttpPost]
        [Authorize(Roles = "Admin")]  // Only Admin can update order status
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            try
            {
                _logger.LogInformation("Admin user {UserId} updating order {OrderId} status to {Status}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier), orderId, status);

                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var validStatuses = new[] { "Pending", "Confirmed", "In Production", "Shipped", "Delivered", "Cancelled" };
                if (!validStatuses.Contains(status))
                {
                    return Json(new { success = false, message = "Invalid status value" });
                }

                await _orderRepository.UpdateOrderStatusAsync(orderId, status);

                // Send email notification to customer about status change
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendOrderStatusUpdateAsync(orderId, status, userEmail);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send status update email for order {OrderId}", orderId);
                    }
                });

                _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
                return Json(new { success = true, message = $"Order status updated to {status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} status", orderId);
                return Json(new { success = false, message = "Failed to update order status" });
            }
        }

        // ================================================
        // QUESTION 10.8: ADMIN ONLY - CANCEL ORDER
        // ================================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                _logger.LogInformation("Admin user {UserId} cancelling order {OrderId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier), orderId);

                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                if (!order.CanBeCancelled())
                {
                    return Json(new { success = false, message = "Order cannot be cancelled at this stage" });
                }

                await _orderRepository.UpdateOrderStatusAsync(orderId, "Cancelled");

                // Restore inventory items
                foreach (var item in order.OrderItems)
                {
                    // Add back the quantities to inventory
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.ReplenishInventory(item.Quantity);
                        await _productRepository.UpdateProductAsync(product);
                    }
                }

                _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
                return Json(new { success = true, message = "Order cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                return Json(new { success = false, message = "Failed to cancel order" });
            }
        }
    }
}