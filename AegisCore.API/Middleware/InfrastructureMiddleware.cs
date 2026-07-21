using AegisCore.Application.ApiGateway;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AegisCore.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString("N")[..12];

        context.Response.Headers["X-Request-Id"] = requestId;

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var level = context.Response.StatusCode >= 500 ? LogLevel.Error
                : context.Response.StatusCode >= 400 ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(level,
                "{RequestId} {Method} {Path} {StatusCode} in {Elapsed:F1}ms",
                requestId, context.Request.Method, context.Request.Path,
                context.Response.StatusCode, elapsed);
        }
    }
}

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Security headers on every response
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var isDev = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().IsDevelopment();
            await context.Response.WriteAsJsonAsync(new
            {
                error = "internal_server_error",
                traceId = context.TraceIdentifier,
                message = isDev ? ex.Message : "An unexpected error occurred. Reference: " + context.TraceIdentifier
            });
        }
    }
}

public class AuthenticatedOriginPullsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticatedOriginPullsMiddleware> _logger;

    public AuthenticatedOriginPullsMiddleware(RequestDelegate next, ILogger<AuthenticatedOriginPullsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientCert = context.Connection.ClientCertificate;
        var certThumbprint = context.Request.Headers["X-Argo-Client-Thumbprint"].ToString();

        if (!string.IsNullOrEmpty(certThumbprint) && clientCert?.Thumbprint != certThumbprint)
        {
            _logger.LogWarning("Authenticated Origin Pulls: certificate thumbprint mismatch.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "origin_pull_denied" });
            return;
        }

        await _next(context);
    }
}

public class SessionBindingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionBindingMiddleware> _logger;

    public SessionBindingMiddleware(RequestDelegate next, ILogger<SessionBindingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sessionId = context.Request.Cookies["__Host-session-id"];
        var boundSession = context.Request.Headers["X-Session-Binding"].ToString();

        if (context.User.Identity?.IsAuthenticated == true
            && !string.IsNullOrEmpty(boundSession)
            && !string.Equals(boundSession, sessionId, StringComparison.Ordinal))
        {
            _logger.LogWarning("Session binding mismatch detected.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "session_binding_mismatch" });
            return;
        }

        await _next(context);
    }
}

public class TurnstileMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TurnstileMiddleware> _logger;

    public TurnstileMiddleware(RequestDelegate next, ILogger<TurnstileMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method))
        {
            var token = context.Request.Headers["cf-turnstile-response"].ToString()
                ?? context.Request.Form["cf-turnstile-response"].ToString();

            // When no Turnstile token is present and the site is public, allow the request
            // through. In a locked-down deployment, a missing token should be rejected.
            if (string.IsNullOrEmpty(token))
            {
                await _next(context);
                return;
            }

            var secret = context.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Turnstile:SecretKey"];
            if (string.IsNullOrEmpty(secret))
            {
                await _next(context);
                return;
            }

            using var httpClient = new HttpClient();
            var payload = new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = token,
                ["remoteip"] = context.Connection.RemoteIpAddress?.ToString() ?? ""
            };

            try
            {
                var verify = await httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify",
                    new FormUrlEncodedContent(payload));

                if (!verify.IsSuccessStatusCode)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "turnstile_failed" });
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Turnstile verification error.");
            }
        }

        await _next(context);
    }
}
