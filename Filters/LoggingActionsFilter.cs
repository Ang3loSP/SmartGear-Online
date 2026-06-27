using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace SmartGear_Online.Filters
{
    /// Question 3: Custom Action Filter
    /// Logs action execution time & user information
    /// Applied to controllers to track & monitor requests
    public class LoggingActionFilter : IActionFilter
    {
        private readonly ILogger<LoggingActionFilter> _logger;
        private Stopwatch _stopwatch = new Stopwatch();

        public LoggingActionFilter(ILogger<LoggingActionFilter> logger)
        {
            _logger = logger;
        }

        // Called BEFORE the action executes
        public void OnActionExecuting(ActionExecutingContext context)
        {
            _stopwatch = Stopwatch.StartNew();

            var actionName = context.ActionDescriptor.DisplayName;
            var userId = context.HttpContext.User?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";

            _logger.LogInformation(
                "Action executing: {ActionName} by User {UserId} at {Timestamp}",
                actionName,
                userId,
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        // Called AFTER the action executes
        public void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch.Stop();

            var actionName = context.ActionDescriptor.DisplayName;
            var statusCode = context.HttpContext.Response.StatusCode;

            _logger.LogInformation(
                "Action completed: {ActionName} - Status {StatusCode} - Duration {Duration}ms",
                actionName,
                statusCode,
                _stopwatch.ElapsedMilliseconds);

            if (context.Exception != null)
            {
                _logger.LogError(context.Exception,
                    "Action {ActionName} threw exception: {ExceptionMessage}",
                    actionName,
                    context.Exception.Message);
            }
        }
    }
}