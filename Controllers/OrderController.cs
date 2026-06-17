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
        // CHECKOUT
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
        // PLACE ORDER
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            try
            {
                _logger.LogInformation("PlaceOrder POST called for user {UserId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));

                var shoppingCart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("ShoppingCart");

                if (shoppingCart == null || !shoppingCart.HasItems())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }

                if (!ModelState.IsValid)
                {
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
                        TempData["Error"] = "Product '" + item.ProductName + "' no longer exists.";
                        return RedirectToAction("Index", "Cart");
                    }

                    if (!product.CanFulfillOrder(item.Quantity))
                    {
                        TempData["Error"] = "Insufficient stock for '" + product.ProductName + "'.";
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

                // Use the customer's own email from their claim
                var customerEmail = User.FindFirstValue(ClaimTypes.Email)
                                    ?? User.FindFirstValue(ClaimTypes.Name);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendOrderConfirmationEmailAsync(orderId, customerEmail);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send confirmation email for order {OrderId}", orderId);
                    }
                });

                _logger.LogInformation("Order {OrderId} placed successfully by user {UserId}",
                    orderId, User.FindFirstValue(ClaimTypes.NameIdentifier));

                TempData["Success"] = "Order #" + orderId + " placed successfully!";
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
        // ORDER CONFIRMATION
        // ================================================
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(id);

                if (order == null)
                    return NotFound("Order not found");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (order.CustomerId != userId && !User.IsInRole("Admin"))
                    return Unauthorized("You don't have permission to view this order");

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
        // ORDER HISTORY
        // ================================================
        [HttpGet]
        public async Task<IActionResult> History()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var orders = await _orderRepository.GetCustomerOrdersAsync(userId);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order history");
                TempData["Error"] = "Unable to load order history.";
                return View(new System.Collections.Generic.List<Order>());
            }
        }

        // ================================================
        // ORDER TRACKING
        // ================================================
        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(id);

                if (order == null)
                    return NotFound("Order not found");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (order.CustomerId != userId && !User.IsInRole("Admin"))
                    return Unauthorized();

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
        // UPDATE ORDER STATUS (Admin only)
        // FIX: email now goes to the customer, not the admin
        // ================================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            try
            {
                _logger.LogInformation("Admin {UserId} updating order {OrderId} to {Status}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier), orderId, status);

                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                var validStatuses = new[] { "Pending", "Confirmed", "In Production", "Shipped", "Delivered", "Cancelled" };
                if (!validStatuses.Contains(status))
                    return Json(new { success = false, message = "Invalid status value" });

                await _orderRepository.UpdateOrderStatusAsync(orderId, status);

                // FIX: retrieve the customer's email from the order's navigation property,
                // not from the currently logged-in admin's claims.
                var customerEmail = order.Customer?.Email ?? string.Empty;

                if (!string.IsNullOrEmpty(customerEmail))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.SendOrderStatusUpdateAsync(orderId, status, customerEmail);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx,
                                "Failed to send status update email for order {OrderId}", orderId);
                        }
                    });
                }

                return Json(new { success = true, message = "Order status updated to " + status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} status", orderId);
                return Json(new { success = false, message = "Failed to update order status" });
            }
        }

        // ================================================
        // CANCEL ORDER (Admin only)
        // ================================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                if (!order.CanBeCancelled())
                    return Json(new { success = false, message = "Order cannot be cancelled at this stage" });

                await _orderRepository.UpdateOrderStatusAsync(orderId, "Cancelled");

                foreach (var item in order.OrderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.ReplenishInventory(item.Quantity);
                        await _productRepository.UpdateProductAsync(product);
                    }
                }

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
