using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers.API   // Fixed namespace
{
    /// <summary>
    /// QUESTION 12: WEB API FOR PRODUCTS
    /// RESTful API endpoints for product CRUD operations
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsAPIController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductsAPIController> _logger;

        public ProductsAPIController(
            IProductRepository productRepository,
            ILogger<ProductsAPIController> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        // ================================================
        // GET: api/products
        // Returns all products (public access)
        // ================================================
        [HttpGet]
        public async Task<ActionResult<object>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("API: GetProducts called - Page {Page}, Size {PageSize}", page, pageSize);

                var products = await _productRepository.GetProductsAsync(page, pageSize);

                return Ok(new
                {
                    Success = true,
                    Data = products,
                    Count = products.Count,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error: GetProducts failed");
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // ================================================
        // GET: api/products/{id}
        // Returns single product by ID
        // ================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProduct(int id)
        {
            try
            {
                _logger.LogInformation("API: GetProduct called - ID {ProductId}", id);

                var product = await _productRepository.GetProductByIdAsync(id);

                if (product == null)
                {
                    return NotFound(new { Success = false, Message = $"Product with ID {id} not found" });
                }

                return Ok(new
                {
                    Success = true,
                    Data = product
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error: GetProduct failed for ID {ProductId}", id);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // ================================================
        // GET: api/products/search?query=keyword
        // Search products by name or description
        // ================================================
        [HttpGet("search")]
        public async Task<ActionResult<object>> SearchProducts(
            [FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { Success = false, Message = "Search query cannot be empty" });
                }

                _logger.LogInformation("API: SearchProducts called - Query '{Query}'", query);

                var products = await _productRepository.SearchProductsAsync(query);

                return Ok(new
                {
                    Success = true,
                    Data = products,
                    Count = products.Count,
                    Query = query
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error: SearchProducts failed for query '{Query}'", query);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // ================================================
        // GET: api/products/category/{category}
        // Get products by category
        // ================================================
        [HttpGet("category/{category}")]
        public async Task<ActionResult<object>> GetProductsByCategory(string category)
        {
            try
            {
                _logger.LogInformation("API: GetProductsByCategory called - Category '{Category}'", category);

                var products = await _productRepository.GetProductsByCategoryAsync(category);

                return Ok(new
                {
                    Success = true,
                    Data = products,
                    Count = products.Count,
                    Category = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error: GetProductsByCategory failed for category '{Category}'", category);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // ================================================
        // POST: api/products
        // Creates a new product (Admin only)
        // ================================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> CreateProduct([FromBody] Product product)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Invalid product data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                _logger.LogInformation("API: CreateProduct called - '{ProductName}'", product.ProductName);

                await _productRepository.AddProductAsync(product);

                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, new
                {
                    Success = true,
                    Data = product,
                    Message = "Product created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error: CreateProduct failed for '{ProductName}'", product.ProductName);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // ================================================
        // PUT: api/products/{id}
        // Updates an existing product (Admin only)
        // ================================================
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            try
            {
                if (id != product.ProductId)
                {
                    return BadRequest(new { Success = false, Message = "Product ID mismatch" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Invalid product data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                _logger.LogInformation("API: UpdateProduct called - ID {ProductId}", id);

                var existingProduct = await _productRepository.GetProductByIdAsync(id);
                if (existingProduct == null)
                {
                    return NotFound(new { Success = false, Message = $"Product with ID {id} not found" });
                }

                // Preserve original created date
                product.CreatedDate = existingProduct.CreatedDate;
                product.UpdatedDate = DateTime.UtcNow;

                await _productRepository.UpdateProductAsync(product);

                return Ok(new
                {
                    Success = true,
                    Data = product,
                    Message = "Product updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error: UpdateProduct failed for ID {ProductId}", id);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }

        // ================================================
        // DELETE: api/products/{id}
        // Deletes a product (Admin only)
        // ================================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                _logger.LogInformation("API: DeleteProduct called - ID {ProductId}", id);

                var product = await _productRepository.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new { Success = false, Message = $"Product with ID {id} not found" });
                }

                await _productRepository.DeleteProductAsync(id);

                return Ok(new
                {
                    Success = true,
                    Message = $"Product with ID {id} deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error: DeleteProduct failed for ID {ProductId}", id);
                return StatusCode(500, new { Success = false, Message = "Internal server error" });
            }
        }
    }
}