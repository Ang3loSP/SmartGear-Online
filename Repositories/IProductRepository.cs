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
        Task<Product> GetProductByIdAsync(int id);
        Task<List<Product>> SearchProductsAsync(string query);
        Task<List<Product>> GetProductsByCategoryAsync(string category);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        Task<bool> ReduceInventoryAsync(int productId, int quantity);
    }
}