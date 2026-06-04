using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SmartGear_Online.Hubs
{
    /// QUESTION 11: REAL-TIME INVENTORY UPDATES
    /// Notifies admins when inventory changes
    [Authorize]
    public class InventoryHub : Hub
    {
        public async Task NotifyStockChange(int productId, string productName, int newQuantity)
        {
            await Clients.Group("Admins").SendAsync("StockUpdated", new
            {
                ProductId = productId,
                ProductName = productName,
                NewQuantity = newQuantity,
                Timestamp = System.DateTime.Now
            });
        }

        public async Task NotifyLowStock(int productId, string productName, int currentStock)
        {
            await Clients.Group("Admins").SendAsync("LowStockAlert", new
            {
                ProductId = productId,
                ProductName = productName,
                CurrentStock = currentStock,
                Timestamp = System.DateTime.Now
            });
        }
    }
}