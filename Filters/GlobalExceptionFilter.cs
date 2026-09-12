using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using System;

namespace SmartGear_Online.Filters
{
    /// QUESTION 9: GLOBAL EXCEPTION HANDLING
    /// Catches all unhandled exceptions across the application
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private static readonly EmptyModelMetadataProvider ModelMetadataProvider = new();

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

            var isApi = context.HttpContext.Request.Path.StartsWithSegments("/api");

            if (isApi)
            {
                if (context.Exception is ArgumentException)
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        error = "Invalid input provided",
                        details = context.Exception.Message
                    });
                }
                else if (context.Exception is UnauthorizedAccessException)
                {
                    context.Result = new UnauthorizedResult();
                }
                else if (context.Exception is KeyNotFoundException)
                {
                    context.Result = new NotFoundObjectResult(new
                    {
                        error = "Resource not found",
                        details = context.Exception.Message
                    });
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
                }
            }
            else
            {
                // MVC page request: render the shared Error view so a browser
                // visitor gets a human-readable page (with a reference ID they
                // can quote) instead of a bare JSON body.
                var statusCode = context.Exception switch
                {
                    ArgumentException => StatusCodes.Status400BadRequest,
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status500InternalServerError
                };

                context.Result = new ViewResult
                {
                    ViewName = "Error",
                    StatusCode = statusCode,
                    ViewData = new ViewDataDictionary(ModelMetadataProvider, new ModelStateDictionary())
                    {
                        ["RequestId"] = Guid.NewGuid().ToString(),
                        ["ErrorMessage"] = statusCode == StatusCodes.Status500InternalServerError
                            ? "We encountered an unexpected error and could not complete your request."
                            : context.Exception.Message
                    }
                };
            }

            context.ExceptionHandled = true;
        }
    }
}
