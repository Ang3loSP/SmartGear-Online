using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace SmartGear_Online.Services
{
    /// <summary>
    /// Background service that drains the email queue. It replaces the
    /// fire-and-forget Task.Run pattern in controllers: emails are now
    /// processed sequentially off the request path, and the NotificationService
    /// (scoped) is resolved per-item through its own scope.
    /// </summary>
    public class EmailWorker : BackgroundService
    {
        private readonly IEmailQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailWorker> _logger;

        public EmailWorker(
            IEmailQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<EmailWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _queue.Reader.WaitToReadAsync(stoppingToken))
            {
                while (_queue.Reader.TryRead(out var item))
                {
                    await ProcessItemAsync(item, stoppingToken);
                }
            }
        }

        private async Task ProcessItemAsync(EmailWorkItem item, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                switch (item.Type)
                {
                    case EmailType.OrderConfirmation:
                        await notifications.SendOrderConfirmationEmailAsync(item.OrderId, item.Email);
                        break;
                    case EmailType.OrderStatusUpdate:
                        if (item.Status.HasValue)
                            await notifications.SendOrderStatusUpdateAsync(item.OrderId, item.Status.Value, item.Email);
                        break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down; stop processing.
            }
            catch (Exception ex)
            {
                // One failing email must not kill the worker or stop the rest
                // of the queue from draining.
                _logger.LogError(ex, "Email worker failed to process item (type {Type}, order {OrderId})",
                    item.Type, item.OrderId);
            }
        }
    }
}