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
        private const string CategoriesCacheKey = "ProductCategories";

        // FIX: generation-stamped listing keys. Search/Category/AllProducts
        // results are cached as lists; IMemoryCache can't enumerate keys, so
        // "let them expire naturally" meant admin edits kept serving stale
        // browse/search results for up to 30 minutes. Bumping the version on
        // every write atomically orphans every prior list entry — the old
        // generation's entries simply age out by size. Individual product
        // keys (Product_{id}) are still cleared explicitly.
        private int _productCacheVersion;

        private string AllProductsCacheKey => $"AllProducts_{_productCacheVersion}";
        private string SearchCacheKey(string lowerQuery) => $"Search_{_productCacheVersion}_{lowerQuery}";
        private string CategoryCacheKey(string lowerCategory) => $"Category_{_productCacheVersion}_{lowerCategory}";

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
                if (!_cache.TryGetValue(AllProductsCacheKey, out List<Product>? cachedProducts))
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
                var pagedProducts = cachedProducts!
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
        public async Task<Product?> GetProductByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("ProductRepository.GetProductByIdAsync({ProductId}) called", id);

                var cacheKey = $"Product_{id}";

                // Try to get from cache first
                if (_cache.TryGetValue(cacheKey, out Product? cachedProduct))
                {
                    _logger.LogInformation("Cache HIT - Product {ProductId} from cache", id);

                    // DATA INTEGRITY: never return a soft-deleted product from a
                    // public read-path cache entry either.
                    if (cachedProduct != null && cachedProduct.IsActive)
                        return cachedProduct;

                    _cache.Remove(cacheKey);
                }

                _logger.LogInformation("Cache MISS - Product {ProductId} from database", id);

                // Cache miss - get active product only (soft-deleted products are
                // invisible to public flows, so they can't be ordered any more).
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

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
        /// DATA INTEGRITY: reads a product including soft-deleted ones.
        /// For admin edit/management paths only — public flows must use
        /// <see cref="GetProductByIdAsync"/> so deactivated products can never
        /// be ordered from a stale link or cart.
        /// </summary>
        public async Task<Product?> GetProductByIdIncludingInactiveAsync(int id)
        {
            try
            {
                // Deliberately uncached: admin edits must always see the live row.
                return await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by ID (including inactive)");
                throw;
            }
        }

        /// <summary>
        /// True if any product already uses this name (SQL Server default
        /// collation is case-insensitive).
        /// </summary>
        public async Task<bool> ProductNameExistsAsync(string name)
        {
            try
            {
                var trimmed = name?.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    return false;

                return await _context.Products
                    .AnyAsync(p => p.ProductName == trimmed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking product name existence");
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

                var cacheKey = SearchCacheKey(query.ToLowerInvariant().Trim());

                // Try to get search results from cache
                if (_cache.TryGetValue(cacheKey, out List<Product>? cachedResults))
                {
                    _logger.LogInformation("Cache HIT - Search results for '{Query}' from cache", query);
                    return cachedResults!;
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

                var cacheKey = CategoryCacheKey(category.ToLowerInvariant());

                // Try to get from cache
                if (_cache.TryGetValue(cacheKey, out List<Product>? cachedProducts))
                {
                    _logger.LogInformation("Cache HIT - Category '{Category}' from cache", category);
                    return cachedProducts!;
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
        /// Clears product cache when inventory changes.
        /// DATA INTEGRITY: now an atomic conditional UPDATE — stock is only
        /// decremented when the row genuinely has enough, so concurrent
        /// checkouts cannot oversell.
        /// </summary>
        public async Task<bool> ReduceInventoryAsync(int productId, int quantity)
        {
            try
            {
                _logger.LogInformation("ProductRepository.ReduceInventoryAsync({ProductId}, {Quantity})", productId, quantity);

                if (quantity <= 0)
                    return false;

                var updated = await _context.Products
                    .Where(p => p.ProductId == productId && p.QuantityInStock >= quantity)
                    .ExecuteUpdateAsync(p => p.SetProperty(
                        x => x.QuantityInStock,
                        x => x.QuantityInStock - quantity));

                if (updated > 0)
                {
                    // FIX: stock is surfaced on the browse/search/category
                    // listings, so an inventory change must invalidate the
                    // listing caches as well as the single-product entry.
                    InvalidateProductCaches();
                    InvalidateProductCache(productId);
                }

                return updated > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reducing inventory");
                throw;
            }
        }

        /// <summary>
        /// DATA INTEGRITY: atomically restores stock (order cancellation).
        /// Returns the number of rows updated.
        /// </summary>
        public async Task<int> ReplenishInventoryAsync(int productId, int quantity)
        {
            try
            {
                _logger.LogInformation("ProductRepository.ReplenishInventoryAsync({ProductId}, {Quantity})", productId, quantity);

                if (quantity <= 0)
                    return 0;

                var updated = await _context.Products
                    .Where(p => p.ProductId == productId)
                    .ExecuteUpdateAsync(p => p.SetProperty(
                        x => x.QuantityInStock,
                        x => x.QuantityInStock + quantity));

                if (updated > 0)
                {
                    InvalidateProductCaches();
                    InvalidateProductCache(productId);
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replenishing inventory");
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
            // Bump the generation stamp: every active list key (all products,
            // search, category) becomes unreachable, so the next request hits
            // the database. No need to enumerate and remove each key.
            _logger.LogInformation(
                "Invalidating product listing caches (version {Version} -> {NewVersion})",
                _productCacheVersion, _productCacheVersion + 1);

            _productCacheVersion++;

            // Navigational category list still uses a fixed key, clear it directly.
            _cache.Remove(CategoriesCacheKey);
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
    }
}