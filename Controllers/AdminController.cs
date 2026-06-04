using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    /// QUESTION 10: ADMIN CONTROLLER WITH ROLE AUTHORIZATION
    /// Only users with Admin role can access these actions
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;

        public AdminController(IProductRepository productRepository,
                               IOrderRepository orderRepository)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
        }

        // ================================================
        // DASHBOARD - Admin only
        // ================================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // Admin dashboard content
            return View();
        }

        // ================================================
        // MANAGE PRODUCTS - Admin only
        // ================================================
        [HttpGet]
        public async Task<IActionResult> ManageProducts()
        {
            var products = await _productRepository.GetProductsAsync(1, 100);
            return View(products);
        }

        // ================================================
        // ADD PRODUCT - Admin only with CSRF protection
        // ================================================
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]  // CSRF Protection
        public async Task<IActionResult> AddProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                await _productRepository.AddProductAsync(product);
                return RedirectToAction("ManageProducts");
            }
            return View(product);
        }

        // ================================================
        // DELETE PRODUCT - Admin only with CSRF protection
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _productRepository.DeleteProductAsync(id);
            return RedirectToAction("ManageProducts");
        }
    }
}