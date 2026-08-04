using AegisCore.Application.ApiGateway;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Text;

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
            Console.Error.WriteLine(ex);
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

public sealed class HybridPqcTransportMiddleware(RequestDelegate next, IConfiguration configuration, IHybridPqcReplayStore replayStore)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/finance", StringComparison.OrdinalIgnoreCase)
            || !configuration.GetValue("HybridPqcTransport:Enforce", false))
        {
            await next(context);
            return;
        }

        var mode = context.Request.Headers["X-AegisCore-PQC-Transport"].ToString();
        var timestampText = context.Request.Headers["X-AegisCore-PQC-Timestamp"].ToString();
        var nonce = context.Request.Headers["X-AegisCore-PQC-Nonce"].ToString();
        var contentHashText = context.Request.Headers["X-AegisCore-Content-SHA256"].ToString();
        var supplied = context.Request.Headers["X-AegisCore-PQC-Proof"].ToString();
        var secretText = configuration["HybridPqcTransport:EdgeProofKeyBase64"];
        var skewSeconds = Math.Clamp(configuration.GetValue("HybridPqcTransport:MaxSkewSeconds", 60), 10, 300);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!long.TryParse(timestampText, out var timestamp)
            || timestamp < now - skewSeconds
            || timestamp > now + skewSeconds
            || !string.Equals(mode, "ml-kem-1024+x25519", StringComparison.OrdinalIgnoreCase)
            || nonce.Length is < 16 or > 128
            || nonce.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            || string.IsNullOrWhiteSpace(secretText))
        {
            await RejectAsync(context);
            return;
        }

        byte[] secret;
        byte[] proof;
        byte[] claimedContentHash;
        try
        {
            secret = Convert.FromBase64String(secretText);
            proof = Convert.FromBase64String(supplied);
            claimedContentHash = Convert.FromBase64String(contentHashText);
        }
        catch (FormatException) { await RejectAsync(context); return; }
        try
        {
            if (secret.Length < 32 || proof.Length != 32 || claimedContentHash.Length != 32)
            {
                await RejectAsync(context);
                return;
            }
            context.Request.EnableBuffering();
            var actualContentHash = await SHA256.HashDataAsync(context.Request.Body, context.RequestAborted);
            context.Request.Body.Position = 0;
            if (!CryptographicOperations.FixedTimeEquals(actualContentHash, claimedContentHash))
            {
                await RejectAsync(context);
                return;
            }
            var message = Encoding.UTF8.GetBytes($"{context.Request.Method}|{context.Request.Path}|{context.Request.QueryString}|{timestampText}|{nonce}|{mode.ToLowerInvariant()}|{contentHashText}");
            var expected = HMACSHA256.HashData(secret, message);
            if (!CryptographicOperations.FixedTimeEquals(expected, proof))
            {
                await RejectAsync(context);
                return;
            }
            if (!await replayStore.TryUseAsync(nonce, TimeSpan.FromSeconds(skewSeconds * 2L), context.RequestAborted))
            {
                await RejectAsync(context);
                return;
            }
        }
        finally { CryptographicOperations.ZeroMemory(secret); }

        await next(context);
    }

    private static async Task RejectAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
        await context.Response.WriteAsJsonAsync(new { error = "hybrid_pqc_transport_required" });
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
