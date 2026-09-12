using SmartGear_Online.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartGear_Online.Repositories
{
    /// Question 6: Repository Interface
    /// Defines contract for data access operations
    /// Allows dependency injection & easy testing
    public interface IProductRepository
    {
        Task<List<Product>> GetProductsAsync(int page, int pageSize);

        /// <summary>
        /// Total number of active (not soft-deleted) products. Used to build
        /// real server-side pagination on the catalog, so the pager links to
        /// pages that actually exist.
        /// </summary>
        Task<int> GetProductCountAsync();

        /// <summary>
        /// Returns an active (not soft-deleted) product, or null. Used for
        /// public-facing flows so deactivated products can no longer be
        /// added to carts or ordered.
        /// </summary>
        Task<Product?> GetProductByIdAsync(int id);

        /// <summary>
        /// Returns a product even if it has been soft-deleted (IsActive =
        /// false). Intended for admin edit/management paths only.
        /// </summary>
        Task<Product?> GetProductByIdIncludingInactiveAsync(int id);

        /// <summary>
        /// True if any product already uses this name (case-insensitive match
        /// under SQL Server collation). Used to give duplicate-name submissions
        /// a friendly validation error instead of a 500.
        /// </summary>
        Task<bool> ProductNameExistsAsync(string name);

        Task<List<Product>> SearchProductsAsync(string query);
        Task<List<Product>> GetProductsByCategoryAsync(string category);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
    }
}