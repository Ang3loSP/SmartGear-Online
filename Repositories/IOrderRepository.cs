using SmartGear_Online.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartGear_Online.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderByIdAsync(int id);
        Task<List<Order>> GetCustomerOrdersAsync(string customerId);

        /// <summary>
        /// The most recent <paramref name="count"/> orders (with customer +
        /// items) — used by the dashboard so it never has to load every order
        /// just to render the "recent orders" panel.
        /// </summary>
        Task<List<Order>> GetRecentOrdersAsync(int count);
    }
}