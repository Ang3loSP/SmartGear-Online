using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Extensions;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<CartController> _logger;

        private const string CartSessionKey = "ShoppingCart";

        public CartController(
            IProductRepository productRepository,
            ILogger<CartController> logger)
        {
            _productRepository = productRepository;
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
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (!product.IsInStock())
                {
                    return Json(new { success = false, message = "Product is out of stock" });
                }

                if (quantity > product.QuantityInStock)
                {
                    return Json(new { success = false, message = $"Only {product.QuantityInStock} items available" });
                }

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

                _logger.LogInformation("Product added to cart successfully. Cart now has {ItemCount} items", cart.ItemCount);

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
        [HttpPost]
        public IActionResult ApplyDiscount(string discountCode)
        {
            // Logic for applying discount
            return Json(new { success = true, message = "Discount applied" });
        }

        // GET: /Cart/GetCartCount
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCart();
            return Json(cart.ItemCount);
        }

        // Helper methods
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