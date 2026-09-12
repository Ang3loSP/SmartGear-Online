using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IOptions<OrderSettings> _orderSettings;
        private readonly ILogger<CartController> _logger;

        public CartController(
            IProductRepository productRepository,
            IOrderService orderService,
            ICartService cartService,
            IOptions<OrderSettings> orderSettings,
            ILogger<CartController> logger)
        {
            _productRepository = productRepository;
            _orderService = orderService;
            _cartService = cartService;
            _orderSettings = orderSettings;
            _logger = logger;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var settings = _orderSettings.Value;
            var cart = _cartService.GetCart();

            // FIX: hand the live business rules (tax rate, free-shipping
            // threshold, shipping rates) to the page so cart.js calculates with
            // the SAME numbers OrderService.CalculateOrderTotalsAsync uses —
            // the site can never show a preview that disagrees with the checkout.
            ViewBag.TotalsConfig = new
            {
                TaxRate = settings.TaxRate ?? 0.08m,
                TaxPercent = (settings.TaxRate ?? 0.08m) * 100,
                FreeThreshold = settings.FreeShippingThreshold ?? 50,
                StandardRate = settings.StandardShippingRate ?? 5.99m,
                ExpressRate = settings.ExpressShippingRate ?? 15.00m
            };

            // FIX: the order summary is now computed server-side too, from the
            // saved cart + the same rules as the calculator, so the numbers are
            // right on first paint AND after a reload — no more depending on JS
            // having watched the discount get applied. After a refresh the
            // FREESHIP flag and discount amount used to silently vanish, and the
            // summary (R5.99 shipping, no discount) disagreed with what the
            // checkout would actually charge.
            var subtotal = cart.Subtotal;
            var tax = subtotal * (settings.TaxRate ?? 0.08m);
            var discount = cart.DiscountAmount;

            var isFreeShippingCode = false;
            if (!string.IsNullOrWhiteSpace(cart.DiscountCode))
            {
                var result = _orderService.ApplyDiscount(cart.DiscountCode, subtotal);
                isFreeShippingCode = result.IsValid && result.DiscountType == "FreeShipping";
            }

            var freeByThreshold = subtotal >= (settings.FreeShippingThreshold ?? 50);
            var shipping = isFreeShippingCode || freeByThreshold
                ? 0m
                : (settings.StandardShippingRate ?? 5.99m);

            ViewBag.CartSummary = new
            {
                Subtotal = subtotal,
                Tax = tax,
                Shipping = shipping,
                Discount = discount,
                GrandTotal = subtotal + tax + shipping - discount,
                IsFreeShipping = isFreeShippingCode
            };

            return View(cart.Items);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, int? customizationId = null)
        {
            // FIX: this action is posted to by plain HTML forms (Product Details and
            // Customization), so a native submit must redirect - not dump raw JSON in
            // the browser. AJAX callers (site.js, jQuery sends X-Requested-With) still
            // get JSON so the badge can update without a page reload.
            bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal);

            try
            {
                _logger.LogInformation("Adding product {ProductId} to cart, quantity: {Quantity}", productId, quantity);

                var product = await _productRepository.GetProductByIdAsync(productId);

                if (product == null)
                    return AddToCartFailure(isAjax, "Product not found");

                if (!product.IsInStock())
                    return AddToCartFailure(isAjax, "Product is out of stock");

                if (quantity < 1)
                    return AddToCartFailure(isAjax, "Quantity must be at least 1");

                // Cap quantity at both the [Range] upper limit and available stock.
                var maxQuantity = Math.Min(999, product.QuantityInStock);
                if (quantity > maxQuantity)
                    return AddToCartFailure(isAjax, "Only " + maxQuantity + " items available");

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

                if (isAjax)
                    return Json(new { success = true, message = "Item added to cart", cartCount = cart.ItemCount });

                TempData["Success"] = "Item added to your cart.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return AddToCartFailure(isAjax, "An error occurred. Please try again.");
            }
        }

        private IActionResult AddToCartFailure(bool isAjax, string message)
        {
            if (isAjax)
                return Json(new { success = false, message });

            TempData["Error"] = message;
            return RedirectToAction("Index");
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
