using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Hubs
{
    /// <summary>
    /// QUESTION 11.5 & 11.6: SignalR Hub for real-time chat
    /// Features:
    /// - Real-time message sending (11.5)
    /// - Typing indicators (11.5)
    /// - Active user tracking (11.6)
    /// - Admin notifications when customers connect (11.6)
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        // Track connected users with their connection IDs and roles
        private static readonly Dictionary<string, UserConnection> _connectedUsers = new();

        // Track typing status
        private static readonly Dictionary<string, DateTime> _typingUsers = new();

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        // ================================================
        // QUESTION 11.5 & 11.6: CONNECTION HANDLING
        // ================================================

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";
            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

            // Store user connection info
            _connectedUsers[Context.ConnectionId] = new UserConnection
            {
                UserId = userId,
                UserName = userName,
                ConnectionId = Context.ConnectionId,
                IsAdmin = isAdmin,
                ConnectedAt = DateTime.Now
            };

            _logger.LogInformation("User {UserName} connected. ConnectionId: {ConnectionId}. IsAdmin: {IsAdmin}",
                userName, Context.ConnectionId, isAdmin);

            // QUESTION 11.6: Notify admins when a customer connects
            if (!isAdmin)
            {
                await Clients.Group("Admins").SendAsync("UserConnected", new
                {
                    UserName = userName,
                    Message = $"{userName} has joined the chat",
                    Timestamp = DateTime.Now.ToString("HH:mm:ss")
                });
            }

            // Add to appropriate group for targeted messaging
            if (isAdmin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");

                // QUESTION 11.6: Send active users list to new admin
                await SendActiveUsersToAdmin(Context.ConnectionId);
            }

            // QUESTION 11.6: Broadcast updated user count to all
            await Clients.All.SendAsync("UserCountUpdated", GetActiveUserCount());

            await base.OnConnectedAsync();
        }

        // ================================================
        // FIXED: Added ? to Exception parameter (Error 8 fix)
        // ================================================
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectedUsers.TryGetValue(Context.ConnectionId, out var user))
            {
                var userName = user.UserName;
                var isAdmin = user.IsAdmin;

                _connectedUsers.Remove(Context.ConnectionId);
                _typingUsers.Remove(user.UserId);

                _logger.LogInformation("User {UserName} disconnected", userName);

                // QUESTION 11.6: Notify admins when a customer disconnects
                if (!isAdmin)
                {
                    await Clients.Group("Admins").SendAsync("UserDisconnected", new
                    {
                        UserName = userName,
                        Message = $"{userName} has left the chat",
                        Timestamp = DateTime.Now.ToString("HH:mm:ss")
                    });
                }

                // QUESTION 11.6: Broadcast updated user count
                await Clients.All.SendAsync("UserCountUpdated", GetActiveUserCount());

                // QUESTION 11.6: Update active users list for admins
                await UpdateActiveUsersList();
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ================================================
        // QUESTION 11.5: REAL-TIME MESSAGE SENDING
        // ================================================

        /// <summary>
        /// Sends a message to all connected clients in real-time
        /// QUESTION 11.5: Core real-time messaging functionality
        /// </summary>
        public async Task SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

            _logger.LogInformation("Message from {UserName}: {Message}", userName, message);

            // Prevent spam - limit message length
            if (message.Length > 500)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Message too long (max 500 characters)");
                return;
            }

            var messageData = new ChatMessage
            {
                UserName = userName,
                Message = message,
                Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                IsAdmin = isAdmin,
                UserId = userId
            };

            // Broadcast to ALL connected clients (customers and admins)
            await Clients.All.SendAsync("ReceiveMessage", messageData);
        }

        // ================================================
        // QUESTION 11.5: TYPING INDICATOR
        // ================================================

        /// <summary>
        /// Shows when a user is typing in real-time
        /// QUESTION 11.5: Typing indicator functionality
        /// </summary>
        public async Task UserTyping(string userName, bool isTyping)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return;

            var userId = Context.UserIdentifier;

            if (isTyping)
            {
                _typingUsers[userId] = DateTime.Now;

                // Auto-clear typing indicator after 3 seconds of no typing
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    if (_typingUsers.TryGetValue(userId, out var lastTyping) &&
                        (DateTime.Now - lastTyping).TotalSeconds >= 3)
                    {
                        _typingUsers.Remove(userId);
                        await Clients.All.SendAsync("UserTyping", userName, false);
                    }
                });
            }
            else
            {
                _typingUsers.Remove(userId);
            }

            await Clients.All.SendAsync("UserTyping", userName, isTyping);
        }

        // ================================================
        // QUESTION 11.6: PRIVATE MESSAGING (Admin to Customer)
        // ================================================

        /// <summary>
        /// Send a private message to a specific user
        /// QUESTION 11.6: Private messaging between admin and customer
        /// </summary>
        public async Task SendPrivateMessage(string targetUserId, string message)
        {
            var senderName = Context.User?.Identity?.Name ?? "Anonymous";
            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

            // Only admins can initiate private messages (or users can message admins)
            var targetConnection = _connectedUsers.Values
                .FirstOrDefault(u => u.UserId == targetUserId);

            if (targetConnection != null)
            {
                var privateMessage = new ChatMessage
                {
                    UserName = senderName,
                    Message = message,
                    Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                    IsAdmin = isAdmin,
                    IsPrivate = true,
                    UserId = Context.UserIdentifier
                };

                await Clients.Client(targetConnection.ConnectionId)
                    .SendAsync("ReceivePrivateMessage", privateMessage);

                // Also send confirmation to sender
                await Clients.Caller.SendAsync("PrivateMessageSent", privateMessage);
            }
        }

        // ================================================
        // QUESTION 11.6: GET ACTIVE USERS (Admin only)
        // ================================================

        /// <summary>
        /// Returns list of currently active users
        /// QUESTION 11.6: Active user list for admin dashboard
        /// </summary>
        public async Task<List<ActiveUserInfo>> GetActiveUsers()
        {
            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

            if (!isAdmin)
                return new List<ActiveUserInfo>();

            var activeUsers = _connectedUsers.Values
                .Where(u => !u.IsAdmin)
                .Select(u => new ActiveUserInfo
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    ConnectedAt = u.ConnectedAt,
                    IsTyping = _typingUsers.ContainsKey(u.UserId)
                })
                .ToList();

            return await Task.FromResult(activeUsers);
        }

        // ================================================
        // QUESTION 11.6: HELPER METHODS
        // ================================================

        private int GetActiveUserCount()
        {
            return _connectedUsers.Count;
        }

        private async Task SendActiveUsersToAdmin(string adminConnectionId)
        {
            var activeUsers = _connectedUsers.Values
                .Where(u => !u.IsAdmin)
                .Select(u => new
                {
                    u.UserName,
                    u.ConnectedAt,
                    IsTyping = _typingUsers.ContainsKey(u.UserId)
                })
                .ToList();

            await Clients.Client(adminConnectionId).SendAsync("ActiveUsersList", activeUsers);
        }

        private async Task UpdateActiveUsersList()
        {
            var activeUsers = _connectedUsers.Values
                .Where(u => !u.IsAdmin)
                .Select(u => new
                {
                    u.UserName,
                    u.ConnectedAt,
                    IsTyping = _typingUsers.ContainsKey(u.UserId)
                })
                .ToList();

            await Clients.Group("Admins").SendAsync("ActiveUsersList", activeUsers);
        }
    }

    // ================================================
    // DTO CLASSES FOR SIGNALR MESSAGES
    // ================================================

    public class UserConnection
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public DateTime ConnectedAt { get; set; }
    }

    public class ChatMessage
    {
        public string UserName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = false;
    }

    public class ActiveUserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public bool IsTyping { get; set; }
        public string ConnectedAtDisplay => ConnectedAt.ToString("HH:mm:ss");
    }
}