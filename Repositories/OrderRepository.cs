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

        public async Task<Order> GetOrderByIdAsync(int id)
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

        public async Task<int> CreateOrderAsync(Order order)
        {
            try
            {
                if (order == null)
                    throw new ArgumentNullException(nameof(order));

                _logger.LogInformation(
                    "OrderRepository.CreateOrderAsync() called for customer {CustomerId}",
                    order.CustomerId);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Order created successfully: {OrderId}", order.OrderId);

                return order.OrderId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        public async Task UpdateOrderAsync(Order order)
        {
            try
            {
                _logger.LogInformation(
                    "OrderRepository.UpdateOrderAsync({OrderId}) called", order.OrderId);

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                throw;
            }
        }

        public async Task<bool> AddOrderItemAsync(OrderItem item)
        {
            try
            {
                _logger.LogInformation(
                    "OrderRepository.AddOrderItemAsync({OrderId}, {ProductId})",
                    item.OrderId, item.ProductId);

                _context.OrderItems.Add(item);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding order item");
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            try
            {
                _logger.LogInformation(
                    "OrderRepository.UpdateOrderStatusAsync({OrderId}, {Status})",
                    orderId, newStatus);

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    return false;

                order.Status = newStatus;
                order.UpdatedDate = DateTime.UtcNow;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return false;
            }
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            try
            {
                _logger.LogInformation("OrderRepository.GetAllOrdersAsync() called");

                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                throw;
            }
        }
    }
}