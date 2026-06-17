using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SmartGear_Online.Middleware
{
    /// Question 2: Custom Middleware
    /// Purpose: Logs every incoming HTTP request path &amp; method
    public class RequestPathLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestPathLoggingMiddleware> _logger;

        public RequestPathLoggingMiddleware(RequestDelegate next,
                                            ILogger<RequestPathLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var path = context.Request.Path;
            var method = context.Request.Method;
            var queryString = context.Request.QueryString;

            _logger.LogInformation(
                "Request: {Method} {Path}{QueryString} at {Timestamp}",
                method,
                path,
                queryString,
                startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes
                    .NameIdentifier)?.Value;
                var email = context.User.FindFirst(System.Security.Claims.ClaimTypes
                    .Email)?.Value;

                _logger.LogInformation(
                    "Request by User: {UserId} ({Email})",
                    userId,
                    email);
            }

            await _next(context);

            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Response: {StatusCode} completed in {Duration}ms",
                context.Response.StatusCode,
                duration);
        }
    }

    public static class RequestPathLoggingExtensions
    {
        public static IApplicationBuilder UseRequestPathLogging(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestPathLoggingMiddleware>();
        }
    }
}
