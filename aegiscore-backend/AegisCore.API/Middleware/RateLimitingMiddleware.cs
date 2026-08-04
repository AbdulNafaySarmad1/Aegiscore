using AegisCore.Application.ApiGateway;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AegisCore.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        var apiKeyHeader = context.Request.Headers["x-api-key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKeyHeader))
        {
            await _next(context);
            return;
        }

        var apiKey = await apiKeyService.ValidateApiKeyAsync(apiKeyHeader);
        if (apiKey == null)
        {
            await _next(context);
            return;
        }

        var isLimited = await apiKeyService.IsRateLimitedAsync(apiKey.Id);

        if (isLimited)
        {
            _logger.LogWarning("Rate limit exceeded for API key {Prefix}", apiKey.KeyPrefix);
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = "60";
            context.Response.Headers["X-RateLimit-Limit"] = apiKey.RateLimitPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            await context.Response.WriteAsJsonAsync(new { error = "rate_limit_exceeded", message = "Too many requests. Please slow down and try again." });
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = apiKey.RateLimitPerMinute.ToString();
        context.Response.Headers["X-Api-Tier"] = apiKey.Tier.ToString();

        await _next(context);
    }
}
