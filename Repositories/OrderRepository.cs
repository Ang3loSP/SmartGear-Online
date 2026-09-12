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
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(ApplicationDbContext context,
                             ILogger<OrderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation(
                    "OrderRepository.GetOrderByIdAsync({OrderId}) called", id);

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order");
                throw;
            }
        }

        public async Task<List<Order>> GetCustomerOrdersAsync(string customerId)
        {
            try
            {
                _logger.LogInformation(
                    "OrderRepository.GetCustomerOrdersAsync({CustomerId}) called",
                    customerId);

                var orders = await _context.Orders
                    .Where(o => o.CustomerId == customerId)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer orders");
                throw;
            }
        }

        public async Task<List<Order>> GetRecentOrdersAsync(int count)
        {
            try
            {
                _logger.LogInformation(
                    "OrderRepository.GetRecentOrdersAsync({Count}) called", count);

                return await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(Math.Max(1, count))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent orders");
                throw;
            }
        }
    }
}