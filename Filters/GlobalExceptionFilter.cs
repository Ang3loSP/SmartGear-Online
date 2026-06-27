using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;

namespace SmartGear_Online.Filters
{
    /// QUESTION 9: GLOBAL EXCEPTION HANDLING
    /// Catches all unhandled exceptions across the application
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception,
                "Unhandled exception occurred in {ActionName} at {Timestamp}. Exception: {ExceptionType} - {ExceptionMessage}",
                context.ActionDescriptor.DisplayName,
                DateTime.UtcNow,
                context.Exception.GetType().Name,
                context.Exception.Message);

            _logger.LogDebug("Stack trace: {StackTrace}", context.Exception.StackTrace);

            if (context.Exception is ArgumentException)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    error = "Invalid input provided",
                    details = context.Exception.Message
                });
                context.ExceptionHandled = true;
            }
            else if (context.Exception is UnauthorizedAccessException)
            {
                context.Result = new UnauthorizedResult();
                context.ExceptionHandled = true;
            }
            else if (context.Exception is KeyNotFoundException)
            {
                context.Result = new NotFoundObjectResult(new
                {
                    error = "Resource not found",
                    details = context.Exception.Message
                });
                context.ExceptionHandled = true;
            }
            else
            {
                context.Result = new ObjectResult(new
                {
                    error = "An unexpected error occurred. Our team has been notified.",
                    reference = Guid.NewGuid().ToString()
                })
                {
                    StatusCode = 500
                };
                context.ExceptionHandled = true;
            }
        }
    }
}
