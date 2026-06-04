using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Data;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Repositories
{
    /// <summary>
    /// Question 6: Repository Implementation
    /// Encapsulates database queries & CRUD operations
    /// Injected into controllers & services
    /// </summary>
    /// <summary>
    /// Question 11.2: Added IMemoryCache for caching product listings
    /// This improves performance by reducing database calls
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductRepository> _logger;

        // ================================================
        // QUESTION 11.2: IN-MEMORY CACHE
        // ================================================
        private readonly IMemoryCache _cache;

        // Cache keys
        private const string AllProductsCacheKey = "AllProducts";
        private const string CategoriesCacheKey = "ProductCategories";

        // Cache expiration times
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan ShortCacheExpiration = TimeSpan.FromMinutes(5);

        public ProductRepository(ApplicationDbContext context,
                               ILogger<ProductRepository> logger,
                               IMemoryCache cache)  // QUESTION 11.2: Added IMemoryCache dependency
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        // =====================================================
        // READ OPERATIONS WITH CACHING (QUESTION 11.2)
        // =====================================================

        /// <summary>
        /// QUESTION 11.2: GetProductsAsync with caching
        /// Products are cached for 30 minutes to reduce database calls
        /// </summary>
        public async Task<List<Product>> GetProductsAsync(int page, int pageSize)
        {
            try
            {
                _logger.LogInformation("ProductRepository.GetProductsAsync() called");

                // Try to get ALL products from cache first
                if (!_cache.TryGetValue(AllProductsCacheKey, out List<Product> cachedProducts))
                {
                    _logger.LogInformation("Cache MISS - Fetching products from database");

                    // Cache miss - get ALL active products from database
                    cachedProducts = await _context.Products
                        .Where(p => p.IsActive)
                        .OrderBy(p => p.ProductName)
                        .ToListAsync();

                    // Configure cache options
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(CacheExpiration)     // Reset expiration if accessed
                        .SetAbsoluteExpiration(TimeSpan.FromHours(1)) // Max 1 hour
                        .SetPriority(CacheItemPriority.High)       // High priority - don't evict easily
                        .RegisterPostEvictionCallback((key, value, reason, state) =>
                        {
                            _logger.LogInformation($"Cache entry '{key}' evicted. Reason: {reason}");
                        });

                    // Store in cache
                    _cache.Set(AllProductsCacheKey, cachedProducts, cacheOptions);

                    _logger.LogInformation("Products cached successfully. Count: {Count}", cachedProducts.Count);
                }
                else
                {
                    _logger.LogInformation("Cache HIT - Returning products from cache");
                }

                // Apply pagination AFTER retrieving from cache
                var pagedProducts = cachedProducts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return pagedProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                throw;
            }
        }

        /// <summary>
        /// QUESTION 11.2: GetProductByIdAsync with caching
        /// Individual products are cached for 30 minutes
        /// </summary>
        public async Task<Product> GetProductByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("ProductRepository.GetProductByIdAsync({ProductId}) called", id);

                var cacheKey = $"Product_{id}";

                // Try to get from cache first
                if (_cache.TryGetValue(cacheKey, out Product cachedProduct))
                {
                    _logger.LogInformation("Cache HIT - Product {ProductId} from cache", id);
                    return cachedProduct;
                }

                _logger.LogInformation("Cache MISS - Product {ProductId} from database", id);

                // Cache miss - get from database
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product != null)
                {
                    // Cache individual product
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(CacheExpiration)
                        .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                    _cache.Set(cacheKey, product, cacheOptions);
                    _logger.LogInformation("Product {ProductId} cached successfully", id);
                }

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by ID");
                throw;
            }
        }

        /// <summary>
        /// QUESTION 11.2: SearchProductsAsync with caching for frequent searches
        /// Search results are cached for 5 minutes
        /// </summary>
        public async Task<List<Product>> SearchProductsAsync(string query)
        {
            try
            {
                _logger.LogInformation("ProductRepository.SearchProductsAsync({Query}) called", query);

                if (string.IsNullOrWhiteSpace(query))
                    return new List<Product>();

                var cacheKey = $"Search_{query.ToLower().Trim()}";

                // Try to get search results from cache
                if (_cache.TryGetValue(cacheKey, out List<Product> cachedResults))
                {
                    _logger.LogInformation("Cache HIT - Search results for '{Query}' from cache", query);
                    return cachedResults;
                }

                _logger.LogInformation("Cache MISS - Searching database for '{Query}'", query);

                // Cache miss - search database
                var products = await _context.Products
                    .Where(p => p.IsActive &&
                           (p.ProductName.Contains(query) ||
                            p.Description.Contains(query)))
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                // Cache search results for 5 minutes (short expiration)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(ShortCacheExpiration)
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                _cache.Set(cacheKey, products, cacheOptions);
                _logger.LogInformation("Search results for '{Query}' cached. Count: {Count}", query, products.Count);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                throw;
            }
        }

        /// <summary>
        /// QUESTION 11.2: GetProductsByCategoryAsync with caching
        /// Category results are cached for 30 minutes
        /// </summary>
        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            try
            {
                _logger.LogInformation("ProductRepository.GetProductsByCategoryAsync({Category}) called", category);

                var cacheKey = $"Category_{category.ToLower()}";

                // Try to get from cache
                if (_cache.TryGetValue(cacheKey, out List<Product> cachedProducts))
                {
                    _logger.LogInformation("Cache HIT - Category '{Category}' from cache", category);
                    return cachedProducts;
                }

                _logger.LogInformation("Cache MISS - Fetching category '{Category}' from database", category);

                // Cache miss - get from database
                var products = await _context.Products
                    .Where(p => p.IsActive && p.Category.ToLower() == category.ToLower())
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                // Cache the results
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(CacheExpiration)
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set(cacheKey, products, cacheOptions);
                _logger.LogInformation("Category '{Category}' cached. Count: {Count}", category, products.Count);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by category");
                throw;
            }
        }

        // =====================================================
        // CREATE OPERATION WITH CACHE INVALIDATION
        // =====================================================

        /// <summary>
        /// QUESTION 11.2: AddProductAsync with cache invalidation
        /// Clears related caches when data changes
        /// </summary>
        public async Task AddProductAsync(Product product)
        {
            try
            {
                if (product == null)
                    throw new ArgumentNullException(nameof(product));

                _logger.LogInformation("ProductRepository.AddProductAsync({ProductName}) called", product.ProductName);

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // QUESTION 11.2: Invalidate caches when data changes
                InvalidateProductCaches();

                _logger.LogInformation("Product added successfully: {ProductId}. Caches invalidated.", product.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                throw;
            }
        }

        // =====================================================
        // UPDATE OPERATION WITH CACHE INVALIDATION
        // =====================================================

        /// <summary>
        /// QUESTION 11.2: UpdateProductAsync with cache invalidation
        /// Clears related caches when data changes
        /// </summary>
        public async Task UpdateProductAsync(Product product)
        {
            try
            {
                if (product == null)
                    throw new ArgumentNullException(nameof(product));

                _logger.LogInformation("ProductRepository.UpdateProductAsync({ProductId}) called", product.ProductId);

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                // QUESTION 11.2: Invalidate caches when data changes
                InvalidateProductCaches();
                // Also invalidate the specific product cache
                InvalidateProductCache(product.ProductId);

                _logger.LogInformation("Product updated successfully: {ProductId}. Caches invalidated.", product.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                throw;
            }
        }

        // =====================================================
        // DELETE OPERATION WITH CACHE INVALIDATION
        // =====================================================

        /// <summary>
        /// QUESTION 11.2: DeleteProductAsync with cache invalidation
        /// Clears related caches when data changes
        /// </summary>
        public async Task DeleteProductAsync(int id)
        {
            try
            {
                _logger.LogInformation("ProductRepository.DeleteProductAsync({ProductId}) called", id);

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                    throw new KeyNotFoundException("Product not found");

                // Soft delete: mark as inactive instead of removing
                product.IsActive = false;
                product.UpdatedDate = DateTime.UtcNow;

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                // QUESTION 11.2: Invalidate caches when data changes
                InvalidateProductCaches();
                InvalidateProductCache(id);

                _logger.LogInformation("Product deleted successfully: {ProductId}. Caches invalidated.", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                throw;
            }
        }

        // =====================================================
        // BUSINESS OPERATIONS WITH CACHE INVALIDATION
        // =====================================================

        /// <summary>
        /// QUESTION 11.2: ReduceInventoryAsync with cache invalidation
        /// Clears product cache when inventory changes
        /// </summary>
        public async Task<bool> ReduceInventoryAsync(int productId, int quantity)
        {
            try
            {
                _logger.LogInformation("ProductRepository.ReduceInventoryAsync({ProductId}, {Quantity})", productId, quantity);

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                    throw new KeyNotFoundException("Product not found");

                if (!product.CanFulfillOrder(quantity))
                    return false;

                product.ReduceInventory(quantity);
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                // QUESTION 11.2: Invalidate specific product cache when inventory changes
                InvalidateProductCache(productId);

                _logger.LogInformation("Inventory reduced for Product {ProductId}. Cache invalidated.", productId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reducing inventory");
                throw;
            }
        }

        // =====================================================
        // QUESTION 11.2: CACHE INVALIDATION METHODS
        // =====================================================

        /// <summary>
        /// Invalidates all product-related caches
        /// Called when products are added, updated, or deleted
        /// </summary>
        private void InvalidateProductCaches()
        {
            _logger.LogInformation("Invalidating all product caches");

            // Remove the main products cache
            _cache.Remove(AllProductsCacheKey);

            // Remove categories cache
            _cache.Remove(CategoriesCacheKey);

            // Note: For category-specific caches, we'd need to track them
            // For simplicity, we'll let them expire naturally
        }

        /// <summary>
        /// Invalidates cache for a specific product
        /// Called when a product is updated or inventory changes
        /// </summary>
        private void InvalidateProductCache(int productId)
        {
            var cacheKey = $"Product_{productId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Invalidated cache for Product {ProductId}", productId);
        }

        // =====================================================
        // QUESTION 11.2: CACHE MANAGEMENT METHODS
        // =====================================================

        /// <summary>
        /// Manually refresh the product cache
        /// Can be called by admin to force cache refresh
        /// </summary>
        public async Task RefreshProductCacheAsync()
        {
            _logger.LogInformation("Manually refreshing product cache");

            // Invalidate existing cache
            InvalidateProductCaches();

            // Force reload from database on next request
            _cache.Remove(AllProductsCacheKey);

            // Preload cache with fresh data
            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(CacheExpiration)
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _cache.Set(AllProductsCacheKey, products, cacheOptions);

            _logger.LogInformation("Product cache refreshed. Count: {Count}", products.Count);
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        public Dictionary<string, object> GetCacheStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                { "AllProductsCached", _cache.TryGetValue(AllProductsCacheKey, out _) },
                { "CategoriesCached", _cache.TryGetValue(CategoriesCacheKey, out _) },
                { "CacheExpirationMinutes", CacheExpiration.TotalMinutes },
                { "ShortCacheExpirationMinutes", ShortCacheExpiration.TotalMinutes }
            };

            return stats;
        }
    }
}