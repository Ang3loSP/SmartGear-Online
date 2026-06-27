using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartGear_Online.Hubs
{
    /// <summary>
    /// QUESTION 11.5 &amp; 11.6: SignalR Hub for real-time chat.
    /// FIX: _connectedUsers and _typingUsers are now ConcurrentDictionary
    /// to prevent race conditions from the background typing-clear task.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        // FIX: ConcurrentDictionary is thread-safe — no lock required
        private static readonly ConcurrentDictionary<string, UserConnection> _connectedUsers = new();
        private static readonly ConcurrentDictionary<string, DateTime> _typingUsers = new();

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        // ================================================
        // CONNECTION HANDLING
        // ================================================

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Unknown";
            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

            _connectedUsers[Context.ConnectionId] = new UserConnection
            {
                UserId = userId,
                UserName = userName,
                ConnectionId = Context.ConnectionId,
                IsAdmin = isAdmin,
                ConnectedAt = DateTime.Now
            };

            _logger.LogInformation(
                "User {UserName} connected. ConnectionId: {ConnectionId}. IsAdmin: {IsAdmin}",
                userName, Context.ConnectionId, isAdmin);

            if (!isAdmin)
            {
                await Clients.Group("Admins").SendAsync("UserConnected", new
                {
                    UserName = userName,
                    Message = userName + " has joined the chat",
                    Timestamp = DateTime.Now.ToString("HH:mm:ss")
                });
            }

            if (isAdmin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                await SendActiveUsersToAdmin(Context.ConnectionId);
            }

            await Clients.All.SendAsync("UserCountUpdated", GetActiveUserCount());
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectedUsers.TryRemove(Context.ConnectionId, out var user))
            {
                var userName = user.UserName;
                var isAdmin = user.IsAdmin;

                // FIX: TryRemove is atomic on ConcurrentDictionary
                _typingUsers.TryRemove(user.UserId, out _);

                _logger.LogInformation("User {UserName} disconnected", userName);

                if (!isAdmin)
                {
                    await Clients.Group("Admins").SendAsync("UserDisconnected", new
                    {
                        UserName = userName,
                        Message = userName + " has left the chat",
                        Timestamp = DateTime.Now.ToString("HH:mm:ss")
                    });
                }

                await Clients.All.SendAsync("UserCountUpdated", GetActiveUserCount());
                await UpdateActiveUsersList();
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ================================================
        // QUESTION 11.5: MESSAGE SENDING
        // ================================================

        public async Task SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

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

            await Clients.All.SendAsync("ReceiveMessage", messageData);
        }

        // ================================================
        // QUESTION 11.5: TYPING INDICATOR
        // FIX: background task uses ConcurrentDictionary safely
        // ================================================

        public async Task UserTyping(string userName, bool isTyping)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return;

            var userId = Context.UserIdentifier;

            if (isTyping)
            {
                _typingUsers[userId] = DateTime.Now;

                // Background clear — safe because ConcurrentDictionary is thread-safe
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    if (_typingUsers.TryGetValue(userId, out var lastTyping) &&
                        (DateTime.Now - lastTyping).TotalSeconds >= 3)
                    {
                        _typingUsers.TryRemove(userId, out _);
                        await Clients.All.SendAsync("UserTyping", userName, false);
                    }
                });
            }
            else
            {
                _typingUsers.TryRemove(userId, out _);
            }

            await Clients.All.SendAsync("UserTyping", userName, isTyping);
        }

        // ================================================
        // QUESTION 11.6: PRIVATE MESSAGING
        // ================================================

        public async Task SendPrivateMessage(string targetUserId, string message)
        {
            var senderName = Context.User?.Identity?.Name ?? "Anonymous";
            var isAdmin = Context.User?.IsInRole("Admin") ?? false;

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

                await Clients.Caller.SendAsync("PrivateMessageSent", privateMessage);
            }
        }

        // ================================================
        // QUESTION 11.6: ACTIVE USERS (Admin only)
        // ================================================

        public async Task<List<ActiveUserInfo>> GetActiveUsers()
        {
            if (!(Context.User?.IsInRole("Admin") ?? false))
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

        // ------------------------------------------------
        // Private helpers
        // ------------------------------------------------

        private int GetActiveUserCount() => _connectedUsers.Count;

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

    // ------------------------------------------------
    // DTOs
    // ------------------------------------------------

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
