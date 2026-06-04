using System.Threading.Tasks;

namespace SmartGear_Online.Services
{
    /// Question 2: Service Interface
    /// Defines contract for notification service (email, SMS)
    public interface INotificationService
    {
        Task SendOrderConfirmationEmailAsync(int orderId, string customerEmail);
        Task SendOrderStatusUpdateAsync(int orderId, string status,
                                       string customerEmail);
        Task SendLowStockAlertAsync(int productId, string productName);
    }
}