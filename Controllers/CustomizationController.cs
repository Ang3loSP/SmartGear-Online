using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using SmartGear_Online.Services;
using System;
using System.IO;
using System.Linq;
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
        private readonly ICartService _cartService;
        private readonly ILogger<CustomizationController> _logger;

        public CustomizationController(
            IProductRepository productRepository,
            ICartService cartService,
            ILogger<CustomizationController> logger)
        {
            _productRepository = productRepository;
            _cartService = cartService;
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
        [RequestFormLimits(MultipartBodyLengthLimit = 8L * 1024 * 1024)]
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

                var maxQuantity = Math.Min(999, product.QuantityInStock);
                if (quantity < 1 || quantity > maxQuantity)
                {
                    TempData["Error"] = "Only " + maxQuantity + " units available per order.";
                    return RedirectToAction("Create", new { productId });
                }

                if ((color?.Length ?? 0) > 50 || (customText?.Length ?? 0) > 100)
                {
                    TempData["Error"] = "Customization details are too long (max 50 chars for color, 100 chars for text).";
                    return RedirectToAction("Create", new { productId });
                }

                // Handle optional logo upload
                string logoUrl = string.Empty;
                if (logoFile != null && logoFile.Length > 0)
                {
                    try
                    {
                        logoUrl = await UploadLogoAsync(logoFile);
                    }
                    catch (InvalidDataException ex)
                    {
                        TempData["Error"] = ex.Message;
                        return RedirectToAction("Create", new { productId });
                    }
                }

                // Build cart item
                var cartItem = new CartItem
                {
                    CartItemId = _cartService.GenerateCartItemId(),
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
                var cart = _cartService.GetCart();

                cart.AddItem(cartItem);

                _cartService.SaveCart(cart);

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
        private const long MaxLogoSizeBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedLogoExtensions = { ".jpg", ".jpeg", ".png" };

        private async Task<string> UploadLogoAsync(IFormFile logoFile)
        {
            if (logoFile.Length == 0)
                throw new InvalidDataException("The uploaded logo file is empty.");

            if (logoFile.Length > MaxLogoSizeBytes)
                throw new InvalidDataException("The logo file must be 5MB or smaller.");

            // Never trust the extension sent by the client blindly; verify it
            // against the allowed set, and also confirm the declared content
            // type matches before we look at the magic bytes.
            var extension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
            if (!AllowedLogoExtensions.Contains(extension))
                throw new InvalidDataException("Only JPG, JPEG or PNG logo files are allowed.");

            var expectedContentType = extension == ".jpeg" ? "image/jpeg" : "image/" + extension.TrimStart('.');
            if (!string.Equals(logoFile.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The file type does not match its extension.");

            // Magic-byte check: a malicious client can fake any content type /
            // extension, but cannot fake the actual file signature. Only real
            // JPEG (FF D8 FF) and PNG (89 50 4E 47 ...) images pass.
            if (!await IsValidImageAsync(logoFile))
                throw new InvalidDataException("The uploaded file is not a valid JPG or PNG image.");

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot/uploads/logos");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Sanitised filename: guidance-generated + a safe extension derived
            // from the validated content type. The user-supplied filename is
            // never used, which blocks both path traversal and stored-XSS via
            // a crafted extension (e.g. ".html" or ".svg").
            var safeExtension = extension == ".jpeg" ? ".jpg" : extension;
            var uniqueFileName = Guid.NewGuid().ToString("N") + safeExtension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = logoFile.OpenReadStream())
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream);
            }

            return "/uploads/logos/" + uniqueFileName;
        }

        /// <summary>
        /// Verifies the first bytes of the upload against known JPEG/PNG
        /// signatures. Opens its own read stream, so the caller's stream is
        /// unaffected.
        /// </summary>
        private static async Task<bool> IsValidImageAsync(IFormFile file)
        {
            if (file.Length < 12) return false;

            using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
            if (read < header.Length) return false;

            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
                return true;

            return false;
        }
    }
}
