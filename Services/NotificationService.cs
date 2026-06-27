using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SmartGear_Online.Services
{
    /// Question 2: Service Implementation
    /// Implements INotificationService
    /// Injected into controllers that need to send notifications
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public async Task SendOrderConfirmationEmailAsync(int orderId,
                                                         string customerEmail)
        {
            try
            {
                _logger.LogInformation(
                    "Sending order confirmation email for Order #{OrderId} to {Email}",
                    orderId, customerEmail);

                // TODO: Integrate with actual email service (SendGrid, Mailgun, etc.)
                // For now, just log
                await Task.Delay(100); // Simulate async operation

                _logger.LogInformation("Order confirmation email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending order confirmation email for Order #{OrderId}",
                    orderId);
                throw;
            }
        }

        public async Task SendOrderStatusUpdateAsync(int orderId, string status,
                                                     string customerEmail)
        {
            try
            {
                _logger.LogInformation(
                    "Sending status update email for Order #{OrderId}: {Status}",
                    orderId, status);

                await Task.Delay(100); // Simulate async operation

                _logger.LogInformation("Status update email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending status update email for Order #{OrderId}",
                    orderId);
                throw;
            }
        }

        public async Task SendLowStockAlertAsync(int productId,
                                                string productName)
        {
            try
            {
                _logger.LogWarning(
                    "Sending low stock alert for Product #{ProductId}: {ProductName}",
                    productId, productName);

                await Task.Delay(100); // Simulate async operation

                _logger.LogWarning("Low stock alert sent to admins");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending low stock alert for Product #{ProductId}",
                    productId);
                throw;
            }
        }
    }
}