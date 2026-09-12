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

            // The query string is only logged at Debug level — it can carry
            // sensitive values (tokens, PII) and is not needed in the standard
            // request log.
            _logger.LogInformation(
                "Request: {Method} {Path} at {Timestamp}",
                method,
                path,
                startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            if (queryString.HasValue)
            {
                _logger.LogDebug(
                    "Request query string: {QueryString} for {Method} {Path}",
                    queryString,
                    method,
                    path);
            }

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
