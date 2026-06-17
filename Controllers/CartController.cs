using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Extensions;
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
        private readonly ILogger<CartController> _logger;

        private const string CartSessionKey = "ShoppingCart";

        public CartController(
            IProductRepository productRepository,
            IOrderService orderService,
            ILogger<CartController> logger)
        {
            _productRepository = productRepository;
            _orderService = orderService;
            _logger = logger;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart.Items);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
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

                if (quantity > product.QuantityInStock)
                    return Json(new { success = false, message = "Only " + product.QuantityInStock + " items available" });

                var cart = GetCart();

                var cartItem = new CartItem
                {
                    CartItemId = GenerateCartItemId(),
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl,
                    CustomizationId = customizationId
                };

                cart.AddItem(cartItem);
                SaveCart(cart);

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
        public IActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                var cart = GetCart();
                cart.UpdateQuantity(cartItemId, quantity);
                SaveCart(cart);

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
        public IActionResult RemoveFromCart(int cartItemId)
        {
            try
            {
                var cart = GetCart();
                cart.RemoveItem(cartItemId);
                SaveCart(cart);

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
        public async Task<IActionResult> ApplyDiscount(string discountCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                    return Json(new { success = false, message = "Please enter a discount code." });

                var cart = GetCart();

                if (!cart.HasItems())
                    return Json(new { success = false, message = "Your cart is empty." });

                var result = await _orderService.ApplyDiscountAsync(discountCode.Trim().ToUpper(), cart.Subtotal);

                if (!result.IsValid)
                    return Json(new { success = false, message = result.Message });

                cart.DiscountCode = discountCode.Trim().ToUpper();
                cart.DiscountAmount = result.DiscountAmount;
                SaveCart(cart);

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
            var cart = GetCart();
            return Json(cart.ItemCount);
        }

        // ------------------------------------------------
        // Private helpers
        // ------------------------------------------------
        private ShoppingCart GetCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CartSessionKey);
            if (cart == null)
            {
                cart = new ShoppingCart();
                SaveCart(cart);
            }
            return cart;
        }

        private void SaveCart(ShoppingCart cart)
        {
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
        }

        private int GenerateCartItemId()
        {
            return Math.Abs(Guid.NewGuid().GetHashCode());
        }
    }
}
