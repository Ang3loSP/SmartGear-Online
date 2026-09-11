using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SmartGear_Online.Hubs
{
    /// <summary>
    /// Real-time inventory updates hub.
    /// Notifies admins when inventory changes or stock is low.
    /// </summary>
    [Authorize]
    public class InventoryHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

            if (isAdmin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }

        public async Task NotifyStockChange(int productId, string productName, int newQuantity)
        {
            await Clients.Group("Admins").SendAsync("StockUpdated", new
            {
                ProductId = productId,
                ProductName = productName,
                NewQuantity = newQuantity,
                Timestamp = System.DateTime.UtcNow
            });
        }

        public async Task NotifyLowStock(int productId, string productName, int currentStock)
        {
            await Clients.Group("Admins").SendAsync("LowStockAlert", new
            {
                ProductId = productId,
                ProductName = productName,
                CurrentStock = currentStock,
                Timestamp = System.DateTime.UtcNow
            });
        }
    }
}
