using SmartGear_Online.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartGear_Online.Repositories
{
    public interface ICartRepository
    {
        Task<List<CartItem>> GetCartItemsAsync(string userId);
        Task AddToCartAsync(CartItem item, string userId);
        Task UpdateQuantityAsync(int cartItemId, int quantity);
        Task RemoveFromCartAsync(int cartItemId);
        Task ClearCartAsync(string userId);
        Task<int> GetCartCountAsync(string userId);
    }
}