using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Data;
using SmartGear_Online.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartRepository> _logger;

        public CartRepository(ApplicationDbContext context, ILogger<CartRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CartItem>> GetCartItemsAsync(string userId)
        {
            return await Task.FromResult(new List<CartItem>());
        }

        public async Task AddToCartAsync(CartItem item, string userId)
        {
            await Task.CompletedTask;
        }

        public async Task UpdateQuantityAsync(int cartItemId, int quantity)
        {
            await Task.CompletedTask;
        }

        public async Task RemoveFromCartAsync(int cartItemId)
        {
            await Task.CompletedTask;
        }

        public async Task ClearCartAsync(string userId)
        {
            await Task.CompletedTask;
        }

        public async Task<int> GetCartCountAsync(string userId)
        {
            return await Task.FromResult(0);
        }
    }
}
