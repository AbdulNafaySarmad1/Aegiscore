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
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self'; connect-src 'self' wss:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
        context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";

        // Cache control for static assets — aggressive caching with immutable
        if (context.Request.Path.StartsWithSegments("/api/profile-pictures"))
        {
            context.Response.Headers["Cache-Control"] = "public, max-age=3600, immutable";
        }

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller went away; there is nobody to answer.
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, code, message) = Describe(ex);
            if (status >= 500)
                _logger.LogError(ex, "Unhandled exception at {Path} — TraceId: {TraceId}", context.Request.Path, context.TraceIdentifier);
            else
                _logger.LogInformation("{Code} at {Path}: {Message}", code, context.Request.Path, ex.Message);
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            // `error` is the sentence a person reads (the portal shows it verbatim); `code` is for programs.
            await context.Response.WriteAsJsonAsync(new { error = message, code, message, traceId = context.TraceIdentifier });
        }
    }

    /// <summary>
    /// Services signal "no such record" with <see cref="KeyNotFoundException"/> and a bad input with an
    /// <see cref="ArgumentException"/>; both describe the caller's situation, not a fault, and are answered
    /// as such. Everything else is a fault, answered with a reference an operator can find in the log.
    /// </summary>
    public static (int Status, string Code, string Message) Describe(Exception ex) => ex switch
    {
        AegisCore.Infrastructure.Licensing.LicenseReadOnlyException => (StatusCodes.Status403Forbidden, "license_read_only", ex.Message),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found", string.IsNullOrWhiteSpace(ex.Message) ? "The requested record was not found." : ex.Message),
        ArgumentException => (StatusCodes.Status400BadRequest, "invalid_request", ex.Message),
        NotSupportedException => (StatusCodes.Status501NotImplemented, "not_available", ex.Message),
        FormatException => (StatusCodes.Status400BadRequest, "invalid_request", "A value in the request has the wrong format: " + ex.Message),
        Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "concurrency_conflict", "Someone else changed this record first. Reload it and try again."),
        Microsoft.EntityFrameworkCore.DbUpdateException => (StatusCodes.Status500InternalServerError, "database_write_failed", "The database rejected this change. An administrator can find the details in the application log under the reference below."),
        System.Data.Common.DbException => (StatusCodes.Status503ServiceUnavailable, "database_unavailable", "The database did not answer. Check that the database server is running and the connection settings are correct; the application log holds the details under the reference below."),
        StackExchange.Redis.RedisException => (StatusCodes.Status503ServiceUnavailable, "cache_unavailable", "The Redis cache did not answer. Check that Redis is running and AEGISCORE_REDIS_CONNECTION points at it."),
        System.Text.Json.JsonException => (StatusCodes.Status500InternalServerError, "response_serialization_failed", "The server produced a response it could not serialise. This is a defect; report the reference below."),
        _ => (StatusCodes.Status500InternalServerError, "internal_server_error", "Something went wrong on the server. Report the reference below to your administrator, who can find the details in the application log.")
    };
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

            var secret = context.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["CF_TURNSTILE_SECRET_KEY"];
            if (string.IsNullOrEmpty(secret))
            {
                await _next(context);
                return;
            }

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
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
