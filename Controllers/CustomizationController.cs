using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Extensions;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    /// <summary>
    /// Customization Controller — handles product customization.
    /// FIX: customized items now save to the session cart (same store
    /// used by CartController) instead of the empty CartRepository.
    /// </summary>
    [Authorize]
    public class CustomizationController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<CustomizationController> _logger;

        // Session key must match CartController exactly
        private const string CartSessionKey = "ShoppingCart";

        public CustomizationController(
            IProductRepository productRepository,
            ILogger<CustomizationController> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        // ================================================
        // GET: /Customization/Create/{productId}
        // ================================================
        [HttpGet]
        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Create(int productId)
        {
            try
            {
                _logger.LogInformation("Customization page requested for product {ProductId}", productId);

                var product = await _productRepository.GetProductByIdAsync(productId);

                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for customization", productId);
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (!product.IsInStock())
                {
                    TempData["Error"] = "This product is currently out of stock.";
                    return RedirectToAction("Details", "Product", new { id = productId });
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customization page for product {ProductId}", productId);
                TempData["Error"] = "An error occurred loading the customization page.";
                return RedirectToAction("Index", "Product");
            }
        }

        // ================================================
        // POST: /Customization/Create
        // FIX: saves directly into the session cart
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int productId,
            string color,
            string customText,
            int quantity,
            IFormFile? logoFile)
        {
            try
            {
                _logger.LogInformation(
                    "Saving customization for product {ProductId}, Color: {Color}, Quantity: {Quantity}",
                    productId, color, quantity);

                var product = await _productRepository.GetProductByIdAsync(productId);

                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (quantity < 1 || quantity > product.QuantityInStock)
                {
                    TempData["Error"] = "Only " + product.QuantityInStock + " units available.";
                    return RedirectToAction("Create", new { productId });
                }

                // Handle optional logo upload
                string logoUrl = string.Empty;
                if (logoFile != null && logoFile.Length > 0)
                {
                    logoUrl = await UploadLogoAsync(logoFile);
                }

                // Build cart item
                var cartItem = new CartItem
                {
                    CartItemId = Math.Abs(Guid.NewGuid().GetHashCode()),
                    ProductId = productId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl,
                    Color = color ?? "Blue",
                    CustomText = customText ?? string.Empty,
                    LogoImageUrl = logoUrl,
                    AddedDate = DateTime.UtcNow
                };

                // Read, update, and save the session cart
                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CartSessionKey)
                           ?? new ShoppingCart();

                cart.AddItem(cartItem);

                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);

                _logger.LogInformation(
                    "Customization added to session cart for product {ProductId}. Cart now has {Count} items.",
                    productId, cart.ItemCount);

                TempData["Success"] = "'" + product.ProductName + "' has been added to your cart!";
                return RedirectToAction("Index", "Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving customization for product {ProductId}", productId);
                TempData["Error"] = "An error occurred while saving your customization.";
                return RedirectToAction("Create", new { productId });
            }
        }

        // ================================================
        // GET: /Customization/Preview
        // ================================================
        [HttpGet]
        public IActionResult Preview(int productId, string color, string customText)
        {
            try
            {
                var previewData = new
                {
                    Color = color,
                    CustomText = customText,
                    Timestamp = DateTime.Now
                };
                return Json(previewData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview");
                return Json(new { error = "Preview generation failed" });
            }
        }

        // ================================================
        // GET: /Customization/GetColors
        // ================================================
        [HttpGet]
        public IActionResult GetColors(int productId)
        {
            var colors = new[]
            {
                new { Name = "Red",    Hex = "#FF0000" },
                new { Name = "Blue",   Hex = "#0000FF" },
                new { Name = "Green",  Hex = "#00FF00" },
                new { Name = "Yellow", Hex = "#FFFF00" },
                new { Name = "White",  Hex = "#FFFFFF" },
                new { Name = "Black",  Hex = "#000000" }
            };
            return Json(colors);
        }

        // ================================================
        // Private helper
        // ================================================
        private async Task<string> UploadLogoAsync(IFormFile logoFile)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot/uploads/logos");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + logoFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(fileStream);
            }

            return "/uploads/logos/" + uniqueFileName;
        }
    }
}
