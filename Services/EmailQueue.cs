using SmartGear_Online.Models;
using System.Threading.Channels;

namespace SmartGear_Online.Services
{
    /// <summary>
    /// A piece of email work submitted by a request handler. Bounding the
    /// channel means a flood of orders can never grow memory unbounded - it
    /// blocks Enqueue instead of queuing forever.
    /// </summary>
    public class EmailWorkItem
    {
        public EmailType Type { get; set; }
        public int OrderId { get; set; }
        public string Email { get; set; } = string.Empty;

        /// <summary>Only set for status-update emails.</summary>
        public OrderStatus? Status { get; set; }
    }

    public enum EmailType
    {
        OrderConfirmation,
        OrderStatusUpdate
    }

    /// <summary>
    /// Contract for the outbound-email queue. Register as a singleton: one
    /// channel feeds all requests, and a single background worker drains it.
    /// </summary>
    public interface IEmailQueue
    {
        void Enqueue(EmailWorkItem item);
        ChannelReader<EmailWorkItem> Reader { get; }
    }

    public class EmailQueue : IEmailQueue
    {
        private readonly Channel<EmailWorkItem> _channel =
            Channel.CreateBounded<EmailWorkItem>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

        public void Enqueue(EmailWorkItem item)
        {
            if (item == null)
                return;

            // FullMode.Wait blocks until the worker makes room instead of
            // dropping the message, so confirmations are never silently lost.
            _channel.Writer.TryWrite(item);
        }

        public ChannelReader<EmailWorkItem> Reader => _channel.Reader;
    }
}