using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AegisCore.API.Middleware;

/// <summary>
/// Middleware that catches unhandled exceptions and returns a generic ProblemDetails response.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandlerMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title, type) = ex switch
            {
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", "https://tools.ietf.org/html/rfc7231#section-6.5.3"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "https://tools.ietf.org/html/rfc7231#section-6.6.1")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception occurred");
            else
                _logger.LogWarning("Request rejected with status {StatusCode}: {ExceptionType}", status, ex.GetType().Name);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = status;

            var problemDetails = new ProblemDetails
            {
                Title = title,
                Status = status,
                Type = type,
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred. Please try again later."
                    : "The request could not be processed."
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
        }
    }
}
