using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SmartGear_Online.Controllers
{
    /// <summary>
    /// Hosts the SignalR live chat interface on /Chat/Index.
    /// Requires an authenticated user (the ChatHub is [Authorize] too).
    /// </summary>
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> _logger;

        public ChatController(ILogger<ChatController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Live Chat - SmartGear Online";
            return View();
        }
    }
}