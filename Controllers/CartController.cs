using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using SmartGear_Online.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            IProductRepository productRepository,
            IOrderService orderService,
            ICartService cartService,
            ILogger<CartController> logger)
        {
            _productRepository = productRepository;
            _orderService = orderService;
            _cartService = cartService;
            _logger = logger;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            return View(cart.Items);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, int? customizationId = null)
        {
            try
            {
                _logger.LogInformation("Adding product {ProductId} to cart, quantity: {Quantity}", productId, quantity);

                var product = await _productRepository.GetProductByIdAsync(productId);

                if (product == null)
                    return Json(new { success = false, message = "Product not found" });

                if (!product.IsInStock())
                    return Json(new { success = false, message = "Product is out of stock" });

                if (quantity < 1)
                    return Json(new { success = false, message = "Quantity must be at least 1" });

                // Cap quantity at both the [Range] upper limit and available stock.
                var maxQuantity = Math.Min(999, product.QuantityInStock);
                if (quantity > maxQuantity)
                    return Json(new { success = false, message = "Only " + maxQuantity + " items available" });

                var cart = _cartService.GetCart();

                var cartItem = new CartItem
                {
                    CartItemId = _cartService.GenerateCartItemId(),
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl,
                    CustomizationId = customizationId
                };

                cart.AddItem(cartItem);
                _cartService.SaveCart(cart);

                _logger.LogInformation(
                    "Product added to cart. Cart now has {ItemCount} items", cart.ItemCount);

                return Json(new { success = true, message = "Item added to cart", cartCount = cart.ItemCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                var cart = _cartService.GetCart();
                var item = cart.Items.FirstOrDefault(i => i.CartItemId == cartItemId);

                if (item == null)
                    return Json(new { success = false, message = "Item not found in cart" });

                if (quantity < 1)
                    return Json(new { success = false, message = "Quantity must be at least 1" });

                // Never allow a cart quantity above the live stock level.
                var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                var maxQuantity = product == null ? 0 : Math.Min(999, product.QuantityInStock);

                if (maxQuantity < 1)
                    return Json(new { success = false, message = "Product is no longer available" });

                if (quantity > maxQuantity)
                    return Json(new { success = false, message = "Only " + maxQuantity + " items available" });

                cart.UpdateQuantity(cartItemId, quantity);
                _cartService.SaveCart(cart);

                return Json(new { success = true, message = "Quantity updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                return Json(new { success = false, message = "Failed to update quantity" });
            }
        }

        // POST: /Cart/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int cartItemId)
        {
            try
            {
                var cart = _cartService.GetCart();
                cart.RemoveItem(cartItemId);
                _cartService.SaveCart(cart);

                return Json(new { success = true, message = "Item removed from cart" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new { success = false, message = "Failed to remove item" });
            }
        }

        // POST: /Cart/ApplyDiscount
        // FIX: discount codes are now validated server-side via IOrderService,
        // not against a hardcoded list in cart.js.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyDiscount(string discountCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                    return Json(new { success = false, message = "Please enter a discount code." });

                if (discountCode.Length > 32)
                    return Json(new { success = false, message = "Discount code is too long." });

                var cart = _cartService.GetCart();

                if (!cart.HasItems())
                    return Json(new { success = false, message = "Your cart is empty." });

                var result = _orderService.ApplyDiscount(discountCode.Trim().ToUpper(), cart.Subtotal);

                if (!result.IsValid)
                    return Json(new { success = false, message = result.Message });

                cart.DiscountCode = discountCode.Trim().ToUpper();
                cart.DiscountAmount = result.DiscountAmount;
                _cartService.SaveCart(cart);

                return Json(new
                {
                    success = true,
                    message = result.Message,
                    discountAmount = result.DiscountAmount,
                    discountType = result.DiscountType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying discount code");
                return Json(new { success = false, message = "An error occurred applying the discount." });
            }
        }

        // GET: /Cart/GetCartCount
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = _cartService.GetCart();
            return Json(cart.ItemCount);
        }
    }
}
