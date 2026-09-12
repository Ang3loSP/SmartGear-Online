using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Data;
using SmartGear_Online.Filters;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    /// Question 3: Controller for Processing Product Requests
    /// Handles: GET (list, details), POST (create), PUT (update), DELETE
    [Route("[controller]")]
    [ServiceFilter(typeof(LoggingActionFilter))] // Apply logging filter to all actions
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductRepository productRepository,
                               ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        // =====================================================
        // READ ACTIONS
        // =====================================================

        /// <summary>
        /// GET: /product (all products with pagination)
        /// QUESTION 11.3: Response caching for 5 minutes (300 seconds)
        /// Caches different pages separately using VaryByQueryKeys
        /// </summary>
        [HttpGet("")]
        [Route("Index")]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "page", "pageSize" })]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
        {
            try
            {
                // Clamp the URL-supplied bounds: a negative/zero page would
                // make Skip() throw, and a huge pageSize would load (and cache)
                // the whole catalogue keyed on a giant string.
                if (page < 1) page = 1;
                pageSize = Math.Clamp(pageSize, 1, 48);

                // Real server-side pagination: the pager links to pages that
                // actually exist, and an out-of-range page is clamped instead
                // of 404ing or rendering an empty grid.
                var totalProducts = await _productRepository.GetProductCountAsync();
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalProducts / (double)pageSize));
                if (page > totalPages) page = totalPages;

                _logger.LogInformation("ProductController.Index() called with page={Page}", page);

                var products = await _productRepository.GetProductsAsync(page, pageSize);

                ViewData["Page"] = page;
                ViewData["PageSize"] = pageSize;
                ViewData["TotalPages"] = totalPages;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// GET: /product/details/5 (single product)
        /// QUESTION 11.3: Response caching for 10 minutes (600 seconds)
        /// Note: For route parameters, use VaryByQueryKeys with parameter name
        /// </summary>
        [HttpGet("details/{id}")]
        [Route("Details")]
        [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation("ProductController.Details() called with id={ProductId}", id);

                if (id <= 0)
                    return BadRequest("Invalid product ID");

                var product = await _productRepository.GetProductByIdAsync(id);

                if (product == null)
                    return NotFound("Product not found");

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product details");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// GET: /product/search?query=jersey (search functionality)
        /// QUESTION 11.3: Short cache duration for search results (2 minutes)
        /// </summary>
        [HttpGet("search")]
        [ResponseCache(Duration = 120, VaryByQueryKeys = new[] { "query" })]
        public async Task<IActionResult> Search(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Search query cannot be empty");

                _logger.LogInformation("ProductController.Search() called with query={Query}", query);

                var products = await _productRepository.SearchProductsAsync(query);

                return View("Index", products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// GET: /product/searchsuggestions?query=jer
        /// FIX: this action was missing entirely. site.js had a live
        /// keyup handler calling it on every keystroke in the search box,
        /// generating a silent 404 each time. Returns a small JSON array
        /// of matching product names for a lightweight autocomplete.
        /// </summary>
        [HttpGet("searchsuggestions")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "query" })]
        public async Task<IActionResult> SearchSuggestions(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                    return Json(new List<string>());

                var products = await _productRepository.SearchProductsAsync(query);

                var suggestions = products
                    .Take(5)
                    .Select(p => p.ProductName)
                    .ToList();

                return Json(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving search suggestions");
                return Json(new List<string>());
            }
        }

        /// <summary>
        /// GET: /product/category/jerseys (filter by category)
        /// QUESTION 11.3: Response caching for 30 minutes
        /// For route parameters, use VaryByQueryKeys - they work with route values too
        /// </summary>
        [HttpGet("category/{categoryName}")]
        [ResponseCache(Duration = 1800, VaryByQueryKeys = new[] { "categoryName" })]
        public async Task<IActionResult> GetByCategory(string categoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                    return BadRequest("Category name cannot be empty");

                _logger.LogInformation(
                    "ProductController.GetByCategory() called with category={Category}",
                    categoryName);

                var products = await _productRepository.GetProductsByCategoryAsync(categoryName);

                return View("Index", products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering by category");
                return StatusCode(500, "Internal server error");
            }
        }

        // =====================================================
        // CREATE ACTION (Admin only) - NO CACHE
        // =====================================================

        /// GET: /product/create (show create form)
        [HttpGet("create")]
        [Authorize(Roles = "Admin")]
        [ResponseCache(NoStore = true, Duration = 0)]
        public IActionResult Create()
        {
            _logger.LogInformation("ProductController.Create() GET called");
            return View();
        }

        /// POST: /product/create (save new product)
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Create(Product product)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ProductController.Create() POST: Invalid model state");
                    return View(product);
                }

                // DATA INTEGRITY: friendly duplicate-name check instead of a 500
                // from the unique index.
                if (await _productRepository.ProductNameExistsAsync(product.ProductName))
                {
                    ModelState.AddModelError("ProductName",
                        "A product with this name already exists. Choose a different name.");
                    return View(product);
                }

                _logger.LogInformation(
                    "ProductController.Create() POST: Creating product {ProductName}",
                    product.ProductName);

                await _productRepository.AddProductAsync(product);

                _logger.LogInformation("Product created successfully: {ProductId}", product.ProductId);

                return RedirectToAction("Details", new { id = product.ProductId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, "Error creating product");
            }
        }

        // =====================================================
        // UPDATE ACTION (Admin only) - NO CACHE
        // =====================================================

        /// GET: /product/edit/5 (show edit form)
        [HttpGet("edit/{id}")]
        [Authorize(Roles = "Admin")]
        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                _logger.LogInformation("ProductController.Edit() GET called with id={ProductId}", id);

                // Admin edit path: must be able to open soft-deleted products too.
                var product = await _productRepository.GetProductByIdIncludingInactiveAsync(id);

                if (product == null)
                    return NotFound("Product not found");

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product for edit");
                return StatusCode(500, "Internal server error");
            }
        }

        /// POST: /product/edit/5 (save changes)
        [HttpPost("edit/{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            try
            {
                if (id != product.ProductId)
                {
                    _logger.LogWarning("ProductController.Edit() POST: ID mismatch");
                    return BadRequest("Product ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ProductController.Edit() POST: Invalid model state");
                    return View(product);
                }

                // DATA INTEGRITY: reload the existing row so we can (a) confirm it
                // still exists, (b) preserve CreatedDate (the edit form doesn't
                // carry it, so full-entity binding would silently reset it to now),
                // and (c) reject renames that collide with an existing product.
                var existing = await _productRepository.GetProductByIdIncludingInactiveAsync(id);

                if (existing == null)
                    return NotFound("Product not found");

                var nameExists = await _productRepository.ProductNameExistsAsync(product.ProductName);
                if (nameExists && !string.Equals(product.ProductName.Trim(), existing.ProductName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("ProductName",
                        "A product with this name already exists. Choose a different name.");
                    return View(product);
                }

                product.CreatedDate = existing.CreatedDate;

                _logger.LogInformation(
                    "ProductController.Edit() POST: Updating product {ProductId}",
                    id);

                await _productRepository.UpdateProductAsync(product);

                _logger.LogInformation("Product updated successfully: {ProductId}", id);

                return RedirectToAction("Details", new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return StatusCode(500, "Error updating product");
            }
        }

        // =====================================================
        // DELETE ACTION (Admin only) - NO CACHE
        // =====================================================

        /// POST: /product/delete/5 (delete product)
        [HttpPost("delete/{id}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation(
                    "ProductController.Delete() called with id={ProductId}",
                    id);

                var product = await _productRepository.GetProductByIdIncludingInactiveAsync(id);

                if (product == null)
                    return NotFound("Product not found");

                await _productRepository.DeleteProductAsync(id);

                _logger.LogInformation("Product deleted successfully: {ProductId}", id);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return StatusCode(500, "Error deleting product");
            }
        }
    }
}
