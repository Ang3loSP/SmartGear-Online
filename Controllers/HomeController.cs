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
        /// Handles contact form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string name, string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return View();
            }

            // In production, send email or save to database
            _logger.LogInformation("Contact form submitted by {Name} ({Email}): {Subject}", name, email, subject);

            TempData["Success"] = "Thank you for contacting us. We'll get back to you soon!";
            return RedirectToAction("Index");
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