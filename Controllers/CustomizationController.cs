using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using SmartGear_Online.Services;
using System;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    /// <summary>
    /// Customization Controller - Handles product customization
    /// Question 3 & 8: Customization engine with real-time preview
    /// </summary>
    [Authorize]
    public class CustomizationController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICartRepository _cartRepository;
        private readonly ILogger<CustomizationController> _logger;

        public CustomizationController(
            IProductRepository productRepository,
            ICartRepository cartRepository,
            ILogger<CustomizationController> logger)
        {
            _productRepository = productRepository;
            _cartRepository = cartRepository;
            _logger = logger;
        }

        /// <summary>
        /// GET: /Customization/Create/{productId}
        /// Displays customization page for a specific product
        /// </summary>
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

        /// <summary>
        /// POST: /Customization/Create
        /// Saves customization and adds to cart
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, string color, string customText, int quantity, IFormFile? logoFile)
        {
            try
            {
                _logger.LogInformation("Saving customization for product {ProductId}, Color: {Color}, Quantity: {Quantity}",
                    productId, color, quantity);

                var product = await _productRepository.GetProductByIdAsync(productId);

                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (quantity > product.QuantityInStock)
                {
                    TempData["Error"] = $"Only {product.QuantityInStock} units available.";
                    return RedirectToAction("Create", new { productId });
                }

                // Handle logo upload if provided
                string logoUrl = string.Empty;
                if (logoFile != null && logoFile.Length > 0)
                {
                    logoUrl = await UploadLogoAsync(logoFile);
                }

                // Create customization record
                var customization = new Customization
                {
                    ProductId = productId,
                    Color = color ?? "Blue",
                    CustomText = customText ?? string.Empty,
                    LogoImageUrl = logoUrl,
                    CreatedDate = DateTime.UtcNow
                };

                // Add to cart
                var cartItem = new CartItem
                {
                    ProductId = productId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl,
                    Color = customization.Color,
                    CustomText = customization.CustomText,
                    LogoImageUrl = customization.LogoImageUrl,
                    AddedDate = DateTime.UtcNow
                };

                await _cartRepository.AddToCartAsync(cartItem, User.Identity?.Name ?? "Anonymous");

                _logger.LogInformation("Customization saved and added to cart for product {ProductId}", productId);

                TempData["Success"] = $"'{product.ProductName}' has been added to your cart!";
                return RedirectToAction("Index", "Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving customization for product {ProductId}", productId);
                TempData["Error"] = "An error occurred while saving your customization.";
                return RedirectToAction("Create", new { productId });
            }
        }

        /// <summary>
        /// GET: /Customization/Preview
        /// AJAX endpoint for real-time preview updates
        /// </summary>
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

        /// <summary>
        /// GET: /Customization/GetColors
        /// Returns available colors for a product
        /// </summary>
        [HttpGet]
        public IActionResult GetColors(int productId)
        {
            var colors = new[]
            {
                new { Name = "Red", Hex = "#FF0000" },
                new { Name = "Blue", Hex = "#0000FF" },
                new { Name = "Green", Hex = "#00FF00" },
                new { Name = "Yellow", Hex = "#FFFF00" },
                new { Name = "White", Hex = "#FFFFFF" },
                new { Name = "Black", Hex = "#000000" }
            };
            return Json(colors);
        }

        private async Task<string> UploadLogoAsync(IFormFile logoFile)
        {
            // In production, upload to Azure Blob Storage or local storage
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/logos");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{logoFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(fileStream);
            }

            return $"/uploads/logos/{uniqueFileName}";
        }
    }
}