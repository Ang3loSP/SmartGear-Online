using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmartGear_Online.Models;
using System.Threading.Tasks;

namespace SmartGear_Online.Services
{
    /// Question 2: Service Implementation
    /// Implements INotificationService
    /// Injected into controllers that need to send notifications.
    /// Emails are delivered over real SMTP when the "Email" configuration
    /// section is populated; otherwise they are logged (safe local default).
    public class NotificationService : INotificationService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IOptions<EmailOptions> options,
                                   ILogger<NotificationService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendOrderConfirmationEmailAsync(int orderId,
                                                          string customerEmail)
        {
            await SendAsync(
                orderId,
                customerEmail,
                $"Order #{orderId} confirmed \u2705",
                $"Hi,\r\n\r\nThank you for shopping with SmartGear Online.\r\n\r\n" +
                $"Your order #{orderId} has been confirmed and is now being prepared.\r\n\r\n" +
                "Thanks,\r\nThe SmartGear Online team");
        }

        public async Task SendOrderStatusUpdateAsync(int orderId, OrderStatus status,
                                                     string customerEmail)
        {
            await SendAsync(
                orderId,
                customerEmail,
                $"Order #{orderId}: status update",
                $"Hi,\r\n\r\nYour order #{orderId} status has changed to: {status}.\r\n\r\n" +
                "Thanks,\r\nThe SmartGear Online team");
        }

        private async Task SendAsync(int orderId, string customerEmail,
                                     string subject, string body)
        {
            try
            {
                // No SMTP host configured -> log only so local/dev runs stay
                // green without a mail server.
                if (string.IsNullOrWhiteSpace(_options.Host))
                {
                    _logger.LogInformation(
                        "Email disabled (no SMTP host configured) - would send " +
                        "'{Subject}' for Order #{OrderId} to {Email}",
                        subject, orderId, customerEmail);
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
                message.To.Add(MailboxAddress.Parse(customerEmail));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _options.Host,
                    _options.Port,
                    _options.UseSsl
                        ? SecureSocketOptions.StartTlsWhenAvailable
                        : SecureSocketOptions.Auto);

                if (!string.IsNullOrEmpty(_options.UserName))
                {
                    await client.AuthenticateAsync(_options.UserName, _options.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation(
                    "Sent '{Subject}' to {Email} (Order #{OrderId})",
                    subject, customerEmail, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending email for Order #{OrderId}", orderId);
                throw;
            }
        }
    }
}