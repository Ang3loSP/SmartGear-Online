using SmartGear_Online.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartGear_Online.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> GetOrderByIdAsync(int id);
        Task<List<Order>> GetCustomerOrdersAsync(string customerId);
        Task<int> CreateOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task<bool> AddOrderItemAsync(OrderItem item);
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
        Task<List<Order>> GetAllOrdersAsync();
    }
}