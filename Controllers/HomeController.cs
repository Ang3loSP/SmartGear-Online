using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    /// <summary>
    /// Home Controller - Handles main pages of the application
    /// Question 3 & 7: Main entry point and layout
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IProductRepository productRepository)
        {
            _logger = logger;
            _productRepository = productRepository;
        }

        /// <summary>
        /// GET: / or /Home/Index
        /// Displays homepage with featured products
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "page" })]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Homepage Index action called");

                // Get featured products (first 3 active products)
                var featuredProducts = await _productRepository.GetProductsAsync(1, 3);

                ViewBag.FeaturedProducts = featuredProducts;
                ViewBag.HeroTitle = "SmartGear Online";
                ViewBag.HeroSubtitle = "Customize Your Team Gear - Fast, Easy & Professional";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading homepage");
                return View();
            }
        }

        /// <summary>
        /// GET: /Home/About
        /// Displays about page
        /// </summary>
        [HttpGet]
        public IActionResult About()
        {
            ViewBag.Title = "About SmartGear";
            return View();
        }

        /// <summary>
        /// GET: /Home/Contact
        /// Displays contact page
        /// </summary>
        [HttpGet]
        public IActionResult Contact()
        {
            ViewBag.Title = "Contact Us";
            return View();
        }

        /// <summary>
        /// POST: /Home/Contact
        /// Handles contact form submission from the dedicated Contact page
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string name, string email, string subject, string message)
        {
            var validationError = ValidateContactInput(name, email, subject, message);
            if (validationError != null)
            {
                TempData["Error"] = validationError;
                return View();
            }

            // In production, send email or save to database
            _logger.LogInformation("Contact form submitted by {Name} ({Email}): {Subject}", name, email, subject);

            TempData["Success"] = "Thank you for contacting us. We'll get back to you soon!";
            return RedirectToAction("Contact");
        }

        /// <summary>
        /// POST: /Home/ContactAjax
        /// FIX: JSON endpoint for the homepage "Contact Us" modal, which
        /// submits via fetch() and needs a JSON response rather than a
        /// redirect. Shares the same validation and logging logic as the
        /// full Contact page above.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ContactAjax(string name, string email, string subject, string message)
        {
            var validationError = ValidateContactInput(name, email, subject, message);
            if (validationError != null)
            {
                return Json(new { success = false, message = validationError });
            }

            _logger.LogInformation("Contact modal submitted by {Name} ({Email}): {Subject}", name, email, subject);

            return Json(new { success = true, message = "Thank you for contacting us. We'll get back to you soon!" });
        }

        /// <summary>
        /// Common input validation for the contact forms — prevents log bloat /
        /// oversized payloads being written to the server logs.
        /// </summary>
        private static string? ValidateContactInput(string? name, string? email, string? subject, string? message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
                return "Please fill in all required fields.";

            if (name.Trim().Length > 100)
                return "Name must be 100 characters or fewer.";

            if (email.Trim().Length > 100)
                return "Email must be 100 characters or fewer.";

            if (subject?.Trim().Length > 200)
                return "Subject must be 200 characters or fewer.";

            if (message.Trim().Length > 2000)
                return "Message must be 2000 characters or fewer.";

            if (!IsValidEmail(email.Trim()))
                return "Please enter a valid email address.";

            return null;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                if (email.Length < 3 || email.Length > 100) return false;
                return email.Contains('@') &&
                       new System.Net.Mail.MailAddress(email).Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// GET: /Home/Error
        /// Displays error page
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string? message = null)
        {
            ViewBag.ErrorMessage = message ?? "An unexpected error occurred.";
            ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }

        /// <summary>
        /// GET: /Home/AccessDenied
        /// Displays access denied page
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
